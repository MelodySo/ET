namespace ET.Server
{
    /// <summary>仅记录/发送已成功施法的表现事件，不改变弹丸或战斗结算。</summary>
    public static class BrawlSkillUseNoticeHelper
    {
        public static void Start(Unit unit, long now)
        {
            BrawlSkillCastComponent cast = unit.GetComponent<BrawlSkillCastComponent>();
            // 直接调用底层序列的测试可以不提供技能ID，不为这种计划生成表现通知。
            cast.PresentationCastId = cast.SkillId > 0
                ? ++unit.Scene().GetComponent<BrawlProjectileRoomComponent>().SkillCastSequence : 0;
            Record(unit, BrawlSkillUsePhase.Started, now);
        }

        public static void Record(Unit unit, BrawlSkillUsePhase phase, long now, BrawlSkillStep step = default)
        {
            BrawlSkillCastComponent cast = unit.GetComponent<BrawlSkillCastComponent>();
            if (cast.PresentationCastId == 0) return;
            BrawlProjectileRoomComponent room = unit.Scene().GetComponent<BrawlProjectileRoomComponent>();
            room.SkillNotices.Add(new BrawlSkillUseNotice
            {
                EventId = ++room.SkillEventSequence, CastId = cast.PresentationCastId,
                UnitId = unit.Id, SkillId = cast.SkillId, Slot = cast.Slot,
                Tick = now / BrawlMovementSettings.TickMilliseconds, Phase = phase,
                DirectionX = cast.Direction.x, DirectionZ = cast.Direction.y,
                LockFacing = cast.Plan.LockFacing, FaceDirection = cast.Plan.FaceDirection,
                ImmediatePresentation = cast.Plan.ImmediatePresentation,
                StepId = step.Id, PresentationId = step.PresentationId,
                DurationMilliseconds = step.PresentationDurationMilliseconds, Radius = step.PresentationRadius,
                PositionX = unit.Position.x, PositionY = unit.Position.y, PositionZ = unit.Position.z,
            });
        }

        public static void Send(BrawlPracticeSessionComponent state)
        {
            Scene scene = state.Room;
            BrawlProjectileRoomComponent room = scene.GetComponent<BrawlProjectileRoomComponent>();
            if (room.SkillNotices.Count == 0) return;
            using G2C_BrawlSkillUse message = G2C_BrawlSkillUse.Create();
            message.EntryId = state.EntryId;
            message.RoomId = scene.Id;
            foreach (BrawlSkillUseNotice notice in room.SkillNotices)
            {
                BrawlSkillUseEvent item = BrawlSkillUseEvent.Create();
                item.EventId = notice.EventId; item.CastId = notice.CastId; item.UnitId = notice.UnitId;
                item.SkillId = notice.SkillId; item.Slot = (int)notice.Slot; item.Tick = notice.Tick;
                item.Phase = (int)notice.Phase; item.DirectionX = notice.DirectionX;
                item.DirectionZ = notice.DirectionZ; item.LockFacing = notice.LockFacing;
                item.FaceDirection = notice.FaceDirection; item.ImmediatePresentation = notice.ImmediatePresentation;
                item.StepId = notice.StepId; item.PresentationId = notice.PresentationId;
                item.DurationMilliseconds = notice.DurationMilliseconds; item.Radius = notice.Radius;
                item.PositionX = notice.PositionX; item.PositionY = notice.PositionY; item.PositionZ = notice.PositionZ;
                message.Events.Add(item);
            }
            // 当前练习房间只有一个真实连接，Bot 与玩家事件一起发给该连接。
            state.GetParent<Session>().Send(message);
            room.SkillNotices.Clear();
        }
    }
}
