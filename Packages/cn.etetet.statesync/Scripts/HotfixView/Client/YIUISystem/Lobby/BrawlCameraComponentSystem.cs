using System;
using UnityEngine;

namespace ET.Client
{
    [EntitySystemOf(typeof(BrawlCameraComponent))]
    public static partial class BrawlCameraComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BrawlCameraComponent self, Camera camera, BrawlGlobalConfigCategory config)
        {
            if (!Finite(config.CameraDistance) || config.CameraDistance <= 0 ||
                !Finite(config.CameraPitch) || config.CameraPitch <= 0 || config.CameraPitch >= 90 ||
                !Finite(config.CameraYaw) || !Finite(config.CameraFieldOfView) ||
                config.CameraFieldOfView <= 0 || config.CameraFieldOfView >= 179 || !Finite(config.CameraTargetHeight))
                throw new InvalidOperationException("BrawlGlobal相机参数无效：距离须大于0，俯角在(0,90)，垂直FOV在(0,179)，各值须有限。");
            self.Camera = camera;
            self.Distance = config.CameraDistance;
            self.TargetHeight = config.CameraTargetHeight;
            self.Rotation = Quaternion.Euler(config.CameraPitch, config.CameraYaw, 0);
            camera.orthographic = false;
            camera.fieldOfView = config.CameraFieldOfView;
            camera.transform.rotation = self.Rotation;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = Mathf.Max(100, config.CameraDistance * 10);
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        public static void SetTarget(this BrawlCameraComponent self, Transform target)
        {
            self.Target = target;
        }

        /// <summary>以角色脚下加高度为观察点，距离为相机至观察点的直线距离；不随角色旋转。</summary>
        public static void Follow(this BrawlCameraComponent self)
        {
            // 死亡销毁到重生模型就绪之间目标可以为空，保持最后镜头。
            if (self.Camera == null || self.Target == null) return;
            Vector3 focus = self.Target.position + Vector3.up * self.TargetHeight;
            self.Camera.transform.SetPositionAndRotation(focus - self.Rotation * Vector3.forward * self.Distance, self.Rotation);
        }

        [EntitySystem]
        private static void LateUpdate(this BrawlCameraComponent self)
        {
            self.Follow();
        }

        [EntitySystem]
        private static void Destroy(this BrawlCameraComponent self)
        {
            self.Target = null;
            if (self.Camera != null) UnityEngine.Object.Destroy(self.Camera.gameObject);
            self.Camera = null;
        }
    }
}
