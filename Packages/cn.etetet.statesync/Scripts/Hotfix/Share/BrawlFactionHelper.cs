namespace ET
{
    public static class BrawlFactionHelper
    {
        public static bool IsValid(int faction) => faction >= (int)BrawlFaction.NeutralHostile && faction <= (int)BrawlFaction.NeutralFriendly;

        public static bool IsEnemy(BrawlFaction source, BrawlFaction target)
        {
            if (source == target || source == BrawlFaction.NeutralFriendly || target == BrawlFaction.NeutralFriendly) return false;
            return true;
        }
    }
}
