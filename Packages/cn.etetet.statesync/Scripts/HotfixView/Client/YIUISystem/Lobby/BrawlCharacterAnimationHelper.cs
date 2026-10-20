using System;
using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    /// <summary>仅消费显示位移。瞄准输入与权威状态不在本类结算。</summary>
    public static class BrawlCharacterAnimationHelper
    {
        // 纯视觉调校，单位分别为米/秒、秒和度/秒。
        private const float IdleSpeed = 0.02f;
        private const float BlendTime = 0.08f;
        private const float TurnSpeed = 720f;
        private const float MaxSampleInterval = 0.25f;

        public static void Bind(BrawlTrainingSession session, GameObject model)
        {
            foreach (Animator animator in model.GetComponentsInChildren<Animator>(true)) animator.applyRootMotion = false;
            // 武器也可能有 Animator；只绑定拥有角色三层契约的那一个。
            Animator character = null;
            foreach (Animator animator in model.GetComponentsInChildren<Animator>(true))
            {
                if (animator.runtimeAnimatorController is AnimatorOverrideController &&
                    animator.GetLayerIndex("Locomotion") == 0 && animator.GetLayerIndex("UpperBody") == 1 &&
                    animator.GetLayerIndex("FullBody") == 2)
                {
                    character = animator;
                    break;
                }
            }
            if (character == null) return; // 墙桶、粒子、非角色模型不需要角色表现。
            AnimatorOverrideController template = (AnimatorOverrideController)character.runtimeAnimatorController;
            List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new(template.overridesCount);
            template.GetOverrides(overrides);
            foreach (var pair in overrides)
                if (pair.Key == null || !pair.Key.name.StartsWith("BrawlSlot_", StringComparison.Ordinal))
                    throw new InvalidOperationException($"角色动画替换键不是 BrawlSlot: {model.name}");
            AnimatorOverrideController runtime = new(template.runtimeAnimatorController);
            runtime.name = $"{model.name}_Runtime";
            runtime.ApplyOverrides(overrides);
            BrawlCharacterAnimationView view = new()
            {
                Model = model, Animator = character, Template = template, RuntimeController = runtime,
                FacingDirection = Vector3.forward, ModelRotationOffset = model.transform.rotation,
            };
            session.CharacterAnimations.Add(model, view);
            foreach (var pair in overrides)
            {
                if (!pair.Key.name.StartsWith("BrawlSlot_Move", StringComparison.Ordinal)) continue;
                Vector3 velocity = pair.Value.averageSpeed;
                velocity.y = 0;
                if (velocity.sqrMagnitude <= 0.000001f || float.IsNaN(velocity.sqrMagnitude) || float.IsInfinity(velocity.sqrMagnitude))
                    throw new InvalidOperationException($"{model.name}/{pair.Key.name} 没有有效根位移速度，请重新生成角色移动映射。");
                view.MotionVelocities[pair.Value] = velocity;
                switch (pair.Key.name)
                {
                    case "BrawlSlot_MoveForward": view.ForwardVelocity = velocity; break;
                    case "BrawlSlot_MoveBackward": view.BackwardVelocity = velocity; break;
                    case "BrawlSlot_MoveLeft": view.LeftVelocity = velocity; break;
                    case "BrawlSlot_MoveRight": view.RightVelocity = velocity; break;
                }
            }
            character.runtimeAnimatorController = runtime;
            // 购买包 AnimationEvent 不接入战斗反馈，本轮不播放枪口特效或声音。
            character.fireEvents = false;
            character.updateMode = AnimatorUpdateMode.UnscaledTime;
            Reset(view);
        }

        public static void Reset(BrawlCharacterAnimationView view)
        {
            if (view == null) return;
            ResetSampling(view);
            view.LifeInitialized = false;
            view.ActiveCastId = 0;
            view.LastCastId = 0;
            view.Dead = false;
            view.Spawning = false;
            view.UpperActionPlaying = false;
            view.SpawnActionPlaying = false;
            view.FacingLocked = false;
            if (view.Animator == null) return;
            Animator animator = view.Animator;
            view.WasActive = animator.isActiveAndEnabled;
            animator.SetLayerWeight(0, 1);
            ClearActionLayers(view);
            animator.SetFloat("UpperActionSpeed", 1);
            animator.SetFloat("FullBodyActionSpeed", 1);
            if (animator.isActiveAndEnabled) animator.Play("Locomotion.Move", 0, 0);
        }

        public static void ResetSampling(BrawlCharacterAnimationView view)
        {
            if (view == null) return;
            view.HasPositionSample = false;
            view.DisplayVelocity = Vector3.zero;
            view.NativeMoveSpeed = 0;
            view.PlaybackSpeed = 1;
            view.MotionClips.Clear();
            if (view.Animator == null) return;
            Animator animator = view.Animator;
            animator.SetFloat("MoveX", 0);
            animator.SetFloat("MoveZ", 0);
            animator.SetFloat("LocomotionSpeed", 1);
        }

        private static void ClearActionLayers(BrawlCharacterAnimationView view)
        {
            StopFullAction(view);
            Animator animator = view.Animator;
            animator.SetLayerWeight(1, 0);
            animator.SetLayerWeight(2, 0);
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                if (parameter.type == AnimatorControllerParameterType.Trigger) animator.ResetTrigger(parameter.nameHash);
            view.UpperActionPlaying = false;
            view.SpawnActionPlaying = false;
            if (animator.isActiveAndEnabled)
            {
                animator.Play("UpperBody.Empty", 1, 0);
                animator.Play("FullBody.Empty", 2, 0);
            }
        }

        public static void UpdateLife(BrawlPracticeUnitView unit, double renderTick)
        {
            while (unit.AnimationLifeSamples.Count > 0 && unit.AnimationLifeSamples[0].Tick <= renderTick)
            {
                var sample = unit.AnimationLifeSamples[0];
                unit.DisplayDead = sample.Dead;
                unit.DisplaySpawning = sample.Spawning;
                unit.AnimationLifeSamples.RemoveAt(0);
            }
            BrawlCharacterAnimationView view = unit.Animation;
            if (view == null || view.Animator == null || !view.Animator.isActiveAndEnabled) return;
            Animator animator = view.Animator;
            if (view.FullActionPlaying && Time.unscaledTime - view.FullActionStartedAt >= view.FullActionDuration)
                StopFullAction(view);
            bool dead = unit.DisplayDead;
            bool spawning = !dead && unit.DisplaySpawning;
            if (!view.LifeInitialized || view.Dead != dead || view.Spawning != spawning)
            {
                ClearActionLayers(view);
                ResetSampling(view);
                view.FacingLocked = false;
                view.ActiveCastId = 0;
                view.LifeInitialized = true;
                view.Dead = dead;
                view.Spawning = spawning;
                animator.SetFloat("FullBodyActionSpeed", 1);
                if (dead || spawning)
                {
                    animator.SetLayerWeight(2, 1);
                    animator.Play(dead ? "FullBody.FullDeath" : "FullBody.FullSpawn", 2, 0);
                    view.SpawnActionPlaying = spawning;
                }
                return;
            }
            // Empty 无 Motion、Write Defaults Off，必须主动关闭覆盖层。
            if (view.SpawnActionPlaying && animator.GetCurrentAnimatorStateInfo(2).IsName("FullBody.Empty") && !animator.IsInTransition(2))
            {
                animator.SetLayerWeight(2, 0);
                view.SpawnActionPlaying = false;
            }
            if (view.UpperActionPlaying && Time.frameCount > view.UpperActionStartedFrame &&
                animator.GetCurrentAnimatorStateInfo(1).IsName("UpperBody.Empty") && !animator.IsInTransition(1))
            {
                animator.SetLayerWeight(1, 0);
                view.UpperActionPlaying = false;
            }
        }

        /// <summary>只供已确认的成功施法事件调用；不从摇杆或弹丸数量推断攻击。</summary>
        public static void PlayAttack(BrawlCharacterAnimationView view, bool super, Vector3 direction)
        {
            if (view == null || view.Animator == null || !view.Animator.isActiveAndEnabled ||
                !view.LifeInitialized || view.Dead || view.Spawning) return;
            foreach (AnimatorControllerParameter parameter in view.Animator.parameters)
                if (parameter.type == AnimatorControllerParameterType.Trigger) view.Animator.ResetTrigger(parameter.nameHash);
            SetFacing(view, direction, view.FacingLocked);
            view.Model.transform.rotation = Quaternion.LookRotation(view.FacingDirection, Vector3.up) * view.ModelRotationOffset;
            view.Animator.SetFloat("UpperActionSpeed", 1);
            view.Animator.SetLayerWeight(1, 1);
            view.Animator.Play(super ? "UpperBody.UpperSuperAttack" : "UpperBody.UpperNormalAttack", 1, 0);
            view.UpperActionPlaying = true;
            view.UpperActionStartedFrame = Time.frameCount;
            RestoreMovementFacing(view);
        }

        public static void BeginCast(BrawlCharacterAnimationView view, BrawlSkillUseNotice notice)
        {
            if (view == null || !view.LifeInitialized || view.Dead || view.Spawning || notice.CastId <= view.LastCastId) return;
            view.LastCastId = notice.CastId;
            view.ActiveCastId = notice.CastId;
            view.FacingLocked = notice.LockFacing;
            if (notice.FaceDirection)
                SetFacing(view, new Vector3(notice.DirectionX, 0, notice.DirectionZ), notice.LockFacing);
        }

        public static void EndCast(BrawlCharacterAnimationView view, long castId, bool interrupted)
        {
            if (view == null || view.ActiveCastId != castId) return;
            view.ActiveCastId = 0;
            view.FacingLocked = false;
            if (interrupted && view.Animator != null)
            {
                StopFullAction(view);
                view.Animator.SetLayerWeight(1, 0);
                foreach (AnimatorControllerParameter parameter in view.Animator.parameters)
                    if (parameter.type == AnimatorControllerParameterType.Trigger) view.Animator.ResetTrigger(parameter.nameHash);
                if (view.Animator.isActiveAndEnabled) view.Animator.Play("UpperBody.Empty", 1, 0);
                view.UpperActionPlaying = false;
            }
            RestoreMovementFacing(view);
        }

        /// <summary>临时旋风动作：原空手攻击 Clip + 显示旋转，不修改根位移或权威面向。</summary>
        public static void PlayWhirlwind(BrawlCharacterAnimationView view, float duration)
        {
            if (view == null || view.Animator == null || !view.Animator.isActiveAndEnabled ||
                !view.LifeInitialized || view.Dead || view.Spawning) return;
            if (!view.WhirlwindMapped)
            {
                List<KeyValuePair<AnimationClip, AnimationClip>> pairs = new();
                view.RuntimeController.GetOverrides(pairs);
                AnimationClip attack = null;
                foreach (var pair in pairs)
                    if (pair.Key.name == "BrawlSlot_UpperNormalAttack") attack = pair.Value;
                if (attack == null) return;
                for (int i = 0; i < pairs.Count; ++i)
                    if (pairs[i].Key.name == "BrawlSlot_FullHero01")
                        pairs[i] = new KeyValuePair<AnimationClip, AnimationClip>(pairs[i].Key, attack);
                view.RuntimeController.ApplyOverrides(pairs);
                view.WhirlwindClipLength = attack.length;
                view.WhirlwindMapped = true;
            }
            ClearActionLayers(view);
            view.FullActionPlaying = true;
            view.FullActionStartedAt = Time.unscaledTime;
            view.FullActionDuration = duration;
            view.FullActionBaseRotation = view.Model.transform.rotation;
            view.Animator.SetFloat("FullBodyActionSpeed", view.WhirlwindClipLength / duration);
            view.Animator.SetLayerWeight(2, 1);
            view.Animator.Play("FullBody.FullHero01", 2, 0);
        }

        private static void StopFullAction(BrawlCharacterAnimationView view)
        {
            if (!view.FullActionPlaying) return;
            view.FullActionPlaying = false;
            if (view.Model != null) view.Model.transform.rotation = view.FullActionBaseRotation;
            if (view.Animator == null) return;
            view.Animator.SetLayerWeight(2, 0);
            view.Animator.SetFloat("FullBodyActionSpeed", 1);
            if (view.Animator.isActiveAndEnabled) view.Animator.Play("FullBody.Empty", 2, 0);
        }

        private static void RestoreMovementFacing(BrawlCharacterAnimationView view)
        {
            if (view.FacingLocked || view.Dead || view.Spawning || view.DisplayVelocity.sqrMagnitude <= IdleSpeed * IdleSpeed || view.Model == null) return;
            view.FacingDirection = view.DisplayVelocity.normalized;
            view.Model.transform.rotation = Quaternion.LookRotation(view.FacingDirection, Vector3.up) * view.ModelRotationOffset;
        }

        /// <summary>未来仅由成功施法表现事件调用，技能摇杆准备阶段不能调用。</summary>
        public static void SetFacing(BrawlCharacterAnimationView view, Vector3 direction, bool locked)
        {
            direction.y = 0;
            if (view == null || float.IsNaN(direction.x) || float.IsInfinity(direction.x) || float.IsNaN(direction.z) || float.IsInfinity(direction.z) || direction.sqrMagnitude == 0) return;
            view.FacingDirection = direction.normalized;
            view.FacingLocked = locked;
        }

        public static void Update(BrawlCharacterAnimationView view, float delta)
        {
            if (view == null || view.Animator == null || view.Model == null) return;
            bool active = view.Animator.isActiveAndEnabled;
            if (active != view.WasActive)
            {
                ResetSampling(view);
                view.WasActive = active;
                view.LifeInitialized = false;
            }
            if (!active) return;
            Vector3 position = view.Model.transform.position;
            if (!view.HasPositionSample || delta <= 0 || delta > MaxSampleInterval ||
                Vector3.Distance(position, view.PreviousPosition) > view.PositionResetSpeed * MaxSampleInterval)
            {
                ResetSampling(view);
                view.PreviousPosition = position;
                view.HasPositionSample = true;
                return;
            }
            Vector3 velocity = (position - view.PreviousPosition) / delta;
            velocity.y = 0;
            view.PreviousPosition = position;
            view.DisplayVelocity = velocity;
            float speed = velocity.magnitude;
            bool moving = !view.Dead && !view.Spawning && speed > IdleSpeed;
            if (moving && !view.FacingLocked) view.FacingDirection = velocity / speed;
            Quaternion facing = Quaternion.LookRotation(view.FacingDirection, Vector3.up);
            view.Model.transform.rotation = Quaternion.RotateTowards(view.FullActionPlaying ? view.FullActionBaseRotation : view.Model.transform.rotation,
                facing * view.ModelRotationOffset, TurnSpeed * delta);
            // 去掉美术模型原始旋转偏移，得到逻辑朝向坐标系。
            Quaternion displayFacing = view.Model.transform.rotation * Quaternion.Inverse(view.ModelRotationOffset);
            Vector3 local = moving ? Quaternion.Inverse(displayFacing) * (velocity / speed) : Vector3.zero;
            view.Animator.SetFloat("MoveX", local.x, BlendTime, delta);
            view.Animator.SetFloat("MoveZ", local.z, BlendTime, delta);
            view.NativeMoveSpeed = moving ? GetNativeMoveSpeed(view, local) : 0;
            view.PlaybackSpeed = moving && view.NativeMoveSpeed > IdleSpeed ? speed / view.NativeMoveSpeed : 1;
            view.Animator.SetFloat("LocomotionSpeed", view.PlaybackSpeed);
            if (view.FullActionPlaying)
            {
                view.FullActionBaseRotation = view.Model.transform.rotation;
                float progress = Mathf.Clamp01((Time.unscaledTime - view.FullActionStartedAt) / view.FullActionDuration);
                view.Model.transform.rotation = displayFacing * Quaternion.Euler(0, progress * 360, 0) * view.ModelRotationOffset;
            }
        }

        private static float GetNativeMoveSpeed(BrawlCharacterAnimationView view, Vector3 direction)
        {
            // 读取实际混合权重，但使用 Clip 在倍率1时的速度，避免把已应用倍率重复除回。
            view.Animator.GetCurrentAnimatorClipInfo(0, view.MotionClips);
            Vector3 velocity = Vector3.zero;
            float weight = 0;
            foreach (AnimatorClipInfo info in view.MotionClips)
            {
                if (!view.MotionVelocities.TryGetValue(info.clip, out Vector3 motion)) continue;
                velocity += motion * info.weight;
                weight += info.weight;
            }
            // Idle 不参与归一化，起步阶段不因其权重大而把分母压到接近0。
            if (weight > 0.0001f) velocity /= weight;
            if (weight <= 0.0001f || velocity.sqrMagnitude <= 0.0001f)
            {
                float x = Mathf.Abs(direction.x);
                float z = Mathf.Abs(direction.z);
                velocity = ((direction.x >= 0 ? view.RightVelocity : view.LeftVelocity) * x +
                    (direction.z >= 0 ? view.ForwardVelocity : view.BackwardVelocity) * z) / Mathf.Max(x + z, 0.0001f);
            }
            // Humanoid 根位移以 Avatar 的 humanScale 还原，再经过实例 Transform 缩放转成米/秒。
            float avatarScale = view.Animator.isHuman ? view.Animator.humanScale : 1;
            Vector3 worldVelocity = view.Animator.transform.TransformVector(velocity * avatarScale);
            worldVelocity.y = 0;
            return worldVelocity.magnitude;
        }
        public static void Release(BrawlTrainingSession session, GameObject model)
        {
            if (ReferenceEquals(model, null) || !session.CharacterAnimations.Remove(model, out BrawlCharacterAnimationView view)) return;
            if (view.Animator != null) view.Animator.runtimeAnimatorController = view.Template;
            UnityEngine.Object.Destroy(view.RuntimeController);
        }

        public static void Clear(BrawlTrainingSession session)
        {
            foreach (BrawlCharacterAnimationView view in session.CharacterAnimations.Values)
            {
                if (view.Animator != null) view.Animator.runtimeAnimatorController = view.Template;
                if (view.RuntimeController != null) UnityEngine.Object.Destroy(view.RuntimeController);
            }
            session.CharacterAnimations.Clear();
        }
    }
}
