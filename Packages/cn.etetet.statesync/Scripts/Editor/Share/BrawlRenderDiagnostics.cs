using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using ET.Client;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ET
{
    /// <summary>编辑器诊断；命令参数来自 Temp/BrawlRenderCommand.txt，不进入玩家构建。</summary>
    public sealed class BrawlRenderDiagnostics : EditorWindow
    {
        private const string CommandPath = "Temp/BrawlRenderCommand.txt";
        private const int SampleFrames = 120;
        private readonly List<int> batches = new();
        private readonly List<int> draws = new();
        private readonly List<int> passes = new();
        private readonly List<int> triangles = new();
        private string reportPath;
        private int lastFrame;
        private int warmup;

        [MenuItem("Brawl/Rendering/Render Diagnostics")]
        public static void Open() => GetWindow<BrawlRenderDiagnostics>("Brawl 渲染诊断");

        [MenuItem("Brawl/Rendering/Execute Diagnostics Command")]
        public static void Execute()
        {
            string[] args = File.ReadAllLines(CommandPath);
            BrawlRenderDiagnostics window = GetWindow<BrawlRenderDiagnostics>("Brawl 渲染诊断");
            switch (args[0])
            {
                case "inspect": window.Inspect(); break;
                case "click":
                    GameObject target = Resources.FindObjectsOfTypeAll<Transform>().Single(t => t.gameObject.scene.IsValid() && t.name == args[1]).gameObject;
                    ExecuteEvents.Execute(target, new PointerEventData(UnityEngine.EventSystems.EventSystem.current), ExecuteEvents.pointerClickHandler);
                    break;
                case "sample":
                    window.BeginSample(args[1]);
                    break;
                case "terrain":
                    GameObject.Find("BrawlTrainingRuntime").transform.Find("Terrain").gameObject.SetActive(bool.Parse(args[1]));
                    break;
                case "batching": BrawlTerrainRenderHelper.SetInstancing(Session(), bool.Parse(args[1])); break;
                case "move":
                    Session().AIProbe = new BrawlAIPlayProbe { Direction = new Vector2(float.Parse(args[1]), float.Parse(args[2])) };
                    break;
                case "stop": Session().AIProbe = null; break;
                case "cleargrass": BrawlTerrainRenderHelper.ClearGrass(Session(), int.Parse(args[1])); break;
                case "cast": BrawlSkillInputHelper.Cast(Session(), (BrawlSkillSlot)int.Parse(args[1]), new Vector2(float.Parse(args[2]), float.Parse(args[3])).normalized); break;
                case "cleanup-orphans":
                    if (EditorApplication.isPlaying) throw new InvalidOperationException("仅能在停止 Play 后清理旧诊断遗留材质。");
                    foreach (Material material in Resources.FindObjectsOfTypeAll<Material>().Where(m => m.name.EndsWith(" (Brawl Terrain Instanced)") && !AssetDatabase.Contains(m)))
                        DestroyImmediate(material);
                    break;
                default: throw new ArgumentException("未知诊断命令：" + args[0]);
            }
        }

        private void BeginSample(string name)
        {
            reportPath = "Temp/" + Path.GetFileName(name) + ".txt";
            batches.Clear(); draws.Clear(); passes.Clear(); triangles.Clear();
            warmup = 30;
            EditorApplication.ExecuteMenuItem("Window/General/Game");
        }

        private static BrawlTrainingSession Session()
        {
            // 仅编辑器诊断访问；不为测试向框架增加全局会话入口。
            Fiber main = (Fiber)typeof(FiberManager).GetField("mainFiber", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(FiberManager.Instance);
            return main.GetFiber("Client").Root.GetComponent<BrawlPracticeClientComponent>().Session;
        }

        private void Update()
        {
            if (string.IsNullOrEmpty(reportPath) || !EditorApplication.isPlaying || Time.frameCount == lastFrame) return;
            lastFrame = Time.frameCount;
            if (warmup-- > 0) return;
            batches.Add(UnityStats.batches); draws.Add(UnityStats.drawCalls); passes.Add(UnityStats.setPassCalls); triangles.Add(UnityStats.triangles);
            if (batches.Count < SampleFrames) return;
            string report = $"scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().path}\n" +
                $"resolution={UnityStats.screenRes}\nframes={SampleFrames}\n" +
                Summary("batches", batches) + Summary("drawCalls", draws) + Summary("setPassCalls", passes) + Summary("triangles", triangles);
            Camera camera = Camera.allCameras.FirstOrDefault(c => c.name == "BrawlCamera");
            if (camera != null) report += $"camera={camera.transform.position}, rotation={camera.transform.eulerAngles}, fov={camera.fieldOfView}\n";
            File.WriteAllText(reportPath, report);
            Debug.Log($"BRAWL_RENDER_SAMPLE {reportPath}\n{report}");
            reportPath = null;
        }

        private static string Summary(string label, List<int> samples)
        {
            samples.Sort();
            return $"{label}: min={samples[0]} median={samples[samples.Count / 2]} max={samples[samples.Count - 1]} mean={samples.Average():F2}\n";
        }

        private void Inspect()
        {
            StringBuilder result = new();
            result.AppendLine($"GPU={SystemInfo.graphicsDeviceName}, instancing={SystemInfo.supportsInstancing}");
            SerializedObject graphics = new(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            result.AppendLine($"Instancing stripping={graphics.FindProperty("m_InstancingStripping").enumDisplayNames[graphics.FindProperty("m_InstancingStripping").enumValueIndex]}");
            result.AppendLine($"Runtime terrain materials={Resources.FindObjectsOfTypeAll<Material>().Count(m => m.name.EndsWith(" (Brawl Terrain Instanced)"))}");
            if (GameObject.Find("BrawlTrainingRuntime") != null)
            {
                BrawlTrainingSession session = Session();
                result.AppendLine($"Terrain tiles={session.TerrainRendering.Tiles.Count}, objects={session.TerrainRendering.Objects.Count}, NPCs={session.TerrainRendering.UnitOwners.Count}, batches={session.TerrainRendering.Batches.Count}, materials={session.TerrainRendering.Materials.Count}, instancing={session.TerrainRendering.Instancing}");
                result.AppendLine($"Server room={session.PracticeRoomId}, tick={session.LastServerTick}, units={session.PracticeUnits.Count}, clearedGrass={session.ClearedGrass.Count}");
                foreach (BrawlPracticeUnitView unit in session.PracticeUnits)
                    result.AppendLine($"Unit {unit.UnitId} config={unit.ConfigId} kind={unit.Kind} pos={unit.Position} hp={unit.Health}/{unit.MaxHealth} model={unit.Model?.name} instanced={session.TerrainRendering.UnitOwners.ContainsKey(unit.UnitId)}");
            }
            foreach (Camera camera in Camera.allCameras)
                result.AppendLine($"Camera {camera.name} pos={camera.transform.position} rotation={camera.transform.eulerAngles} size={camera.pixelWidth}x{camera.pixelHeight}");
            foreach (Light light in FindObjectsOfType<Light>())
                result.AppendLine($"Light {light.name} type={light.type} shadows={light.shadows} intensity={light.intensity}");
            foreach (IGrouping<string, MeshRenderer> group in FindObjectsOfType<MeshRenderer>().GroupBy(r =>
                $"{r.GetComponent<MeshFilter>()?.sharedMesh?.name} / {string.Join(",", r.sharedMaterials.Select(m => m != null ? m.name : "NULL"))}"))
            {
                MeshRenderer renderer = group.First();
                Mesh mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                result.AppendLine($"Mesh {group.Key}: objects={group.Count()}, enabled={group.Count(r => r.enabled)}, vertices={mesh?.vertexCount}, submeshes={mesh?.subMeshCount}, shadow={renderer.shadowCastingMode}, probes={renderer.lightProbeUsage}, lightmap={renderer.lightmapIndex}");
                foreach (Material material in renderer.sharedMaterials.Where(m => m != null))
                    result.AppendLine($"  Material {material.name}: shader={material.shader.name}, instancing={material.enableInstancing}, passes={material.passCount}, path={AssetDatabase.GetAssetPath(material)}");
            }
            foreach (Button button in Resources.FindObjectsOfTypeAll<Button>().Where(b => b.gameObject.scene.IsValid()))
                result.AppendLine($"Button {button.name} active={button.gameObject.activeInHierarchy} interactable={button.interactable}");
            foreach (MonoBehaviour behaviour in FindObjectsOfType<MonoBehaviour>().Where(b => b is IPointerClickHandler))
                result.AppendLine($"Clickable {behaviour.name} type={behaviour.GetType().FullName}");
            File.WriteAllText("Temp/BrawlRenderInspection.txt", result.ToString());
            Debug.Log("BRAWL_RENDER_INSPECT Temp/BrawlRenderInspection.txt");
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("进入训练场或练习赛后，在同一相机位置比较两种渲染路径。只切换显示，不改变碰撞、画质或服务器状态。报告写入 Temp。", MessageType.Info);
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || GameObject.Find("BrawlTrainingRuntime") == null))
            {
                if (GUILayout.Button("原始 Renderer（对照）")) BrawlTerrainRenderHelper.SetInstancing(Session(), false);
                if (GUILayout.Button("GPU 实例化（默认）")) BrawlTerrainRenderHelper.SetInstancing(Session(), true);
                if (GUILayout.Button("采样 120 帧")) BeginSample("Brawl-render-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            }
            if (GUILayout.Button("导出当前渲染对象信息")) Inspect();
            EditorGUILayout.LabelField(string.IsNullOrEmpty(reportPath) ? "未采样" : $"采样中：{batches.Count}/{SampleFrames}");
        }
    }
}
