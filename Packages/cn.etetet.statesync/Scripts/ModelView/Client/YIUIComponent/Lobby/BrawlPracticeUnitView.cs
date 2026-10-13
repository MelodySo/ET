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
        public int TeamId;
        public Vector3 Position;
        public string ModelPath;
        public GameObject Model;
        public readonly List<BrawlPositionSample> Samples = new();
    }
}
