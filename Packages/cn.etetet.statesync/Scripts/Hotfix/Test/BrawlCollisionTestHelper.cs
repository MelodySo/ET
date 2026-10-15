using System.Collections.Generic;
namespace ET.Test
{
    public static class BrawlCollisionTestHelper
    {
        public static BrawlCollisionRuleConfigCategory Category() => new(new Dictionary<int, BrawlCollisionRuleConfig>
        {
            [101] = new(101, 1, 8, 0, 3, true, false),
            [102] = new(102, 1, 4, 0, 1, true, true),
            [201] = new(201, 2, 8, 32, 3, true, false),
            [202] = new(202, 2, 40, 0, 3, false, true),
            [203] = new(203, 2, 4, 0, 1, false, true),
            [204] = new(204, 2, 48, 0, 3, false, true),
        });
        public static BrawlCollisionRuleConfig[] Rules(int group) => BrawlCollisionRuleHelper.Resolve(Category(), group);
    }
}
