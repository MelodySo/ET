using System;
using UnityEngine;

namespace ET.Client
{
    /// <summary>只发送意图与插值服务器快照，不做本地移动预测，也不驱动 Animator。</summary>
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
                Vector2 direction = Vector2.zero;
                if (Application.isFocused)
                {
                    direction.x = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
                    direction.y = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
                    direction = direction.sqrMagnitude > 0 ? Vector2.ClampMagnitude(direction, 1) : session.JoystickDirection;
                }
                else BrawlJoystickHelper.Reset(session);
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
            }
        }

        public static void Receive(BrawlTrainingSession session, G2C_BrawlMoveSnapshot message)
        {
            if (session.Closing || session.CancelRequested || message.EntryId != session.PracticeEntryId ||
                message.RoomId != session.PracticeRoomId || message.Tick <= session.LastServerTick) return;
            bool first = session.LastServerTick == 0;
            session.LastServerTick = message.Tick;
            session.LastAcknowledgedInput = message.LastInputSequence;
            if (first || message.Tick - session.RenderTick > BrawlMovementSettings.MaxCatchUpTicks)
                session.RenderTick = message.Tick - BrawlMovementSettings.InterpolationDelayTicks;
            foreach (BrawlMoveState info in message.Units)
            {
                if (float.IsNaN(info.X) || float.IsInfinity(info.X) || float.IsNaN(info.Z) || float.IsInfinity(info.Z)) continue;
                foreach (BrawlPracticeUnitView view in session.PracticeUnits)
                {
                    if (view.UnitId != info.UnitId) continue;
                    if (first) view.Samples.Add(new BrawlPositionSample { Tick = message.Tick - BrawlMovementSettings.InterpolationDelayTicks, Position = view.Position });
                    view.Position = new Vector3(info.X, view.Position.y, info.Z);
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
            sender.Send(message);
            session.LastSentDirection = direction;
        }

        public static void Stop(BrawlTrainingSession session)
        {
            BrawlJoystickHelper.Reset(session);
            try { SendInput(session, Vector2.zero, Time.unscaledTime); }
            catch (Exception exception) { Log.Warning($"Brawl stop input failed: {exception.Message}"); }
        }
    }
}
