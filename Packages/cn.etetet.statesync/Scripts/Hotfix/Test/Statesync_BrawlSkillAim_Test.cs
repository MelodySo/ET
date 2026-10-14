using Unity.Mathematics;

namespace ET.Test
{
    public class Statesync_BrawlSkillAim_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;
            BrawlSkillAimState state = new();
            float2 north = new(0, 1);
            float2 east = new(1, 0);
            BrawlSkillAimHelper.Begin(state);
            if (!Same(BrawlSkillAimHelper.Release(state, north), north)) return Fail(100401, "Click must use movement direction.");
            BrawlSkillAimHelper.Begin(state);
            BrawlSkillAimHelper.Sample(state, new float2(12, 0), 12);
            if (state.EverOutsideDeadZone || !Same(BrawlSkillAimHelper.Release(state, north), north))
                return Fail(100402, "Dead-zone boundary must behave like click.");
            BrawlSkillAimHelper.Begin(state);
            BrawlSkillAimHelper.Sample(state, new float2(12.01f, 0), 12);
            if (!state.EverOutsideDeadZone || !Same(BrawlSkillAimHelper.Release(state, north), east))
                return Fail(100403, "Dragging outside must use aim, not movement.");
            BrawlSkillAimHelper.Begin(state);
            BrawlSkillAimHelper.Sample(state, new float2(30, 0), 12);
            BrawlSkillAimHelper.Sample(state, new float2(5, 0), 12);
            if (!state.EverOutsideDeadZone || !Same(state.Direction, float2.zero) ||
                !Same(BrawlSkillAimHelper.Release(state, north), float2.zero))
                return Fail(100404, "Returning to the dead zone must hide aim and cancel release.");
            BrawlSkillAimHelper.Begin(state);
            BrawlSkillAimHelper.Sample(state, new float2(30, 0), 12);
            BrawlSkillAimHelper.Sample(state, float2.zero, 12);
            BrawlSkillAimHelper.Sample(state, new float2(0, -30), 12);
            if (!Same(BrawlSkillAimHelper.Release(state, north), -north))
                return Fail(100405, "Leaving the dead zone again must restore aiming.");
            if (!Same(BrawlSkillAimHelper.Release(state, north), float2.zero))
                return Fail(100406, "Duplicate release must not cast again.");
            BrawlSkillAimHelper.Begin(state);
            BrawlSkillAimHelper.Sample(state, new float2(50, 50), 12);
            if (math.abs(math.length(state.Direction) - 1) > 0.0001f)
                return Fail(100407, "Aim direction must be normalized independently of drag distance.");
            BrawlSkillAimHelper.Reset(state);
            if (state.Active || state.EverOutsideDeadZone || !Same(BrawlSkillAimHelper.Release(state, east), float2.zero))
                return Fail(100408, "Cancellation must clear the entire gesture.");
            BrawlSkillAimHelper.Begin(state);
            if (!Same(BrawlSkillAimHelper.Release(state, east), east))
                return Fail(100409, "Previous cancellation must not affect a new click.");
            BrawlSkillAimState other = new();
            BrawlSkillAimHelper.Begin(state);
            BrawlSkillAimHelper.Begin(other);
            BrawlSkillAimHelper.Sample(state, new float2(30, 0), 12);
            if (other.EverOutsideDeadZone || !Same(BrawlSkillAimHelper.Release(other, north), north) ||
                !Same(BrawlSkillAimHelper.Release(state, north), east))
                return Fail(100410, "Two skill gestures must have independent state.");
            return ErrorCode.ERR_Success;
        }

        private static bool Same(float2 actual, float2 expected) => math.lengthsq(actual - expected) < 0.00000001f;

        private static int Fail(int code, string message)
        {
            Log.Console(message);
            return code;
        }
    }
}
