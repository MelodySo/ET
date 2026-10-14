namespace ET.Server
{
    public static class BrawlUnitSnapshotHelper
    {
        public static BrawlPracticeUnitInfo Create(Unit unit)
        {
            BrawlPracticeUnitComponent data = unit.GetComponent<BrawlPracticeUnitComponent>();
            BrawlPracticeUnitInfo info = BrawlPracticeUnitInfo.Create();
            info.UnitId = unit.Id; info.ConfigId = unit.ConfigId; info.Kind = (int)data.Kind; info.Faction = (int)data.Faction;
            info.X = unit.Position.x; info.Y = unit.Position.y; info.Z = unit.Position.z;
            BrawlMoveComponent move = unit.GetComponent<BrawlMoveComponent>();
            info.FacingX = move == null ? 0 : move.FacingDirection.x;
            info.FacingZ = move == null ? 1 : move.FacingDirection.y;
            PropertyComponent properties = unit.GetComponent<PropertyComponent>();
            info.Level = properties.GetInt(PropertyId.Level); info.Health = properties.GetFloat(PropertyId.Health);
            info.MaxHealth = properties.GetFloat(PropertyId.MaxHealth);
            info.SpawnCompleteAt = unit.GetComponent<BrawlSpawnComponent>().CompleteAt;
            info.DeathDestroyAt = unit.GetComponent<BrawlDeathComponent>().DestroyAt;
            BrawlStateSyncHelper.FillInitial(unit.GetComponent<StateComponent>(), info.States);
            return info;
        }

        public static void FillRoster(Scene scene, G2C_BrawlMoveSnapshot snapshot)
        {
            BrawlLifecycleRoomComponent life = scene.GetComponent<BrawlLifecycleRoomComponent>();
            snapshot.HasRoster = life.RosterDirty;
            if (life.RosterDirty)
            {
                foreach (Entity child in scene.GetComponent<UnitComponent>().Children.Values) snapshot.Roster.Add(Create((Unit)child));
                life.RosterDirty = false;
            }
            foreach (BrawlLifeNotice notice in life.Notices)
            {
                BrawlLifeEvent item = BrawlLifeEvent.Create();
                item.UnitId = notice.UnitId; item.Phase = (int)notice.Phase; item.At = notice.At; item.EndsAt = notice.EndsAt;
                snapshot.LifeEvents.Add(item);
            }
            life.Notices.Clear();
        }
    }
}
