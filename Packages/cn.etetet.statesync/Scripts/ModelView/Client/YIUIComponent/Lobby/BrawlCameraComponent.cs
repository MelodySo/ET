using UnityEngine;

namespace ET.Client
{
    /// <summary>所有Brawl玩法共用的固定角度透视相机。目标由玩法入口绑定，不依赖具体模式。</summary>
    [ComponentOf(typeof(Scene))]
    public sealed class BrawlCameraComponent : Entity, IAwake<Camera, BrawlGlobalConfigCategory>, ILateUpdate, IDestroy
    {
        public Camera Camera;
        public Transform Target;
        public Quaternion Rotation;
        public float Distance;
        public float TargetHeight;
    }
}
