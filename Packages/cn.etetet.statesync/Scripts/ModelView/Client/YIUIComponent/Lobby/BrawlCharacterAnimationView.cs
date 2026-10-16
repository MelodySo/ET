using UnityEngine;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>单个角色视图的动画状态；不参与服务器战斗判定。</summary>
    [EnableClass]
    public sealed class BrawlCharacterAnimationView
    {
        public GameObject Model;
        public Animator Animator;
        public RuntimeAnimatorController Template;
        public AnimatorOverrideController RuntimeController;
        public Vector3 PreviousPosition;
        public Vector3 DisplayVelocity;
        public Vector3 FacingDirection;
        public Quaternion ModelRotationOffset;
        public bool HasPositionSample;
        public bool WasActive;
        public bool FacingLocked;
        // 仅用于识别位置跳变，不再作为动画播放倍率分母。
        public float PositionResetSpeed = 1;
        public readonly Dictionary<AnimationClip, Vector3> MotionVelocities = new();
        public readonly List<AnimatorClipInfo> MotionClips = new(5);
        public Vector3 ForwardVelocity;
        public Vector3 BackwardVelocity;
        public Vector3 LeftVelocity;
        public Vector3 RightVelocity;
        public float NativeMoveSpeed;
        public float PlaybackSpeed = 1;
    }
}
