using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ET
{
    /// <summary>Targeted exporter: does not enumerate or overwrite MMO Spell/Buff assets.</summary>
    public static class BrawlAIEditor
    {
        public const string AssetPath = "Packages/cn.etetet.statesync/Settings/BrawlAI/BullChase.asset";
        private const string AssetDirectory = "Packages/cn.etetet.statesync/Settings/BrawlAI";

        [MenuItem("Brawl/AI/Create or Select Shelly and Colt Combat")]
        public static void CreateCombat()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("请先退出 Play。");
            // 演示配对保存在资产中，技能射程与碰撞规则从运行时配置读取。
            CreateCombatAsset("ShellyCombat", 101, 401);
            BrawlAIScriptableObject colt = CreateCombatAsset("ColtCombat", 401, 101);
            AssetDatabase.SaveAssets();
            Selection.activeObject = colt;
            Export(colt);
        }

        private static BrawlAIScriptableObject CreateCombatAsset(string name, int heroId, int opponentId)
        {
            string path = AssetDirectory + "/" + name + ".asset";
            BrawlAIScriptableObject asset = AssetDatabase.LoadAssetAtPath<BrawlAIScriptableObject>(path);
            if (asset != null) return asset; // 不覆盖已在行为树编辑器调整的内容。
            asset = ScriptableObject.CreateInstance<BrawlAIScriptableObject>();
            asset.HeroId = heroId;
            asset.OpponentHeroId = opponentId;
            asset.Tree = new BrawlAIRoot { Id = 1, Children = new()
            {
                new BTSelector { Id = 2, Children = new()
                {
                    new BTSequence { Id = 3, Children = new()
                    {
                        new BTBrawlHasActiveCast { Id = 4, Desc = "当前连发尚未结束" },
                        new BTBrawlIdle { Id = 5, Desc = "停步完成当前攻击" },
                    } },
                    new BTSequence { Id = 6, Children = new()
                    {
                        new BTBrawlAttackPosition { Id = 7, Slot = BrawlSkillSlot.Super, Desc = "大招位置：技能射程与碰撞规则" },
                        new BTBrawlUseSkill { Id = 8, Slot = BrawlSkillSlot.Super, Desc = "优先使用大招" },
                    } },
                    new BTSequence { Id = 9, Children = new()
                    {
                        new BTBrawlAttackPosition { Id = 10, Slot = BrawlSkillSlot.NormalAttack, Desc = "普攻位置：技能射程与碰撞规则" },
                        new BTBrawlUseSkill { Id = 11, Slot = BrawlSkillSlot.NormalAttack, Desc = "普通攻击" },
                    } },
                    new BTSequence { Id = 12, Children = new()
                    {
                        new BTBrawlAttackPosition { Id = 13, Slot = BrawlSkillSlot.NormalAttack, Desc = "已在畅通的普攻位置" },
                        new BTBrawlIdle { Id = 14, Desc = "等待装填或冷却，不继续贴近" },
                    } },
                    new BTBrawlChase { Id = 15, Desc = "无警戒距离，追逐或绕墙" },
                } },
            } };
            if (heroId == 401)
            {
                int nextId = MaxNodeId(asset.Tree) + 1;
                EnableFollowing(asset.Tree, ref nextId);
            }
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        [MenuItem("Brawl/AI/Enable Colt Moving Attacks")]
        public static void EnableColtMovingAttacks()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("请先退出 Play。");
            BrawlAIScriptableObject asset = AssetDatabase.LoadAssetAtPath<BrawlAIScriptableObject>(AssetDirectory + "/ColtCombat.asset");
            if (asset == null) throw new InvalidOperationException("请先创建 ColtCombat 行为树。");
            Undo.RecordObject(asset, "Enable Colt Moving Attacks");
            int nextId = MaxNodeId(asset.Tree) + 1;
            EnableFollowing(asset.Tree, ref nextId);
            EditorUtility.SetDirty(asset);
            Selection.activeObject = asset;
            Export(asset);
        }

        private static int MaxNodeId(BTNode node)
        {
            int result = node.Id;
            foreach (BTNode child in node.Children) result = Math.Max(result, MaxNodeId(child));
            return result;
        }

        // 只迁移旧的连发待机与施法成功分支；重复执行不覆盖已编辑的跟随参数。
        private static void EnableFollowing(BTNode node, ref int nextId)
        {
            if (node is BTSequence && node.Children.OfType<BTBrawlHasActiveCast>().Any())
            {
                for (int i = 0; i < node.Children.Count; ++i)
                    if (node.Children[i] is BTBrawlIdle idle)
                        node.Children[i] = new BTBrawlFollowTarget
                        { Id = idle.Id, Desc = "按连发技能矩形纠正站位；受阻则追击" };
            }
            if (node is BTSequence && node.Children.OfType<BTBrawlUseSkill>().Any() &&
                !node.Children.OfType<BTBrawlFollowTarget>().Any())
            {
                BTBrawlAttackPosition position = node.Children.OfType<BTBrawlAttackPosition>().FirstOrDefault();
                BTBrawlFollowTarget follow = new() { Id = nextId++, Desc = "按技能完整宽度纠偏并维持射程余量" };
                if (position != null) { follow.RangeMargin = position.RangeMargin; follow.SightWidth = position.SightWidth; }
                node.Children.Add(follow);
            }
            foreach (BTNode child in node.Children) EnableFollowing(child, ref nextId);
        }
        [MenuItem("Brawl/AI/Create or Select Bull Chase")]
        public static void CreateOrSelect()
        {
            BrawlAIScriptableObject asset = AssetDatabase.LoadAssetAtPath<BrawlAIScriptableObject>(AssetPath);
            if (asset == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
                AssetDatabase.Refresh();
                asset = ScriptableObject.CreateInstance<BrawlAIScriptableObject>();
                asset.Tree = new BrawlAIRoot { Id = 1, Children = new()
                {
                    new BTSelector { Id = 2, Children = new()
                    {
                        new BTSequence { Id = 3, Children = new()
                        {
                            new BTBrawlPlayerWithinDistance { Id = 4, Distance = 5 },
                            new BTBrawlChase { Id = 5 },
                        } },
                        new BTBrawlIdle { Id = 6 },
                    } },
                } };
                AssetDatabase.CreateAsset(asset, AssetPath);
                AssetDatabase.SaveAssets();
            }
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
        [MenuItem("Brawl/AI/Export Selected AI")]
        public static void ExportSelected()
        {
            BrawlAIScriptableObject asset = Selection.activeObject as BrawlAIScriptableObject;
            Export(asset);
        }
        public static void Export(BrawlAIScriptableObject asset)
        {
            if (asset == null) throw new InvalidOperationException("Select an ET/Brawl AI asset first.");
            if (asset.OpponentHeroId <= 0) throw new InvalidOperationException("此历史树没有练习赛配对。请使用 ShellyCombat / ColtCombat。");
            BrawlAIScriptableObject[] assets = AssetDatabase.FindAssets("t:BrawlAIScriptableObject", new[] { AssetDirectory })
                .Select(guid => AssetDatabase.LoadAssetAtPath<BrawlAIScriptableObject>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(item => item.OpponentHeroId > 0).OrderBy(item => item.HeroId).ToArray();
            List<string> errors = new();
            HashSet<int> heroIds = new();
            foreach (BrawlAIScriptableObject item in assets)
            {
                if (item.HeroId <= 0 || item.Tree == null || item.Tree.Children.Count != 1 || !heroIds.Add(item.HeroId))
                    throw new InvalidOperationException("AI requires unique hero IDs and one root child.");
                if (UpgradeAttackPositions(item.Tree)) EditorUtility.SetDirty(item);
                BTNodeValidator.ValidateNodeRecursive(item.Tree, new Dictionary<string, Type>(), errors);
                Validate(item.Tree, errors);
                if (!assets.Any(other => other.HeroId == item.OpponentHeroId && other.OpponentHeroId == item.HeroId && other != item))
                    errors.Add($"Missing reciprocal opponent for hero {item.HeroId}.");
            }
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            EditorUtility.SetDirty(asset); AssetDatabase.SaveAssets();
            string trees = string.Join(",\n", assets.Select(item => $"[{item.HeroId}] = " + new CSharpObjectCodeEmitter().Emit(item.Tree)));
            string opponents = string.Join(", ", assets.Select(item => $"[{item.HeroId}] = {item.OpponentHeroId}"));
            string code = "// Generated by Brawl/AI/Export Selected AI. Do not edit.\nnamespace ET\n{\n" +
                "    [ConfigGroup(\"Config\")]\n    public sealed class BrawlAIConfigCategoryFactory_Config : IConfigFactory\n    {\n" +
                "        public System.Type ConfigType => typeof(BrawlAIConfigCategory);\n" +
                "        public ASingleton Create() => new BrawlAIConfigCategory { Trees = new() { " + trees +
                " }, PracticeOpponents = new() { " + opponents + " } };\n    }\n}\n";
            foreach (string mode in new[] { "Client", "ClientServer", "Server" })
            {
                string directory = $"Packages/cn.etetet.statesync/CodeMode/Config/{mode}/Config";
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, "BrawlAIConfigCategoryFactory_Config.cs");
                if (!File.Exists(path) || File.ReadAllText(path) != code) File.WriteAllText(path, code, new UTF8Encoding(false));
            }
            AssetDatabase.Refresh();
            Debug.Log($"BRAWL_AI_EXPORT_OK heroes={string.Join(",", heroIds)}; trees={assets.Length}; reciprocal practice opponents verified");
        }
        private static void Validate(BTNode node, List<string> errors)
        {
            if (node is not BrawlAIRoot && node is not BTSelector && node is not BTSequence &&
                node is not BTBrawlPlayerWithinDistance && node is not BTBrawlChase && node is not BTBrawlIdle &&
                node is not BTBrawlHasActiveCast && node is not BTBrawlAttackPosition && node is not BTBrawlUseSkill &&
                node is not BTBrawlFollowTarget)
                errors.Add($"Unsupported Brawl AI node: {node.GetType().Name}");
            if (node is BTBrawlAttackPosition position)
            {
                if (float.IsNaN(position.RangeMargin) || float.IsInfinity(position.RangeMargin) || position.RangeMargin < 0 ||
                    float.IsNaN(position.SightWidth) || float.IsInfinity(position.SightWidth) || position.SightWidth < 0)
                    errors.Add("Range margin and sight width must be finite and non-negative.");
                if (position.Slot != BrawlSkillSlot.NormalAttack && position.Slot != BrawlSkillSlot.Super)
                    errors.Add("Attack position must select NormalAttack or Super.");
            }
            if (node is BTBrawlUseSkill skill && skill.Slot != BrawlSkillSlot.NormalAttack && skill.Slot != BrawlSkillSlot.Super)
                errors.Add("AI skill slot must be NormalAttack or Super.");
            if (node is BTBrawlPlayerWithinDistance condition && (float.IsNaN(condition.Distance) || float.IsInfinity(condition.Distance) || condition.Distance < 0))
                errors.Add("Distance must be finite and non-negative.");
            if (node is BTBrawlChase chase && (chase.RepathMilliseconds < 50 || chase.MaxSearchNodes <= 0 || chase.MaxSearchNodes > 10000))
                errors.Add("RepathMilliseconds >= 50 and MaxSearchNodes in 1..10000 are required.");
            if (node is BTBrawlFollowTarget follow &&
                (float.IsNaN(follow.RangeMargin) || float.IsInfinity(follow.RangeMargin) || follow.RangeMargin < 0 ||
                 float.IsNaN(follow.SightWidth) || float.IsInfinity(follow.SightWidth) || follow.SightWidth < 0 ||
                 follow.RepathMilliseconds < 50 || follow.MaxSearchNodes <= 0 || follow.MaxSearchNodes > 10000))
                errors.Add("Follow requires finite non-negative margin/sight width, RepathMilliseconds >= 50 and MaxSearchNodes in 1..10000.");
            if (node is BTAction || node is BTCondition)
                if (node.Children.Count != 0) errors.Add("Brawl actions and conditions must be leaves.");
            foreach (BTNode child in node.Children)
            {
                if (child == null) errors.Add("Null node.");
                else Validate(child, errors);
            }
        }

        // 旧资产没有 Slot；按所在 Sequence 的施法动作迁移，等待装填分支使用普攻。
        // 已配置节点不覆盖，保留用户编辑的参数和树结构。
        private static bool UpgradeAttackPositions(BTNode node, BrawlSkillSlot slot = BrawlSkillSlot.NormalAttack)
        {
            if (node is BTSequence)
                slot = node.Children.OfType<BTBrawlUseSkill>().Select(item => item.Slot).DefaultIfEmpty(BrawlSkillSlot.NormalAttack).First();
            bool changed = false;
            if (node is BTBrawlAttackPosition position && position.Slot == 0)
            {
                position.Slot = slot;
                position.RangeMargin = 0.5f;
                position.Desc = slot == BrawlSkillSlot.Super ? "大招位置：技能射程与碰撞规则" : "普攻位置：技能射程与碰撞规则";
                changed = true;
            }
            foreach (BTNode child in node.Children)
                if (child != null) changed |= UpgradeAttackPositions(child, slot);
            return changed;
        }
    }
}
