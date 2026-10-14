using UnityEngine;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>服务器单位快照及其本地模型，不承担战斗逻辑。</summary>
    [EnableClass]
    public sealed class BrawlPracticeUnitView
    {
        public long UnitId;
        public int ConfigId;
        public BrawlPracticeUnitKind Kind;
        public BrawlFaction Faction;
        public Vector3 Position;
        public Vector2 MoveDirection;
        public Vector2 FacingDirection;
        public string ModelPath;
        public GameObject Model;
        public readonly List<BrawlPositionSample> Samples = new();
    }
}
