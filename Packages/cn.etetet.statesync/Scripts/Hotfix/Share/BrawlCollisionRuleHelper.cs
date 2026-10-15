using System;
using System.Collections.Generic;

namespace ET
{
    public static class BrawlCollisionRuleHelper
    {
        public static BrawlCollisionRuleConfig[] Resolve(BrawlCollisionRuleConfigCategory category, int groupId)
        {
            if (groupId <= 0) throw new ArgumentException("CollisionRuleGroupId must be positive.");
            List<BrawlCollisionRuleConfig> rules = new();
            foreach (BrawlCollisionRuleConfig rule in category.GetAll().Values)
            {
                if (rule.GroupId != groupId) continue;
                int required = rule.RequiredLayerMask;
                int excluded = rule.ExcludedLayerMask;
                if (((required | excluded) & ~(int)BrawlUnitLayer.All) != 0 || (required & excluded) != 0 ||
                    rule.RelationMask <= 0 || rule.RelationMask > 3 || (!rule.BlockProjectile && !rule.ApplyHitEvents))
                    throw new ArgumentException($"Collision rule {rule.Id} is invalid.");
                rules.Add(rule);
            }
            if (rules.Count == 0) throw new ArgumentException($"Collision rule group {groupId} does not exist.");
            return rules.ToArray();
        }

        public static void Match(BrawlCollisionRuleConfig[] rules, BrawlUnitLayer layers, BrawlFaction source,
            BrawlFaction target, out bool blocked, out bool effective)
        {
            blocked = false;
            effective = false;
            int relation = BrawlFactionHelper.IsEnemy(source, target) ? 1 : 2;
            foreach (BrawlCollisionRuleConfig rule in rules)
            {
                if (((int)layers & rule.RequiredLayerMask) != rule.RequiredLayerMask ||
                    ((int)layers & rule.ExcludedLayerMask) != 0 || (rule.RelationMask & relation) == 0) continue;
                blocked |= rule.BlockProjectile;
                effective |= rule.ApplyHitEvents;
            }
        }
    }
}
