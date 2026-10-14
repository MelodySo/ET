using Unity.Mathematics;

namespace ET.Test
{
    public class Statesync_BrawlBlockGeometry_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;
            float2 center = new float2(2, 3);
            const float radius = 0.3f;
            // 大位移横跨整个障碍，验证扫掠路径而非只判断终点。
            if (!Near(BrawlBlockGeometryHelper.ClampAxis(new float2(0, 3), 5, radius, center, 0.6f, 0.3f, true), 1.4f))
                return Fail(100101, "Box left sweep failed.");
            if (!Near(BrawlBlockGeometryHelper.ClampAxis(new float2(4, 3), -5, radius, center, 0.6f, 0.3f, true), 2.6f))
                return Fail(100102, "Box right sweep failed.");
            if (!Near(BrawlBlockGeometryHelper.ClampAxis(new float2(2, 1), 5, radius, center, 0.6f, 0.3f, false), 2.55f))
                return Fail(100103, "Box bottom sweep failed.");
            if (!Near(BrawlBlockGeometryHelper.ClampAxis(new float2(2, 5), -5, radius, center, 0.6f, 0.3f, false), 3.45f))
                return Fail(100104, "Box top sweep failed.");
            if (!Near(BrawlBlockGeometryHelper.ClampAxis(new float2(0, 3.6f), 5, radius, center, 0.6f, 0, true), 5))
                return Fail(100105, "Cylinder tangency should allow passage.");
            if (!Near(BrawlBlockGeometryHelper.ClampAxis(new float2(0, 3), 5, radius, center, 0.6f, 0, true), 1.4f))
                return Fail(100106, "Cylinder left sweep failed.");
            if (!Near(BrawlBlockGeometryHelper.ClampAxis(new float2(4, 3), -5, radius, center, 0.6f, 0, true), 2.6f))
                return Fail(100107, "Cylinder right sweep failed.");
            if (!Near(BrawlBlockGeometryHelper.ClampAxis(new float2(2, 1), 5, radius, center, 0.6f, 0, false), 2.4f))
                return Fail(100108, "Cylinder bottom sweep failed.");
            if (!Near(BrawlBlockGeometryHelper.ClampAxis(new float2(2, 5), -5, radius, center, 0.6f, 0, false), 3.6f))
                return Fail(100109, "Cylinder top sweep failed.");
            if (!BrawlBlockGeometryHelper.Overlaps(center, radius, center, 0.6f, 0.3f) ||
                BrawlBlockGeometryHelper.Overlaps(new float2(1.4f, 3), radius, center, 0.6f, 0.3f))
                return Fail(100110, "Box overlap/tangency failed.");
            if (!BrawlBlockGeometryHelper.Overlaps(center, radius, center, 0.6f, 0) ||
                BrawlBlockGeometryHelper.Overlaps(new float2(1.4f, 3), radius, center, 0.6f, 0))
                return Fail(100111, "Cylinder overlap/tangency failed.");
            if (!Near(BrawlBlockGeometryHelper.ClampAxis(new float2(1.4f, 3), 0.1f, radius, center, 0.6f, 0.3f, false), 3.1f))
                return Fail(100112, "Box side sliding failed.");
            if (!Near(BrawlBlockGeometryHelper.ClampAxis(new float2(0, 3.3f), 5, radius, center, 0.6f, 0, true), 2 - math.sqrt(0.27f)))
                return Fail(100113, "Cylinder curved boundary failed.");
            float2 touching = new float2(2 - math.sqrt(0.27f) + 0.000012f, 3.3f);
            if (!Near(BrawlBlockGeometryHelper.ClampAxis(touching, 5, radius, center, 0.6f, 0, true), touching.x))
                return Fail(100114, "Contact tolerance allowed inward penetration.");
            return ErrorCode.ERR_Success;
        }

        private static bool Near(float actual, float expected) => math.abs(actual - expected) < 0.0001f;

        private static int Fail(int code, string message)
        {
            Log.Console(message);
            return code;
        }
    }
}
