using UnityEngine;

namespace ET.Client
{
    public static class BrawlProjectileViewHelper
    {
        public static void Receive(BrawlTrainingSession session, G2C_BrawlProjectileSnapshot message)
        {
            if (session.Loading || session.Closing || session.CancelRequested || session.RuntimeRoot == null ||
                message.EntryId != session.PracticeEntryId || message.RoomId != session.PracticeRoomId ||
                message.Sequence <= session.LastProjectileSnapshotSequence) return;
            session.LastProjectileSnapshotSequence = message.Sequence;
            foreach (BrawlProjectileEvent info in message.Events)
            {
                if (info.EventId <= session.LastProjectileEventId) continue;
                session.LastProjectileEventId = info.EventId;
                // 协议对象会回池，必须复制为独立值记录。
                session.PendingProjectileEvents.Add(new BrawlProjectileNotice
                {
                    EventId = info.EventId, Tick = info.Tick, ProjectileId = info.ProjectileId, ConfigId = info.ConfigId,
                    OwnerUnitId = info.OwnerUnitId, TargetUnitId = info.TargetUnitId, TerrainIndex = info.TerrainIndex,
                    Kind = (BrawlProjectileEventKind)info.Kind, EndReason = (BrawlProjectileEndReason)info.EndReason,
                    X = info.X, Y = info.Y, Z = info.Z, NormalX = info.NormalX, NormalZ = info.NormalZ,
                    Blocked = info.Blocked, Effective = info.Effective,
                });
            }
            foreach (int index in message.ClearedGrass)
                if (!session.ClearedGrass.Contains(index) && !session.PendingClearedGrass.ContainsKey(index))
                    session.PendingClearedGrass.Add(index, message.Tick);
            foreach (BrawlProjectileState info in message.Projectiles)
            {
                if (!session.Projectiles.TryGetValue(info.ProjectileId, out BrawlProjectileView view))
                {
                    GameObject model = BrawlProjectileVisualHelper.CreateFlight(session, info);
                    view = new BrawlProjectileView { Model = model, SpawnTick = message.Tick, Faction = (BrawlFaction)info.Faction };
                    session.Projectiles.Add(info.ProjectileId, view);
                }
                view.LastSeenSequence = message.Sequence;
                view.DespawnTick = -1;
                Vector3 position = new Vector3(info.X, info.Y, info.Z);
                // 同 Tick 可以有多次释放快照；覆盖同 Tick 采样，避免插值除零。
                if (view.Samples.Count > 0 && view.Samples[view.Samples.Count - 1].Tick == message.Tick)
                    view.Samples[view.Samples.Count - 1].Position = position;
                else view.Samples.Add(new BrawlPositionSample { Tick = message.Tick, Position = position });
                if (view.Samples.Count > BrawlMovementSettings.MaxSnapshotHistory) view.Samples.RemoveAt(0);
            }
            foreach (BrawlProjectileView view in session.Projectiles.Values)
                if (view.LastSeenSequence != message.Sequence && view.DespawnTick < 0) view.DespawnTick = message.Tick;
        }

        public static void Update(BrawlTrainingSession session)
        {
            if (session.Loading || session.Closing || session.CancelRequested) return;
            foreach (var pair in session.Projectiles)
            {
                BrawlProjectileView view = pair.Value;
                if (view.DespawnTick >= 0 && session.RenderTick >= view.DespawnTick)
                {
                    UnityEngine.Object.Destroy(view.Model);
                    session.RemovedProjectiles.Add(pair.Key);
                    continue;
                }
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
                bool visible = !view.Ended && session.RenderTick >= view.SpawnTick;
                if (visible && !view.Model.activeSelf)
                {
                    view.Model.SetActive(true);
                    foreach (ParticleSystem particles in view.Model.GetComponentsInChildren<ParticleSystem>()) particles.Play(false);
                }
                else if (!visible) view.Model.SetActive(false);
            }
            foreach (long id in session.RemovedProjectiles) session.Projectiles.Remove(id);
            session.RemovedProjectiles.Clear();
            int consumed = 0;
            foreach (BrawlProjectileNotice notice in session.PendingProjectileEvents)
            {
                if (notice.Tick > session.RenderTick) break;
                ++consumed;
                session.ProjectilePresentation?.OnProjectileEvent(session, notice);
            }
            if (consumed > 0) session.PendingProjectileEvents.RemoveRange(0, consumed);
            foreach (var pair in session.PendingClearedGrass)
            {
                if (pair.Value > session.RenderTick) continue;
                session.ClearedGrass.Add(pair.Key);
                session.AppliedClearedGrass.Add(pair.Key);
                session.ProjectilePresentation?.OnGrassCleared(session, pair.Key);
            }
            foreach (int index in session.AppliedClearedGrass) session.PendingClearedGrass.Remove(index);
            session.AppliedClearedGrass.Clear();
            BrawlProjectileVisualHelper.UpdateEffects(session);
        }

        public static void Clear(BrawlTrainingSession session)
        {
            foreach (BrawlProjectileView view in session.Projectiles.Values)
                if (view.Model != null) UnityEngine.Object.Destroy(view.Model);
            BrawlProjectileVisualHelper.Clear(session);
            session.Projectiles.Clear();
            session.RemovedProjectiles.Clear();
            session.PendingProjectileEvents.Clear();
            session.ClearedGrass.Clear();
            session.PendingClearedGrass.Clear();
            session.AppliedClearedGrass.Clear();
            BrawlProjectileVisualHelper.UpdateEffects(session);
            session.ProjectilePresentation = null;
            session.LastProjectileEventId = 0;
        }
    }
}
