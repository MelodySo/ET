using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ET
{
    public static class BrawlSkillAimAssetEditor
    {
        private const string ShaderPath = "Packages/cn.etetet.brawl/Assets/Shaders/BrawlSkillAimPreview.shader";
        private const string Folder = "Packages/cn.etetet.brawl/Bundles/Prefabs/Presentation";

        [MenuItem("Brawl/Presentation/Build Skill Aim Assets")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("请退出 Play 后创建资源。");
            AssetDatabase.ImportAsset(ShaderPath, ImportAssetOptions.ForceSynchronousImport);
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("技能范围 Shader 导入失败。");
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Packages/cn.etetet.brawl/Bundles/Prefabs", "Presentation");
            string materialPath = Folder + "/BrawlSkillAimPreview.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else if (material.shader != shader)
                throw new InvalidOperationException("已有范围材质使用不同 Shader，请先确认资源。");
            string prefabPath = Folder + "/BrawlSkillAimPreview.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                GameObject root = new("BrawlSkillAimPreview", typeof(MeshFilter), typeof(MeshRenderer));
                try
                {
                    MeshRenderer renderer = root.GetComponent<MeshRenderer>();
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    renderer.lightProbeUsage = LightProbeUsage.Off;
                    renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                    // 无初始 Mesh，无 Collider，无 Update；运行时由会话生成四顶点承载面。
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            AssetDatabase.SaveAssets();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab.GetComponent<MeshFilter>() == null || prefab.GetComponent<MeshRenderer>().sharedMaterial != material ||
                prefab.GetComponentsInChildren<Collider>().Length != 0 || !material.HasProperty("_Shape") ||
                !material.HasProperty("_Range") || !material.HasProperty("_Width") || !material.HasProperty("_HalfAngle"))
                throw new InvalidOperationException("技能范围 Prefab／材质契约检查失败。");
            Debug.Log("BRAWL_SKILL_AIM_ASSET_PASS: URP Shader 无导入错误，材质和 Prefab 引用有效，无 Collider。");
        }
    }
}
