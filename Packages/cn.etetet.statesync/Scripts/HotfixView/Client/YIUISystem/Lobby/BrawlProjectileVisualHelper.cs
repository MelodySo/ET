using System;
using UnityEngine;

namespace ET.Client
{
    public static class BrawlProjectileVisualHelper
    {
        public static async ETTask Preload(BrawlTrainingSession session, BrawlProjectileConfigCategory configs)
        {
            session.ProjectileTemplateRoot = new GameObject("ProjectileTemplates");
            session.ProjectileTemplateRoot.SetActive(false);
            session.ProjectileTemplateRoot.transform.SetParent(session.RuntimeRoot.transform, false);
            foreach (BrawlProjectileConfig config in configs.GetAll().Values)
            {
                if (config.CollisionRuleGroupId <= 0) continue;
                session.ProjectileVisualConfigs.Add(config.Id, config);
                await LoadTemplate(session, config.ModelPath, false);
                if (session.CancelRequested || session.Closing) return;
                await LoadTemplate(session, config.ImpactEffectPath, true);
                if (session.CancelRequested || session.Closing) return;
                await LoadTemplate(session, config.ExpireEffectPath, true);
                if (session.CancelRequested || session.Closing) return;
            }
        }

        private static async ETTask LoadTemplate(BrawlTrainingSession session, string address, bool effect)
        {
            if (string.IsNullOrWhiteSpace(address)) return;
            if (!session.ProjectileTemplates.TryGetValue(address, out GameObject template))
            {
                template = await BrawlModelHelper.InstantiateModel(session, address, session.ProjectileTemplateRoot.transform,
                    $"Template_{address}", Vector3.zero);
                if (session.CancelRequested || session.Closing || template == null) return;
                template.SetActive(false);
                foreach (Collider collider in template.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                foreach (Rigidbody body in template.GetComponentsInChildren<Rigidbody>(true))
                {
                    body.isKinematic = true; body.useGravity = false; body.detectCollisions = false;
                }
                foreach (ParticleSystem particles in template.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = particles.main;
                    main.stopAction = ParticleSystemStopAction.None;
                    main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
                    var collision = particles.collision; collision.enabled = false;
                    var trigger = particles.trigger; trigger.enabled = false;
                }
                session.ProjectileTemplates.Add(address, template);
            }
            if (effect && template.GetComponentsInChildren<ParticleSystem>(true).Length == 0)
                throw new InvalidOperationException($"弹丸一次性特效 {address} 缺少ParticleSystem，无法按粒子结束回收。");
        }

        public static GameObject CreateFlight(BrawlTrainingSession session, BrawlProjectileState info)
        {
            BrawlProjectileConfig config = session.ProjectileVisualConfigs[info.ConfigId];
            GameObject root = new GameObject($"Projectile_{info.ConfigId}_{info.ProjectileId}");
            root.SetActive(false);
            root.transform.SetParent(session.RuntimeRoot.transform, false);
            root.transform.localPosition = new Vector3(info.X, info.Y, info.Z);
            Vector3 direction = new Vector3(info.DirectionX, 0, info.DirectionZ);
            if (direction.sqrMagnitude > 0) root.transform.localRotation = Quaternion.LookRotation(direction, Vector3.up);
            if (string.IsNullOrWhiteSpace(config.ModelPath))
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.SetParent(root.transform, false);
                sphere.transform.localScale = Vector3.one * (info.Radius * 2);
                sphere.GetComponent<Collider>().enabled = false;
                UnityEngine.Object.Destroy(sphere.GetComponent<Collider>());
            }
            else
            {
                GameObject visual = UnityEngine.Object.Instantiate(session.ProjectileTemplates[config.ModelPath], root.transform, false);
                foreach (ParticleSystem particles in visual.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = particles.main; main.loop = true;
                }
                visual.SetActive(true);
            }
            return root;
        }

        public static void Present(BrawlTrainingSession session, BrawlProjectileNotice notice)
        {
            if (session.Closing || session.CancelRequested) return;
            BrawlProjectileConfig config = session.ProjectileVisualConfigs[notice.ConfigId];
            session.Projectiles.TryGetValue(notice.ProjectileId, out BrawlProjectileView view);
            Quaternion rotation = view?.Model != null ? view.Model.transform.localRotation : Quaternion.identity;
            string address = null;
            if (notice.Kind == BrawlProjectileEventKind.Impact)
            {
                address = config.ImpactEffectPath;
                Vector3 normal = new Vector3(notice.NormalX, 0, notice.NormalZ);
                if (normal.sqrMagnitude > 0) rotation = Quaternion.LookRotation(normal, Vector3.up);
            }
            else if (notice.Kind == BrawlProjectileEventKind.End)
            {
                if (view != null)
                {
                    view.Ended = true;
                    if (view.Model != null) view.Model.SetActive(false);
                }
                if (notice.EndReason == BrawlProjectileEndReason.Range || notice.EndReason == BrawlProjectileEndReason.Lifetime)
                    address = config.ExpireEffectPath;
            }
            if (string.IsNullOrWhiteSpace(address)) return;
            GameObject root = UnityEngine.Object.Instantiate(session.ProjectileTemplates[address], session.RuntimeRoot.transform, false);
            root.name = $"ProjectileEffect_{notice.EventId}_{address}";
            root.transform.localPosition = new Vector3(notice.X, notice.Y, notice.Z);
            root.transform.localRotation = rotation * root.transform.localRotation;
            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem particles in systems)
            {
                var main = particles.main; main.loop = false;
            }
            root.SetActive(true);
            foreach (ParticleSystem particles in systems)
                if (particles.gameObject.activeInHierarchy) particles.Play(false);
            session.ProjectileEffects.Add(new BrawlProjectileEffectView { Root = root, Particles = systems });
        }

        public static void UpdateEffects(BrawlTrainingSession session)
        {
            for (int i = session.ProjectileEffects.Count - 1; i >= 0; --i)
            {
                BrawlProjectileEffectView effect = session.ProjectileEffects[i];
                bool alive = false;
                if (effect.Root != null)
                    foreach (ParticleSystem particles in effect.Particles)
                        if (particles != null && particles.gameObject.activeInHierarchy && particles.IsAlive(false)) { alive = true; break; }
                if (alive) continue;
                if (effect.Root != null) UnityEngine.Object.Destroy(effect.Root);
                session.ProjectileEffects.RemoveAt(i);
            }
        }

        public static void Clear(BrawlTrainingSession session)
        {
            foreach (BrawlProjectileEffectView effect in session.ProjectileEffects)
                if (effect.Root != null) UnityEngine.Object.Destroy(effect.Root);
            session.ProjectileEffects.Clear();
            if (session.ProjectileTemplateRoot != null) UnityEngine.Object.Destroy(session.ProjectileTemplateRoot);
            session.ProjectileTemplateRoot = null;
            session.ProjectileTemplates.Clear();
            session.ProjectileVisualConfigs.Clear();
        }
    }
}
