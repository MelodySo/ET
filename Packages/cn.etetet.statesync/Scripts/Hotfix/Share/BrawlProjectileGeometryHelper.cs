using Unity.Mathematics;

namespace ET
{
    /// <summary>移动圆与静态圆/轴对齐矩形的精确连续相交。返回本段首次接触比例 [0,1]。</summary>
    public static class BrawlProjectileGeometryHelper
    {
        public static bool Circle(float2 start, float2 delta, float2 center, float radius, out float fraction)
        {
            fraction = 0;
            float2 offset = start - center;
            float c = math.lengthsq(offset) - radius * radius;
            if (c <= 0) return true;
            float a = math.lengthsq(delta);
            if (a <= 0) return false;
            float b = math.dot(offset, delta);
            if (b >= 0) return false;
            float discriminant = b * b - a * c;
            if (discriminant < 0) return false;
            fraction = (-b - math.sqrt(discriminant)) / a;
            return fraction >= 0 && fraction <= 1;
        }

        public static bool Rectangle(float2 start, float2 delta, float2 center, float2 halfSize, float radius, out float fraction)
        {
            float2 min = center - halfSize;
            float2 max = center + halfSize;
            fraction = 0;
            if (math.lengthsq(start - math.clamp(start, min, max)) <= radius * radius) return true;
            float best = float.PositiveInfinity;
            // Minkowski 和为圆角矩形，不能仅扩大 AABB，否则四角会误挡。
            Side(start.x, delta.x, start.y, delta.y, min.x - radius, min.y, max.y, ref best);
            Side(start.x, delta.x, start.y, delta.y, max.x + radius, min.y, max.y, ref best);
            Side(start.y, delta.y, start.x, delta.x, min.y - radius, min.x, max.x, ref best);
            Side(start.y, delta.y, start.x, delta.x, max.y + radius, min.x, max.x, ref best);
            Corner(start, delta, min, radius, ref best);
            Corner(start, delta, new float2(min.x, max.y), radius, ref best);
            Corner(start, delta, new float2(max.x, min.y), radius, ref best);
            Corner(start, delta, max, radius, ref best);
            fraction = best;
            return best <= 1;
        }

        private static void Side(float start, float delta, float otherStart, float otherDelta, float edge,
            float min, float max, ref float best)
        {
            if (delta == 0) return;
            float t = (edge - start) / delta;
            float other = otherStart + otherDelta * t;
            if (t >= 0 && t <= 1 && t < best && other >= min && other <= max) best = t;
        }

        private static void Corner(float2 start, float2 delta, float2 center, float radius, ref float best)
        {
            if (Circle(start, delta, center, radius, out float t)) best = math.min(best, t);
        }
    }
}
