using System;
using System.Collections.Generic;
using UnityEngine;
namespace ET.Client
{
    public static class BrawlUnitRosterViewHelper
    {
        public static void Apply(BrawlTrainingSession session, List<BrawlPracticeUnitInfo> roster, long myUnitId)
        {
            BrawlPracticeClientComponent client = session.MovementClient;
            if (client == null) return;
            HashSet<long> present = new();
            if (session.MyUnitId != myUnitId) session.Hero = null;
            session.MyUnitId = myUnitId;
            foreach (BrawlPracticeUnitInfo info in roster)
            {
                present.Add(info.UnitId);
                BrawlPracticeUnitView view = session.PracticeUnits.Find(v => v.UnitId == info.UnitId);
                if (view == null)
                {
                    BrawlPracticeUnitKind kind = (BrawlPracticeUnitKind)info.Kind;
                    string address = kind == BrawlPracticeUnitKind.NPC
                        ? client.Fiber().GetSingleton<BrawlNPCConfigCategory>().Get(info.ConfigId).ModelPath
                        : client.Fiber().GetSingleton<BrawlHeroConfigCategory>().Get(info.ConfigId).ModelPath;
                    view = new BrawlPracticeUnitView { UnitId = info.UnitId, ConfigId = info.ConfigId, Kind = kind, ModelPath = address };
                    session.PracticeUnits.Add(view);
                }
                view.Faction = (BrawlFaction)info.Faction;
                view.Position = new Vector3(info.X, info.Y, info.Z);
                view.FacingDirection = new Vector2(info.FacingX, info.FacingZ);
                view.Level = info.Level; view.Health = info.Health; view.MaxHealth = info.MaxHealth;
                view.MaxAmmo = info.MaxAmmo; view.Ammo = info.Ammo; view.ReloadRate = info.ReloadRate;
                view.MaxSuperAmmo = info.MaxSuperAmmo; view.SuperAmmo = info.SuperAmmo; view.SuperChargeRegenRate = info.SuperChargeRegenRate;
                view.SpawnCompleteAt = info.SpawnCompleteAt; view.DeathDestroyAt = info.DeathDestroyAt;
                view.States.Clear();
                foreach (BrawlStateValue value in info.States) view.States[(StateId)value.StateId] = value.Value;
            }
            for (int i = session.PracticeUnits.Count - 1; i >= 0; --i)
            {
                BrawlPracticeUnitView view = session.PracticeUnits[i];
                if (present.Contains(view.UnitId)) continue;
                view.Removed = true;
                if (view.Model != null) UnityEngine.Object.Destroy(view.Model);
                session.PracticeUnits.RemoveAt(i);
            }
        }

        public static void Update(BrawlTrainingSession session)
        {
            if (session.Loading || session.Closing || session.CancelRequested || session.NPCRoot == null) return;
            foreach (BrawlPracticeUnitView view in session.PracticeUnits)
            {
                if (view.Model == null && !view.ModelLoading && !view.ModelLoadFailed) LoadModel(session, view).Coroutine();
                if (view.Model != null)
                {
                    bool spawning = view.States.TryGetValue(StateId.Spawning, out bool value) && value;
                    if (view.Model.activeSelf == spawning) view.Model.SetActive(!spawning);
                    if (view.UnitId == session.MyUnitId) session.Hero = view.Model;
                }
            }
            BrawlCameraComponent cameraController = session.CameraController;
            cameraController?.SetTarget(session.Hero != null ? session.Hero.transform : null);
        }

        public static async ETTask LoadModel(BrawlTrainingSession session, BrawlPracticeUnitView view)
        {
            if (view.Removed || view.ModelLoading || view.Model != null) return;
            view.ModelLoading = true;
            try
            {
                GameObject model = await BrawlModelHelper.InstantiateModel(session, view.ModelPath, session.NPCRoot.transform,
                    $"{view.Kind}_{view.ConfigId}_{view.UnitId}", view.Position);
                if (session.CancelRequested || session.Closing || view.Removed)
                {
                    if (model != null) UnityEngine.Object.Destroy(model);
                    return;
                }
                view.Model = model;
                model.transform.localPosition = view.Position;
                foreach (Animator animator in model.GetComponentsInChildren<Animator>(true)) animator.applyRootMotion = false;
                model.SetActive(!(view.States.TryGetValue(StateId.Spawning, out bool spawning) && spawning));
                if (view.UnitId == session.MyUnitId) session.Hero = model;
            }
            catch (Exception exception)
            {
                view.ModelLoadFailed = true;
                if (session.Loading && !session.CancelRequested && !session.Closing) throw;
                if (!session.CancelRequested && !session.Closing) Log.Error($"Brawl unit model load failed: {view.UnitId}: {exception}");
            }
            finally { view.ModelLoading = false; }
        }
    }
}
