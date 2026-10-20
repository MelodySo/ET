using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ET.Client
{
    /// <summary>同一空间块、网格、子网格与绘制状态的一批静态地形实例。</summary>
    [EnableClass]
    public sealed class BrawlTerrainBatch
    {
        public Mesh Mesh;
        public Material Material;
        public int SubMesh;
        public int Layer;
        public ShadowCastingMode Shadows;
        public bool ReceiveShadows;
        public LightProbeUsage Probes;
        public readonly List<Matrix4x4> Matrices = new();
        public readonly List<int> Owners = new();
    }
}
