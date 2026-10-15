using UnityEngine;

namespace ET.Client
{
    public static class BrawlCameraInputHelper
    {
        /// <summary>屏幕摇杆方向映射到相机朝向的XZ平面，保留长度供死区判断。</summary>
        public static Vector2 ToWorld(Camera camera, Vector2 direction)
        {
            if (camera == null) return direction;
            Vector3 forward = camera.transform.forward;
            Vector2 up = new Vector2(forward.x, forward.z).normalized;
            Vector2 right = new Vector2(up.y, -up.x);
            return right * direction.x + up * direction.y;
        }
    }
}
