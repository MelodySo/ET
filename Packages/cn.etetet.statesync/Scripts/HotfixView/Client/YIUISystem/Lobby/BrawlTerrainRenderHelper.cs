using UnityEngine;
using UnityEngine.Rendering;

namespace ET.Client
{
    /// <summary>合批地形墙、草及静态 NPC；权威格子、碰撞与 NPC 生命周期不受影响。</summary>
    public static class BrawlTerrainRenderHelper
    {
        // 空间分块保持相机/阴影裁剪粒度；不是一次提交整张地图。每批不超过 API 的 1023 实例限制。
        private const int ChunkCells = 8;
        private const int MaxInstances = 1023;

        public static void Register(BrawlTrainingSession session, GameObject tile, int cell, int columns, int tileType)
        {
            if (tile == null) return; // 加载过程中取消会返回空。
            BrawlTerrainRenderState state = session.TerrainRendering;
            state.Tiles.Add(cell, tile);
            if (!SystemInfo.supportsInstancing ||
                (tileType != (int)BrawlTileType.Wall && tileType != (int)BrawlTileType.Grass)) return;
            Vector2Int chunk = new(cell % columns / ChunkCells, cell / columns / ChunkCells);
            RegisterModel(session, tile, chunk);
        }

        public static void RegisterNPC(BrawlTrainingSession session, BrawlPracticeUnitView view)
        {
            if (view.Kind != BrawlPracticeUnitKind.NPC || view.Model == null) return;
            RemoveNPC(session, view.UnitId); // 视图重建时先清理旧实例。
            float chunkSize = session.CellSize * ChunkCells;
            Vector3 position = view.Model.transform.position;
            RegisterModel(session, view.Model, new Vector2Int(Mathf.FloorToInt(position.x / chunkSize), Mathf.FloorToInt(position.z / chunkSize)));
            int owner = view.Model.GetInstanceID();
            if (session.TerrainRendering.Objects.ContainsKey(owner)) session.TerrainRendering.UnitOwners.Add(view.UnitId, owner);
        }

        private static void RegisterModel(BrawlTrainingSession session, GameObject tile, Vector2Int chunk)
        {
            BrawlTerrainRenderState state = session.TerrainRendering;
            // 动画、LOD、脚本驱动和粒子资源保留原路径；这里只接管静态 MeshRenderer。
            if (!SystemInfo.supportsInstancing || tile.GetComponentInChildren<Animator>(true) != null ||
                tile.GetComponentInChildren<Animation>(true) != null || tile.GetComponentInChildren<LODGroup>(true) != null ||
                tile.GetComponentInChildren<MonoBehaviour>(true) != null || tile.GetComponentInChildren<ParticleSystem>(true) != null) return;
            int owner = tile.GetInstanceID();
            BrawlStaticRenderObject entry = new() { Root = tile, Matrix = tile.transform.localToWorldMatrix };
            foreach (MeshRenderer renderer in tile.GetComponentsInChildren<MeshRenderer>())
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (!renderer.enabled || filter == null || filter.sharedMesh == null || renderer.HasPropertyBlock() ||
                    renderer.lightmapIndex >= 0 || renderer.realtimeLightmapIndex >= 0 || renderer.probeAnchor != null ||
                    renderer.lightProbeUsage == LightProbeUsage.UseProxyVolume || renderer.renderingLayerMask != 1 ||
                    renderer.additionalVertexStreams != null || renderer.sortingOrder != 0 || renderer.sortingLayerID != 0) continue;
                Mesh mesh = filter.sharedMesh;
                Material[] materials = renderer.sharedMaterials;
                if (materials.Length != mesh.subMeshCount || !Supported(materials)) continue;
                bool full = false;
                for (int subMesh = 0; subMesh < materials.Length; ++subMesh)
                {
                    var key = (mesh, materials[subMesh], subMesh, chunk, renderer.gameObject.layer,
                        renderer.shadowCastingMode, renderer.receiveShadows, renderer.lightProbeUsage);
                    if (state.Batches.TryGetValue(key, out BrawlTerrainBatch existing) && existing.Matrices.Count >= MaxInstances) full = true;
                }
                if (full) continue;
                for (int subMesh = 0; subMesh < materials.Length; ++subMesh)
                {
                    Material source = materials[subMesh];
                    if (!state.Materials.TryGetValue(source, out Material material))
                    {
                        material = new Material(source) { name = source.name + " (Brawl Terrain Instanced)", enableInstancing = true, hideFlags = HideFlags.DontSave };
                        state.Materials.Add(source, material);
                    }
                    var key = (mesh, source, subMesh, chunk, renderer.gameObject.layer,
                        renderer.shadowCastingMode, renderer.receiveShadows, renderer.lightProbeUsage);
                    if (!state.Batches.TryGetValue(key, out BrawlTerrainBatch batch))
                    {
                        batch = new BrawlTerrainBatch
                        {
                            Mesh = mesh, Material = material, SubMesh = subMesh, Layer = renderer.gameObject.layer,
                            Shadows = renderer.shadowCastingMode, ReceiveShadows = renderer.receiveShadows, Probes = renderer.lightProbeUsage,
                        };
                        state.Batches.Add(key, batch);
                    }
                    batch.Matrices.Add(renderer.localToWorldMatrix);
                    batch.Owners.Add(owner);
                }
                entry.Renderers.Add(renderer);
                renderer.enabled = !state.Instancing;
            }
            if (entry.Renderers.Count > 0) state.Objects.Add(owner, entry);
        }

        private static bool Supported(Material[] materials)
        {
            // 已验证的 URP 静态不透明/裁剪材质；未知 shader 或透明排序不强行合批。
            foreach (Material material in materials)
                if (material == null || !material.shader.isSupported || material.renderQueue > (int)RenderQueue.AlphaTest ||
                    (material.shader.name != "Universal Render Pipeline/Lit" && material.shader.name != "Universal Render Pipeline/Simple Lit" &&
                     material.shader.name != "Universal Render Pipeline/Unlit")) return false;
            return true;
        }

        public static void Draw(BrawlTrainingSession session)
        {
#if UNITY_EDITOR
            BrawlTerrainRenderState state = session.TerrainRendering;
            if (state.EditorCameraDraw == null)
            {
                state.EditorCameraDraw = (_, camera) =>
                {
                    if (camera == session.BattleCamera || camera.cameraType == CameraType.SceneView) Draw(session, camera);
                };
                RenderPipelineManager.beginCameraRendering += state.EditorCameraDraw;
            }
#else
            Draw(session, session.BattleCamera);
#endif
        }

        private static void Draw(BrawlTrainingSession session, Camera camera)
        {
            if (session.Loading || session.Closing || session.CancelRequested || !session.TerrainRendering.Instancing ||
                camera == null || session.TerrainRoot == null || !session.TerrainRoot.activeInHierarchy) return;
            foreach (BrawlTerrainBatch batch in session.TerrainRendering.Batches.Values)
            {
                if (batch.Matrices.Count == 0) continue;
                Graphics.DrawMeshInstanced(batch.Mesh, batch.SubMesh, batch.Material, batch.Matrices, null,
                    batch.Shadows, batch.ReceiveShadows, batch.Layer, camera, batch.Probes);
            }
        }

        /// <summary>仅切换表现，供编辑器同房间 A/B 验证；不重建材质或改变画质。</summary>
        public static void SetInstancing(BrawlTrainingSession session, bool enabled)
        {
            session.TerrainRendering.Instancing = enabled && SystemInfo.supportsInstancing;
            foreach (BrawlStaticRenderObject entry in session.TerrainRendering.Objects.Values)
                foreach (MeshRenderer renderer in entry.Renderers)
                    if (renderer != null) renderer.enabled = !session.TerrainRendering.Instancing;
        }

        public static void ClearGrass(BrawlTrainingSession session, int cell)
        {
            BrawlTerrainRenderState state = session.TerrainRendering;
            if (!state.Tiles.TryGetValue(cell, out GameObject tile) || tile == null) return;
            tile.SetActive(false);
            RemoveObject(state, tile.GetInstanceID());
        }

        public static void RefreshNPC(BrawlTrainingSession session, BrawlPracticeUnitView view)
        {
            BrawlTerrainRenderState state = session.TerrainRendering;
            if (!state.UnitOwners.TryGetValue(view.UnitId, out int owner)) return;
            BrawlStaticRenderObject entry = state.Objects[owner];
            // NPC 当前静止；将来出现移动/隐藏时安全回退，不能留下旧位置的幽灵实例。
            if (entry.Root == null || !entry.Root.activeInHierarchy || entry.Root.transform.localToWorldMatrix != entry.Matrix)
                RemoveNPC(session, view.UnitId);
        }

        public static void RemoveNPC(BrawlTrainingSession session, long unitId)
        {
            if (!session.TerrainRendering.UnitOwners.TryGetValue(unitId, out int owner)) return;
            RemoveObject(session.TerrainRendering, owner);
            session.TerrainRendering.UnitOwners.Remove(unitId);
        }

        private static void RemoveObject(BrawlTerrainRenderState state, int owner)
        {
            if (!state.Objects.TryGetValue(owner, out BrawlStaticRenderObject entry)) return;
            foreach (MeshRenderer renderer in entry.Renderers) if (renderer != null) renderer.enabled = true;
            state.Objects.Remove(owner);
            // 仅生命周期事件修改矩阵；稳定帧不重新分组、不复制材质。
            foreach (BrawlTerrainBatch batch in state.Batches.Values)
                for (int i = batch.Owners.Count - 1; i >= 0; --i)
                    if (batch.Owners[i] == owner) { batch.Owners.RemoveAt(i); batch.Matrices.RemoveAt(i); }
        }

        public static void Clear(BrawlTrainingSession session)
        {
            BrawlTerrainRenderState state = session.TerrainRendering;
#if UNITY_EDITOR
            if (state.EditorCameraDraw != null)
            {
                RenderPipelineManager.beginCameraRendering -= state.EditorCameraDraw;
                state.EditorCameraDraw = null;
            }
#endif
            foreach (Material material in state.Materials.Values) if (material != null) UnityEngine.Object.Destroy(material);
            state.Materials.Clear(); state.Batches.Clear(); state.Objects.Clear(); state.UnitOwners.Clear(); state.Tiles.Clear();
        }
    }
}
