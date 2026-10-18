using Unity.Mathematics;
namespace ET
{
    /// <summary>XZ有限矩形走廊的相交查询，宽度与弹丸尺寸无关。</summary>
    public static class BrawlAISightGeometryHelper
    {
        public static bool Rectangle(float2 start, float2 end, float width, float2 center, float2 halfSize)
        {
            float2 delta = end - start;
            float length = math.length(delta);
            float2 forward = math.normalizesafe(delta, new float2(0, 1));
            float2 right = new float2(forward.y, -forward.x);
            float2 difference = center - (start + end) / 2;
            float halfLength = length / 2;
            float halfWidth = width / 2;
            // 两个矩形的四个分离轴：视线长/短轴、世界X/Z。
            return math.abs(math.dot(difference, forward)) <= halfLength + math.dot(halfSize, math.abs(forward)) &&
                math.abs(math.dot(difference, right)) <= halfWidth + math.dot(halfSize, math.abs(right)) &&
                math.abs(difference.x) <= halfSize.x + math.abs(forward.x) * halfLength + math.abs(right.x) * halfWidth &&
                math.abs(difference.y) <= halfSize.y + math.abs(forward.y) * halfLength + math.abs(right.y) * halfWidth;
        }

        public static bool Circle(float2 start, float2 end, float width, float2 center, float radius)
        {
            float2 forward = math.normalizesafe(end - start, new float2(0, 1));
            float2 right = new float2(forward.y, -forward.x);
            float2 difference = center - (start + end) / 2;
            float2 local = new float2(math.dot(difference, forward), math.dot(difference, right));
            float2 half = new float2(math.distance(start, end), width) / 2;
            return math.lengthsq(local - math.clamp(local, -half, half)) <= radius * radius;
        }
    }
}
