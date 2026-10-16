using System;
using UnityEngine;

namespace ET.Client
{
    /// <summary>只发送意图与插值服务器快照，不做本地移动预测，在插值后驱动纯客户端动画。</summary>
    public static class BrawlPracticeMovementViewHelper
    {
        public static void Update(BrawlTrainingSession session)
        {
            if (session.Closing || session.CancelRequested) return;
            float now = Time.unscaledTime;
            float delta = Mathf.Max(0, now - session.LastRenderTime);
            session.LastRenderTime = now;
            if (session.JoystickBase != null) session.JoystickBase.gameObject.SetActive(!session.Loading);
            if (!session.Loading)
            {
                if (!Application.isFocused) BrawlJoystickHelper.Reset(session);
                Vector2 direction = BrawlMovementInputHelper.Read(session);
                bool startedOrStopped = (direction.sqrMagnitude > 0) != (session.LastSentDirection.sqrMagnitude > 0);
                if (now >= session.NextInputTime || startedOrStopped) SendInput(session, direction, now);
            }
            if (session.LastServerTick == 0) return;
            // 始终在已收到的快照之间播放，网络断流时停在最后位置，绝不向前外推。
            session.RenderTick = Math.Min(session.LastServerTick, session.RenderTick + delta / BrawlMovementSettings.TickSeconds);
            foreach (BrawlPracticeUnitView view in session.PracticeUnits)
            {
                if (view.Model == null || view.Samples.Count == 0) continue;
                while (view.Samples.Count > 1 && view.Samples[1].Tick <= session.RenderTick) view.Samples.RemoveAt(0);
                BrawlPositionSample first = view.Samples[0];
                Vector3 position = first.Position;
                if (view.Samples.Count > 1)
                {
                    BrawlPositionSample second = view.Samples[1];
                    float t = Mathf.Clamp01((float)((session.RenderTick - first.Tick) / (second.Tick - first.Tick)));
                    position = Vector3.Lerp(first.Position, second.Position, t);
                }
                view.Model.transform.localPosition = position;
                BrawlCharacterAnimationHelper.Update(view.Animation, delta);
            }
        }

        public static void Receive(BrawlTrainingSession session, G2C_BrawlMoveSnapshot message)
        {
            if (session.Closing || session.CancelRequested || message.EntryId != session.PracticeEntryId ||
                message.RoomId != session.PracticeRoomId || message.Tick <= session.LastServerTick) return;
            bool first = session.LastServerTick == 0;
            session.LastServerTick = message.Tick;
            session.LastAcknowledgedInput = message.LastInputSequence;
            if (message.HasRoster) BrawlUnitRosterViewHelper.Apply(session, message.Roster, message.MyUnitId);
            session.LastLifeEvents.Clear();
            foreach (BrawlLifeEvent item in message.LifeEvents)
            {
                session.LastLifeEvents.Add(new BrawlLifeNotice { UnitId = item.UnitId, Phase = (BrawlLifePhase)item.Phase, At = item.At, EndsAt = item.EndsAt });
                foreach (BrawlPracticeUnitView view in session.PracticeUnits)
                {
                    if (view.UnitId != item.UnitId) continue;
                    if ((BrawlLifePhase)item.Phase == BrawlLifePhase.Died) view.DeathDestroyAt = item.EndsAt;
                    if ((BrawlLifePhase)item.Phase == BrawlLifePhase.SpawnStarted) view.SpawnCompleteAt = item.EndsAt;
                    break;
                }
            }
            foreach (BrawlUnitStateChanges change in message.States)
            {
                foreach (BrawlPracticeUnitView view in session.PracticeUnits)
                {
                    if (view.UnitId != change.UnitId) continue;
                    foreach (BrawlStateValue value in change.States) view.States[(StateId)value.StateId] = value.Value;
                    break;
                }
            }
            foreach (BrawlPropertyState info in message.Properties)
            {
                foreach (BrawlPracticeUnitView view in session.PracticeUnits)
                {
                    if (view.UnitId != info.UnitId) continue;
                    view.Level = info.Level;
                    view.Health = info.Health;
                    view.MaxHealth = info.MaxHealth;
                    view.MaxAmmo = info.MaxAmmo;
                    view.Ammo = info.Ammo;
                    view.ReloadRate = info.ReloadRate;
                    view.MaxSuperAmmo = info.MaxSuperAmmo;
                    view.SuperAmmo = info.SuperAmmo;
                    view.SuperChargeRegenRate = info.SuperChargeRegenRate;
                    break;
                }
            }
            foreach (BrawlPracticeUnitView view in session.PracticeUnits)
            {
                bool dead = view.States.TryGetValue(StateId.Dead, out bool deadValue) && deadValue;
                bool spawning = view.States.TryGetValue(StateId.Spawning, out bool spawnValue) && spawnValue;
                view.AnimationLifeSamples.Add((message.Tick, dead, spawning));
                if (view.AnimationLifeSamples.Count > BrawlMovementSettings.MaxSnapshotHistory)
                    view.AnimationLifeSamples.RemoveAt(0);
            }
            bool resetTimeline = first || message.Tick - session.RenderTick > BrawlMovementSettings.MaxCatchUpTicks;
            if (resetTimeline)
                session.RenderTick = message.Tick - BrawlMovementSettings.InterpolationDelayTicks;
            foreach (BrawlMoveState info in message.Units)
            {
                if (float.IsNaN(info.X) || float.IsInfinity(info.X) || float.IsNaN(info.Z) || float.IsInfinity(info.Z)) continue;
                foreach (BrawlPracticeUnitView view in session.PracticeUnits)
                {
                    if (view.UnitId != info.UnitId) continue;
                    if (resetTimeline) BrawlCharacterAnimationHelper.ResetSampling(view.Animation);
                    if (first) view.Samples.Add(new BrawlPositionSample { Tick = message.Tick - BrawlMovementSettings.InterpolationDelayTicks, Position = view.Position });
                    view.Position = new Vector3(info.X, view.Position.y, info.Z);
                    view.MoveDirection = new Vector2(info.MoveX, info.MoveZ);
                    view.FacingDirection = new Vector2(info.FacingX, info.FacingZ);
                    view.Samples.Add(new BrawlPositionSample { Tick = message.Tick, Position = view.Position });
                    if (view.Samples.Count > BrawlMovementSettings.MaxSnapshotHistory) view.Samples.RemoveAt(0);
                    break;
                }
            }
        }

        private static void SendInput(BrawlTrainingSession session, Vector2 direction, float now)
        {
            session.NextInputTime = now + BrawlMovementSettings.TickSeconds;
            ClientSenderComponent sender = session.PracticeSender;
            if (sender == null || session.PracticeEntryId == 0) return;
            C2G_BrawlMoveInput message = C2G_BrawlMoveInput.Create();
            message.EntryId = session.PracticeEntryId;
            message.Sequence = ++session.InputSequence;
            message.X = direction.x;
            message.Z = direction.y;
            message.FacingX = session.FacingDirection.x;
            message.FacingZ = session.FacingDirection.y;
            sender.Send(message);
            session.LastSentDirection = direction;
        }

        public static void Stop(BrawlTrainingSession session)
        {
            BrawlJoystickHelper.Reset(session);
            try { SendInput(session, Vector2.zero, Time.unscaledTime); }
            catch (Exception exception) { Log.Warning($"Brawl stop input failed: {exception.Message}"); }
        }

        /// <summary>未来技能控制人物面向的入口；技能摇杆准备过程不调用。暂不操作模型/Animator。</summary>
        public static void SetFacingDirection(BrawlTrainingSession session, Vector2 facing)
        {
            if (float.IsNaN(facing.x) || float.IsInfinity(facing.x) || float.IsNaN(facing.y) || float.IsInfinity(facing.y) ||
                facing.sqrMagnitude <= 0) return;
            session.FacingDirection = facing.normalized;
            if (!session.Loading && !session.Closing && !session.CancelRequested)
                SendInput(session, BrawlMovementInputHelper.Read(session), Time.unscaledTime);
        }
    }
}
