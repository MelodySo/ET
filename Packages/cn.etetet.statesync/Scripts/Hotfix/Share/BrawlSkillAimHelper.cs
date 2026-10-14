using Unity.Mathematics;

namespace ET
{
    public static class BrawlSkillAimHelper
    {
        public static void Begin(BrawlSkillAimState state)
        {
            Reset(state);
            state.Active = true;
        }

        public static void Sample(BrawlSkillAimState state, float2 offset, float deadZoneRadius)
        {
            if (!state.Active) return;
            bool outside = math.all(math.isfinite(offset)) && math.lengthsq(offset) > deadZoneRadius * deadZoneRadius;
            state.Direction = outside ? math.normalizesafe(offset) : float2.zero;
            state.EverOutsideDeadZone |= outside;
        }

        /// <summary>零向量表示取消。未出死区用移动方向；出过死区后回到里面则取消。</summary>
        public static float2 Release(BrawlSkillAimState state, float2 moveDirection)
        {
            if (!state.Active) return float2.zero;
            float2 direction = state.EverOutsideDeadZone ? state.Direction : math.normalizesafe(moveDirection);
            Reset(state);
            return direction;
        }

        public static void Reset(BrawlSkillAimState state)
        {
            state.Active = false;
            state.EverOutsideDeadZone = false;
            state.Direction = float2.zero;
        }
    }
}
