using UnityEngine;

namespace ET.Client
{
    public static class BrawlMovementInputHelper
    {
        public static Vector2 Read(BrawlTrainingSession session)
        {
            if (!Application.isFocused) return Vector2.zero;
            Vector2 keyboard = new(
                (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
                (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0));
            Vector2 direction = keyboard.sqrMagnitude > 0 ? keyboard.normalized : session.JoystickDirection;
            direction = BrawlCameraInputHelper.ToWorld(session.BattleCamera, direction);
            if (direction.sqrMagnitude > 0) session.LastMoveDirection = direction.normalized;
            return direction;
        }

        public static Vector2 ClickDirection(BrawlTrainingSession session)
        {
            // 直接采样松手时输入，不依赖上一网络帧，也不读取人物面向。
            Vector2 direction = Read(session);
            return direction.sqrMagnitude > 0 ? direction.normalized : session.LastMoveDirection;
        }
    }
}
