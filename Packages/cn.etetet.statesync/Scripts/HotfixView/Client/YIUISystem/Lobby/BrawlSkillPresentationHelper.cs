using UnityEngine;
using UnityEngine.Rendering;

namespace ET.Client
{
    /// <summary>步骤表现的独立消费者。动画或模型缺失不影响权威技能结算。</summary>
    public static class BrawlSkillPresentationHelper
    {
        private const float ReleaseDuration = 0.35f;
        private const float GroundOffset = 0.06f;

        public static void Consume(BrawlTrainingSession session, BrawlPracticeUnitView unit, BrawlSkillUseNotice notice)
        {
            if (!session.SkillCasts.TryGetValue(notice.UnitId, out BrawlSkillCastView cast))
            {
                cast = new BrawlSkillCastView();
                session.SkillCasts.Add(notice.UnitId, cast);
            }
            if (notice.Phase == BrawlSkillUsePhase.Started)
            {
                if (notice.CastId <= cast.LastCastId) return;
                EndCharge(cast);
                cast.LastCastId = notice.CastId;
                cast.ActiveCastId = notice.CastId;
                BrawlCharacterAnimationHelper.BeginCast(unit?.Animation, notice);
                return;
            }
            if (cast.ActiveCastId != notice.CastId) return;
            if (notice.Phase == BrawlSkillUsePhase.Completed || notice.Phase == BrawlSkillUsePhase.Interrupted)
            {
                EndCharge(cast);
                cast.ActiveCastId = 0;
                BrawlCharacterAnimationHelper.EndCast(unit?.Animation, notice.CastId, notice.Phase == BrawlSkillUsePhase.Interrupted);
                return;
            }
            if (notice.Phase != BrawlSkillUsePhase.StepExecuted || unit?.Model == null ||
                unit.Removed || unit.DisplayDead || unit.DisplaySpawning || !unit.Model.activeInHierarchy) return;
            switch (notice.PresentationId)
            {
                case BrawlSkillPresentationId.GunShot:
                    BrawlCharacterAnimationHelper.PlayAttack(unit.Animation, notice.Slot == BrawlSkillSlot.Super,
                        new Vector3(notice.DirectionX, 0, notice.DirectionZ));
                    break;
                case BrawlSkillPresentationId.WhirlwindCharge:
                    EndCharge(cast);
                    cast.Charge = Circle(session, notice.Radius, unit.Model.transform.position, false);
                    if (cast.Charge != null) cast.Charge.Duration = Mathf.Max(0.001f, notice.DurationMilliseconds / 1000f);
                    break;
                case BrawlSkillPresentationId.WhirlwindRelease:
                    EndCharge(cast);
                    BrawlSkillEffectVisual effect = Circle(session, notice.Radius, unit.Model.transform.position, true);
                    if (effect != null) session.SkillEffects.Add(effect);
                    BrawlCharacterAnimationHelper.PlayWhirlwind(unit.Animation, ReleaseDuration);
                    break;
            }
        }

        public static void Update(BrawlTrainingSession session)
        {
            session.RemovedSkillCasters.Clear();
            foreach (var pair in session.SkillCasts)
            {
                BrawlPracticeUnitView unit = session.PracticeUnits.Find(v => v.UnitId == pair.Key);
                if (unit == null || unit.Removed)
                {
                    EndCharge(pair.Value);
                    session.RemovedSkillCasters.Add(pair.Key);
                    continue;
                }
                if (unit.DisplayDead || unit.DisplaySpawning || unit.Model == null || !unit.Model.activeInHierarchy)
                {
                    EndCharge(pair.Value);
                    continue;
                }
                BrawlSkillEffectVisual charge = pair.Value.Charge;
                if (charge?.Root == null) continue;
                charge.Root.transform.position = unit.Model.transform.position + Vector3.up * GroundOffset;
                float progress = Mathf.Clamp01((Time.unscaledTime - charge.StartedAt) / charge.Duration);
                charge.Properties.SetFloat("_BorderAlpha", Mathf.Lerp(0.55f, 1f, progress));
                charge.Renderer.SetPropertyBlock(charge.Properties);
            }
            foreach (long id in session.RemovedSkillCasters) session.SkillCasts.Remove(id);
            for (int i = session.SkillEffects.Count - 1; i >= 0; --i)
            {
                BrawlSkillEffectVisual effect = session.SkillEffects[i];
                float progress = (Time.unscaledTime - effect.StartedAt) / effect.Duration;
                if (effect.Root == null || progress >= 1)
                {
                    Release(effect);
                    session.SkillEffects.RemoveAt(i);
                    continue;
                }
                // 圆形释放脉冲仅改变视觉亮度，实际范围始终使用服务端步骤半径。
                effect.Properties.SetColor("_Color", new Color(1f, 0.78f, 0.12f, 1 - progress));
                effect.Properties.SetFloat("_FillAlpha", Mathf.Lerp(0.42f, 0, progress));
                effect.Renderer.SetPropertyBlock(effect.Properties);
            }
        }

        public static void Clear(BrawlTrainingSession session)
        {
            foreach (BrawlSkillCastView cast in session.SkillCasts.Values) EndCharge(cast);
            foreach (BrawlSkillEffectVisual effect in session.SkillEffects) Release(effect);
            session.SkillEffects.Clear();
            session.SkillCasts.Clear();
            session.RemovedSkillCasters.Clear();
        }

        private static BrawlSkillEffectVisual Circle(BrawlTrainingSession session, float radius, Vector3 position, bool released)
        {
            // 复用已加载的自有范围材质，不创建全局材质或依赖 Shader.Find 的打包保留规则。
            if (!float.IsFinite(radius) || radius <= 0 ||
                !session.SkillAimVisuals.TryGetValue(BrawlSkillSlot.NormalAttack, out BrawlSkillAimVisual source) ||
                source.Renderer == null) return null;
            GameObject root = new(released ? "WhirlwindRelease" : "WhirlwindCharge");
            root.layer = source.Root.layer;
            root.transform.SetParent(session.RuntimeRoot.transform, false);
            root.transform.position = position + Vector3.up * GroundOffset;
            Mesh mesh = new() { name = root.name + "_Mesh" };
            mesh.vertices = new[]
            {
                new Vector3(-radius, 0, -radius), new Vector3(radius, 0, -radius),
                new Vector3(-radius, 0, radius), new Vector3(radius, 0, radius),
            };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            mesh.RecalculateBounds();
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = source.Renderer.sharedMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            MaterialPropertyBlock properties = new();
            properties.SetFloat("_Shape", (int)BrawlSkillRangeType.Circle);
            properties.SetFloat("_Range", radius);
            properties.SetFloat("_BorderWidth", released ? 0.2f : 0.08f);
            properties.SetFloat("_FillAlpha", released ? 0.42f : 0);
            properties.SetFloat("_BorderAlpha", 1);
            properties.SetColor("_Color", new Color(1f, 0.78f, 0.12f, 1f));
            renderer.SetPropertyBlock(properties);
            return new BrawlSkillEffectVisual
            {
                Root = root, Mesh = mesh, Renderer = renderer, Properties = properties,
                StartedAt = Time.unscaledTime, Duration = ReleaseDuration,
            };
        }

        private static void EndCharge(BrawlSkillCastView cast)
        {
            Release(cast.Charge);
            cast.Charge = null;
        }

        private static void Release(BrawlSkillEffectVisual effect)
        {
            if (effect == null) return;
            if (effect.Root != null) UnityEngine.Object.Destroy(effect.Root);
            if (effect.Mesh != null) UnityEngine.Object.Destroy(effect.Mesh);
        }
    }
}
