using UnityEngine;

namespace ET.Client
{
    /// <summary>权威施法通知到纯视觉动作的单向适配，不读取摇杆来判断施法成功。</summary>
    public static class BrawlSkillUseViewHelper
    {
        public static void Receive(BrawlTrainingSession session, G2C_BrawlSkillUse message)
        {
            if (session.Closing || session.CancelRequested || message.EntryId != session.PracticeEntryId ||
                message.RoomId != session.PracticeRoomId) return;
            foreach (BrawlSkillUseEvent item in message.Events)
            {
                if (item.EventId <= session.LastSkillUseEventId) continue;
                session.LastSkillUseEventId = item.EventId;
                if (float.IsNaN(item.DirectionX) || float.IsInfinity(item.DirectionX) ||
                    float.IsNaN(item.DirectionZ) || float.IsInfinity(item.DirectionZ)) continue;
                session.PendingSkillUseEvents.Add(new BrawlSkillUseNotice
                {
                    EventId = item.EventId, CastId = item.CastId, UnitId = item.UnitId, SkillId = item.SkillId,
                    Slot = (BrawlSkillSlot)item.Slot, Tick = item.Tick, Phase = (BrawlSkillUsePhase)item.Phase,
                    DirectionX = item.DirectionX, DirectionZ = item.DirectionZ, Sustained = item.Sustained,
                });
            }
        }

        public static void Update(BrawlTrainingSession session)
        {
            if (session.Loading || session.Closing || session.CancelRequested) return;
            for (int i = 0; i < session.PendingSkillUseEvents.Count;)
            {
                BrawlSkillUseNotice notice = session.PendingSkillUseEvents[i];
                if (notice.Tick > session.RenderTick) break;
                bool expired = session.RenderTick - notice.Tick > BrawlMovementSettings.MaxCatchUpTicks;
                BrawlPracticeUnitView unit = session.PracticeUnits.Find(v => v.UnitId == notice.UnitId);
                // 名单/模型可能稍晚于事件；有限等待，加载完成后不补播很久以前的攻击。
                if (!expired && (unit == null || unit.Model == null)) { ++i; continue; }
                session.PendingSkillUseEvents.RemoveAt(i);
                BrawlCharacterAnimationView view = unit?.Animation;
                if (view == null) continue;
                if (notice.Phase == BrawlSkillUsePhase.Completed || notice.Phase == BrawlSkillUsePhase.Interrupted)
                {
                    BrawlCharacterAnimationHelper.EndCast(view, notice.CastId, notice.Phase == BrawlSkillUsePhase.Interrupted);
                }
                else if (!expired)
                {
                    if (notice.Phase == BrawlSkillUsePhase.Started) BrawlCharacterAnimationHelper.BeginCast(view, notice);
                    else if (notice.Phase == BrawlSkillUsePhase.Fired && view.ActiveCastId == notice.CastId)
                        BrawlCharacterAnimationHelper.PlayAttack(view, notice.Slot == BrawlSkillSlot.Super,
                            new Vector3(notice.DirectionX, 0, notice.DirectionZ));
                }
                if (unit.UnitId == session.MyUnitId)
                    session.FacingDirection = new Vector2(view.FacingDirection.x, view.FacingDirection.z);
            }
        }

        public static void Clear(BrawlTrainingSession session)
        {
            session.PendingSkillUseEvents.Clear();
            session.LastSkillUseEventId = 0;
        }
    }
}
