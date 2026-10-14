using Unity.Mathematics;

namespace ET
{
    /// <summary>英雄圆形与轴对齐 NPC 矩形/圆柱的 XZ 几何，不依赖 Unity 物理或模型。</summary>
    public static class BrawlBlockGeometryHelper
    {
        public static bool Overlaps(float2 position, float radius, float2 center, float length, float width)
        {
            float tolerance = BrawlMovementSettings.ContactTolerance;
            if (width == 0)
            {
                float combined = math.max(0, radius + length * 0.5f - tolerance);
                return math.lengthsq(position - center) < combined * combined;
            }
            float2 half = new float2(length, width) * 0.5f;
            float2 nearest = math.clamp(position, center - half, center + half);
            float allowedRadius = math.max(0, radius - tolerance);
            return math.lengthsq(position - nearest) < allowedRadius * allowedRadius;
        }

        public static float ClampAxis(float2 position, float delta, float radius, float2 center, float length, float width, bool xAxis)
        {
            float current = xAxis ? position.x : position.y;
            if (delta == 0) return current;
            float perpendicularDistance = math.abs(xAxis ? position.y - center.y : position.x - center.x);
            float axisHalf;
            float reach;
            float tolerance = BrawlMovementSettings.ContactTolerance;
            if (width == 0)
            {
                float combined = radius + length * 0.5f;
                if (perpendicularDistance >= combined - tolerance) return current + delta;
                axisHalf = 0;
                reach = math.sqrt(math.max(0, combined * combined - perpendicularDistance * perpendicularDistance));
            }
            else
            {
                float otherHalf = (xAxis ? width : length) * 0.5f;
                float distance = math.max(0, perpendicularDistance - otherHalf);
                if (distance >= radius - tolerance) return current + delta;
                axisHalf = (xAxis ? length : width) * 0.5f;
                reach = math.sqrt(math.max(0, radius * radius - distance * distance));
            }
            float axisCenter = xAxis ? center.x : center.y;
            float near = axisCenter - axisHalf - reach;
            float far = axisCenter + axisHalf + reach;
            float target = current + delta;
            if (delta > 0 && current <= near + tolerance) return math.min(target, math.max(current, near));
            if (delta < 0 && current >= far - tolerance) return math.max(target, math.min(current, far));
            // 出生检查允许接触容差；若因此已轻微嵌入，只允许朝近侧退出，不能继续穿向另一侧。
            if (delta > 0 && current < axisCenter) return current;
            if (delta < 0 && current > axisCenter) return current;
            return target;
        }
    }
}
