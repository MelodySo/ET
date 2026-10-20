using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ET.Client
{
    /// <summary>资源归属本局会话；不修改共享材质、网格或地图配置。</summary>
    [EnableClass]
    public sealed class BrawlTerrainRenderState
    {
        public bool Instancing = true;
#if UNITY_EDITOR
        // 回调归属本局会话；编辑器摄像机重绘不依赖游戏 Update，暂停时也能查看地形。
        public System.Action<ScriptableRenderContext, Camera> EditorCameraDraw;
#endif
        public readonly Dictionary<Material, Material> Materials = new();
        public readonly Dictionary<int, GameObject> Tiles = new();
        public readonly Dictionary<int, BrawlStaticRenderObject> Objects = new();
        public readonly Dictionary<long, int> UnitOwners = new();
        public readonly Dictionary<(Mesh Mesh, Material Material, int SubMesh, Vector2Int Chunk, int Layer,
            ShadowCastingMode Shadows, bool ReceiveShadows, LightProbeUsage Probes), BrawlTerrainBatch> Batches = new();
    }
}
