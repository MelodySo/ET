using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ET
{
    /// <summary>从 Prefab 实际启用武器生成角色动作映射，只修改自有资源。</summary>
    public static class BrawlCharacterAnimationEditor
    {
        private const string CharacterPath = "Packages/cn.etetet.brawl/Bundles/Prefabs/Character/PBRDefault";
        private const string AnimationPath = "Packages/cn.etetet.brawl/Assets/Animation/BrawlCharacter";
        private const string SourcePath = "Packages/cn.etetet.brawl/Assets/Art/BattleRoyaleHeroesPBR/Animator";

        [MenuItem("Brawl/Animation/Build Character Locomotion Mappings")]
        [MenuItem("Brawl/Animation/Build Character Action Mappings")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("请先退出 Play。");
            AnimatorOverrideController defaults = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(AnimationPath + "/BrawlCharacterDefault.overrideController");
            if (!AssetDatabase.IsValidFolder(AnimationPath + "/Characters")) AssetDatabase.CreateFolder(AnimationPath, "Characters");
            List<string> report = new();
            foreach (string path in Directory.GetFiles(CharacterPath, "*.prefab").OrderBy(p => p))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                // 名称契约来自购买包武器资源；不根据英雄名字决定套装。双持的左右武器归为同类。
                string[] weaponTypes = prefab.GetComponentsInChildren<Transform>(true)
                    .Where(t => IsActive(t, prefab.transform))
                    .Select(t => t.name.Replace("_Left", "").Replace("Default", ""))
                    .Where(n => Directory.GetFiles(SourcePath, "*Animator.controller")
                        .Any(p => string.Equals(Path.GetFileNameWithoutExtension(p), n + "Animator", StringComparison.OrdinalIgnoreCase)))
                    .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                if (weaponTypes.Length > 1) throw new InvalidOperationException($"多个不同武器启用，需人工确认: {path}: {string.Join(",", weaponTypes)}");
                string weapon = weaponTypes.Length == 0 ? "NoWeapon" : weaponTypes[0];
                string sourceFile = Directory.GetFiles(SourcePath, "*Animator.controller")
                    .Single(p => string.Equals(Path.GetFileNameWithoutExtension(p), weapon + "Animator", StringComparison.OrdinalIgnoreCase));
                RuntimeAnimatorController source = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(sourceFile);
                List<KeyValuePair<AnimationClip, AnimationClip>> pairs = new();
                defaults.GetOverrides(pairs);
                AnimationClip idle = FindSourceClip(source, "IdleBattle01_");
                AnimationClip attack = FindAttackClip(source, weapon);
                for (int i = 0; i < pairs.Count; ++i)
                {
                    AnimationClip action = pairs[i].Key.name switch
                    {
                        "BrawlSlot_FullSpawn" => idle, // 当前购买包没有专用出生动画。
                        "BrawlSlot_FullDeath" => FindSourceClip(source, "Die_"),
                        "BrawlSlot_UpperNormalAttack" => attack,
                        "BrawlSlot_UpperSuperAttack" => attack,
                        _ => null,
                    };
                    if (action != null)
                    {
                        if (pairs[i].Key.name == "BrawlSlot_FullDeath" && action.isLooping)
                            throw new InvalidOperationException($"死亡动画不可循环: {AssetDatabase.GetAssetPath(action)}");
                        pairs[i] = new KeyValuePair<AnimationClip, AnimationClip>(pairs[i].Key, action);
                        continue;
                    }
                    string prefix = pairs[i].Key.name switch
                    {
                        "BrawlSlot_Idle" => "IdleBattle01_",
                        "BrawlSlot_MoveForward" => "RunFWD_",
                        "BrawlSlot_MoveBackward" => "RunBWD_",
                        "BrawlSlot_MoveLeft" => "RunLeft_",
                        "BrawlSlot_MoveRight" => "RunRight_",
                        _ => null,
                    };
                    if (prefix == null) continue;
                    AnimationClip clip = source.animationClips.Distinct().Single(c =>
                        Path.GetFileName(AssetDatabase.GetAssetPath(c)).StartsWith(prefix, StringComparison.Ordinal) &&
                        !AssetDatabase.GetAssetPath(c).Contains("_RM"));
                    if (prefix != "IdleBattle01_") clip = ResolveRootMotionClip(clip);
                    if (!clip.isLooping) throw new InvalidOperationException($"移动动画必须循环: {AssetDatabase.GetAssetPath(clip)}");
                    pairs[i] = new KeyValuePair<AnimationClip, AnimationClip>(pairs[i].Key, clip);
                }
                string destination = AnimationPath + "/Characters/" + prefab.name + ".overrideController";
                AnimatorOverrideController mapping = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(destination);
                if (mapping == null)
                {
                    mapping = new AnimatorOverrideController(defaults.runtimeAnimatorController);
                    AssetDatabase.CreateAsset(mapping, destination);
                }
                mapping.ApplyOverrides(pairs);
                EditorUtility.SetDirty(mapping);
                // 不重存整个 Prefab；只修改角色 Animator 的 controller 序列化引用。
                Animator animator = prefab.GetComponentsInChildren<Animator>(true).Single(a =>
                    a.runtimeAnimatorController == defaults || a.runtimeAnimatorController == mapping);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(animator.runtimeAnimatorController, out string oldGuid, out long oldId);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mapping, out string newGuid, out long newId);
                string text = File.ReadAllText(path);
                string oldReference = $"m_Controller: {{fileID: {oldId}, guid: {oldGuid}, type: 2}}";
                string newReference = $"m_Controller: {{fileID: {newId}, guid: {newGuid}, type: 2}}";
                if (!text.Contains(oldReference)) throw new InvalidOperationException($"无法定位角色 Controller 引用: {path}");
                File.WriteAllText(path, text.Replace(oldReference, newReference));
                AssetDatabase.ImportAsset(path);
                Validate(mapping);
                report.Add($"{Path.GetFileNameWithoutExtension(path)} | {weapon} | {Path.GetFileNameWithoutExtension(sourceFile)} | " +
                    string.Join(", ", pairs.Where(p => p.Key.name == "BrawlSlot_Idle" || p.Key.name.StartsWith("BrawlSlot_Move") ||
                        p.Key.name == "BrawlSlot_FullSpawn" || p.Key.name == "BrawlSlot_FullDeath" || p.Key.name.EndsWith("Attack"))
                        .Select(p => p.Key.name + "=" + Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(p.Value)))));
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Temp");
            File.WriteAllLines("Temp/BrawlCharacterAnimation-mappings.txt", report);
            Debug.Log($"BRAWL_ANIMATION_MAPPING_PASS: {report.Count} 个角色；移动、出生、攻击、死亡映射及运行时实例隔离通过。详见 Temp/BrawlCharacterAnimation-mappings.txt");
        }

        private static AnimationClip FindSourceClip(RuntimeAnimatorController source, string prefix)
        {
            return source.animationClips.Distinct().Single(c =>
                Path.GetFileName(AssetDatabase.GetAssetPath(c)).StartsWith(prefix, StringComparison.Ordinal) &&
                !AssetDatabase.GetAssetPath(c).Contains("_RM"));
        }

        private static AnimationClip FindAttackClip(RuntimeAnimatorController source, string weapon)
        {
            // 同一握持套装存在多种攻击时，霰弹枪明确选择泵动射击；其余按源控制器拥有的动作匹配。
            if (string.Equals(weapon, "ShotGun", StringComparison.OrdinalIgnoreCase)) return FindSourceClip(source, "ShootShotgun_");
            foreach (string prefix in new[] { "ShootSingleshot_", "ShootAutoshot_", "ShootSustainshot_", "ShootChargeshot_", "ShootAuto_", "Throw_", "Stab_", "PunchRight_" })
            {
                if (source.animationClips.Any(c => Path.GetFileName(AssetDatabase.GetAssetPath(c)).StartsWith(prefix, StringComparison.Ordinal)))
                    return FindSourceClip(source, prefix);
            }
            throw new InvalidOperationException($"没有可用的攻击动作: {source.name}: " +
                string.Join(", ", source.animationClips.Select(c => AssetDatabase.GetAssetPath(c)).Distinct()));
        }

        [MenuItem("Brawl/Animation/Inspect Motion Speeds")]
        public static void InspectMotionSpeeds()
        {
            List<string> report = new();
            foreach (string path in Directory.GetFiles(AnimationPath + "/Characters", "*.overrideController"))
            {
                AnimatorOverrideController mapping = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
                List<KeyValuePair<AnimationClip, AnimationClip>> pairs = new();
                mapping.GetOverrides(pairs);
                foreach (var pair in pairs.Where(p => p.Key.name.StartsWith("BrawlSlot_Move")))
                {
                    string sourcePath = AssetDatabase.GetAssetPath(pair.Value);
                    string file = Path.GetFileName(sourcePath);
                    int separator = file.IndexOf('_');
                    string rmPath = Path.GetDirectoryName(sourcePath).Replace('\\', '/') + "/" + (file.Contains("_RM_") ? file : file.Insert(separator, "_RM"));
                    AnimationClip rm = AssetDatabase.LoadAllAssetsAtPath(rmPath).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
                    report.Add($"{mapping.name} | {pair.Key.name} | source={pair.Value.averageSpeed:F4} duration={pair.Value.length:F4} | RM={(rm == null ? "MISSING" : rm.averageSpeed.ToString("F4"))} duration={rm?.length:F4} loop={rm?.isLooping}");
                }
            }
            File.WriteAllLines("Temp/BrawlAnimation-motion-speeds.txt", report);
            Debug.Log("BRAWL_MOTION_SPEED_INSPECTED: Temp/BrawlAnimation-motion-speeds.txt");
        }
        [MenuItem("Brawl/Animation/Verify Root Motion Scale")]
        public static void VerifyRootMotionScale()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("只在非 Play 状态执行。");
            List<string> report = new();
            foreach (string name in new[] { "BasicFemaleDefault", "SalarymanDefault" })
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath + "/" + name + ".prefab");
                var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
                GameObject instance = UnityEngine.Object.Instantiate(prefab);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance, scene);
                PlayableGraph graph = default;
                try
                {
                    Animator animator = instance.GetComponentsInChildren<Animator>().Single(a => a.GetLayerIndex("Locomotion") == 0);
                    var mapping = (AnimatorOverrideController)animator.runtimeAnimatorController;
                    List<KeyValuePair<AnimationClip, AnimationClip>> pairs = new();
                    mapping.GetOverrides(pairs);
                    AnimationClip source = pairs.Single(p => p.Key.name == "BrawlSlot_MoveForward").Value;
                    string path = AssetDatabase.GetAssetPath(source);
                    string file = Path.GetFileName(path);
                    string rmPath = Path.GetDirectoryName(path).Replace('\\', '/') + "/" + (file.Contains("_RM_") ? file : file.Insert(file.IndexOf('_'), "_RM"));
                    AnimationClip rm = AssetDatabase.LoadAllAssetsAtPath(rmPath).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));
                    animator.runtimeAnimatorController = null;
                    animator.applyRootMotion = true;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    graph = PlayableGraph.Create("BrawlMotionScaleVerification");
                    graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    var output = AnimationPlayableOutput.Create(graph, "Motion", animator);
                    var playable = AnimationClipPlayable.Create(graph, rm);
                    output.SetSourcePlayable(playable);
                    graph.Play();
                    graph.Evaluate(0);
                    Vector3 distance = Vector3.zero;
                    const int samples = 60;
                    float step = rm.length / samples;
                    for (int i = 0; i < samples; i++)
                    {
                        graph.Evaluate(step);
                        distance += animator.deltaPosition;
                    }
                    distance.y = 0;
                    Vector3 expected = animator.transform.TransformVector(rm.averageSpeed * animator.humanScale);
                    expected.y = 0;
                    float measured = distance.magnitude / rm.length;
                    if (Mathf.Abs(measured - expected.magnitude) > expected.magnitude * 0.02f)
                        throw new InvalidOperationException($"Avatar速度换算与实际根位移不符: {name}");
                    animator.applyRootMotion = false;
                    graph.Evaluate(0);
                    Vector3 stationary = instance.transform.position;
                    for (int i = 0; i < samples; i++) graph.Evaluate(step);
                    if (Vector3.Distance(stationary, instance.transform.position) > 0.0001f)
                        throw new InvalidOperationException($"关闭Root Motion后仍发生模型位移: {name}");
                    report.Add($"{name}: clip={rm.averageSpeed:F4}, humanScale={animator.humanScale:F4}, lossyScale={animator.transform.lossyScale:F4}, measured={measured:F4} m/s, predicted={expected.magnitude:F4}, rootMotionOff=stationary");
                }
                finally
                {
                    if (graph.IsValid()) graph.Destroy();
                    UnityEngine.Object.DestroyImmediate(instance);
                    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
                }
            }
            File.WriteAllLines("Temp/BrawlAnimation-motion-scale.txt", report);
            Debug.Log("BRAWL_MOTION_SCALE_VERIFIED: Temp/BrawlAnimation-motion-scale.txt");
        }
        private static AnimationClip ResolveRootMotionClip(AnimationClip source)
        {
            string path = AssetDatabase.GetAssetPath(source);
            string file = Path.GetFileName(path);
            string rmPath = Path.GetDirectoryName(path).Replace('\\', '/') + "/" +
                (file.Contains("_RM_") ? file : file.Insert(file.IndexOf('_'), "_RM"));
            AnimationClip rm = AssetDatabase.LoadAllAssetsAtPath(rmPath).OfType<AnimationClip>()
                .SingleOrDefault(c => !c.name.StartsWith("__preview__"));
            if (rm == null || !rm.isLooping || Mathf.Abs(rm.length - source.length) > 0.001f ||
                new Vector2(rm.averageSpeed.x, rm.averageSpeed.z).sqrMagnitude <= 0.000001f)
                throw new InvalidOperationException($"缺少有效的同周期 RM 移动动画: {rmPath}");
            return rm;
        }
        private static bool IsActive(Transform transform, Transform root)
        {
            for (Transform current = transform; current != null; current = current.parent)
            {
                if (!current.gameObject.activeSelf) return false;
                if (current == root) return true;
            }
            return false;
        }

        private static void Validate(AnimatorOverrideController mapping)
        {
            List<KeyValuePair<AnimationClip, AnimationClip>> pairs = new();
            mapping.GetOverrides(pairs);
            if (pairs.Count != 24 || pairs.Select(p => p.Key).Distinct().Count() != 24 ||
                pairs.Any(p => p.Key == null || p.Value == null || !p.Key.name.StartsWith("BrawlSlot_")))
                throw new InvalidOperationException($"槽位契约无效: {mapping.name}");
            AnimatorOverrideController first = new(mapping.runtimeAnimatorController);
            AnimatorOverrideController second = new(mapping.runtimeAnimatorController);
            try
            {
                first.ApplyOverrides(pairs);
                second.ApplyOverrides(pairs);
                AnimationClip key = pairs[0].Key;
                AnimationClip original = mapping[key];
                first[key] = key;
                if (mapping[key] != original || second[key] != original)
                    throw new InvalidOperationException("Override 实例共享修改，隔离验证失败。");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
            }
        }
    }
}
