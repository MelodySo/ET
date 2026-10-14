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
            foreach (BrawlProjectileState info in message.Projectiles)
            {
                if (!session.Projectiles.TryGetValue(info.ProjectileId, out BrawlProjectileView view))
                {
                    GameObject model = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    model.name = $"Projectile_{info.ConfigId}_{info.ProjectileId}";
                    Collider collider = model.GetComponent<Collider>();
                    collider.enabled = false;
                    UnityEngine.Object.Destroy(collider);
                    model.transform.SetParent(session.RuntimeRoot.transform, false);
                    model.transform.localScale = Vector3.one * (info.Radius * 2);
                    model.transform.localPosition = new Vector3(info.X, info.Y, info.Z);
                    model.SetActive(false);
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
                view.Model.SetActive(session.RenderTick >= view.SpawnTick);
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
            foreach (long id in session.RemovedProjectiles) session.Projectiles.Remove(id);
            session.RemovedProjectiles.Clear();
        }

        public static void Clear(BrawlTrainingSession session)
        {
            foreach (BrawlProjectileView view in session.Projectiles.Values)
                if (view.Model != null) UnityEngine.Object.Destroy(view.Model);
            session.Projectiles.Clear();
            session.RemovedProjectiles.Clear();
        }
    }
}

