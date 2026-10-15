using System.Collections.Generic;
using Unity.Mathematics;
namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public sealed class BrawlAIComponent : Entity, IAwake, IDestroy
    {
        public readonly List<float2> Path = new();
        public int Waypoint;
        public long TargetId;
        public long PlannedTargetId;
        public long Now;
        public long NextPlanAt;
        public int LevelId;
        public float CellSize;
        public bool Chasing;
        public int Plans;
    }
}