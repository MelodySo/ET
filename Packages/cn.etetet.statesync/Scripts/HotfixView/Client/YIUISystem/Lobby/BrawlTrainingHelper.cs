using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YooAsset;

namespace ET.Client
{
    /// <summary>本地训练场与服务器练习赛共用的显示流程，不调用原 MMO EnterMap。</summary>
    public static class BrawlTrainingHelper
    {
        // 当前入口按约定固定选用训练关卡，场景名称仍由配置决定。
        private const int TrainingLevelId = 10101;

        public static async ETTask Enter(LobbyPanelComponent self, int levelId = TrainingLevelId, bool practice = false)
        {
            if (self.TrainingSession != null) return;
            EntityRef<LobbyPanelComponent> owner = self;
            BrawlTrainingSession session = new() { Loading = true, IsPractice = practice, PreviousScene = SceneManager.GetActiveScene() };
            self.TrainingSession = session;
            try
            {
                int heroId = self.SelectedHeroId;
                CreateOverlay(self, session);
                HideLobby(self, session);
                if (practice)
                {
                    levelId = await BrawlPracticeClientHelper.Enter(self, session, heroId);
                    self = owner;
                    if (self == null || session.CancelRequested)
                    {
                        session.Loading = false;
                        await Cleanup(session);
                        return;
                    }
                }
                else session.CellSize = self.Fiber().GetSingleton<BrawlGlobalConfigCategory>().CellSize;
                // 分支中的 RPC / 取消清理均可能 await，公共路径重新取得存活大厅实体。
                self = owner;
                if (session.CellSize <= 0) throw new InvalidOperationException("BrawlGlobal.CellSize 必须大于 0，请修改并导出配置。");
                BrawlHeroConfig hero = self.Fiber().GetSingleton<BrawlHeroConfigCategory>().GetOrDefault(heroId);
                BrawlLevelConfig level = self.Fiber().GetSingleton<BrawlLevelConfigCategory>().GetOrDefault(levelId);
                if (hero == null) throw new InvalidOperationException("请先选择英雄。");
                if (level == null || string.IsNullOrWhiteSpace(level.SceneAsset))
                    throw new InvalidOperationException($"缺少关卡 {levelId} 或 SceneAsset，请先导出配置。");
                BrawlLevelStyleConfig style = self.Fiber().GetSingleton<BrawlLevelStyleConfigCategory>().GetOrDefault(level.StyleId);
                ValidateLevel(level, style);
                if (string.IsNullOrWhiteSpace(hero.ModelPath))
                    throw new InvalidOperationException($"英雄 {hero.Id} 缺少 ModelPath，请检查英雄表并导出。");
                BrawlNPCConfig[] npcs = practice ? null : ResolveNPCs(level, self.Fiber().GetSingleton<BrawlNPCConfigCategory>());

                BrawlProjectileConfigCategory projectileConfigs = practice ? self.Fiber().GetSingleton<BrawlProjectileConfigCategory>() : null;
                // 资源收集使用 AddressByFileName，同时兼容表内的项目相对路径和短名称。
                string address = Path.GetFileNameWithoutExtension(level.SceneAsset.Replace('\\', '/'));
                session.SceneHandle = YooAssets.GetPackage("DefaultPackage").LoadSceneAsync(address, LoadSceneMode.Additive);
                await session.SceneHandle.Task;

                self = owner;
                if (self == null || session.CancelRequested)
                {
                    session.Loading = false;
                    await Cleanup(session);
                    return;
                }
                if (session.SceneHandle.Status != EOperationStatus.Succeed)
                    throw new InvalidOperationException($"训练场加载失败：{address}\n{session.SceneHandle.LastError}");
                if (!session.SceneHandle.ActivateScene()) throw new InvalidOperationException("无法激活训练场场景。");

                CreatePreview(session, level);
                session.CameraController = self.Root().AddComponent<BrawlCameraComponent, Camera, BrawlGlobalConfigCategory>(
                    session.BattleCamera, self.Fiber().GetSingleton<BrawlGlobalConfigCategory>());
                await LoadTerrain(session, level, style);
                if (practice && !session.CancelRequested)
                {
                    await BrawlProjectileVisualHelper.Preload(session, projectileConfigs);
                    if (!session.CancelRequested && !session.Closing) session.ProjectilePresentation = new BrawlProjectilePresentation();
                }
                if (!session.CancelRequested)
                {
                    if (practice) await LoadPracticeUnits(session);
                    else await LoadCharacters(session, hero, level, npcs);
                }
                self = owner;
                if (self == null || session.CancelRequested)
                {
                    session.Loading = false;
                    await Cleanup(session);
                    return;
                }
                BrawlCameraComponent cameraController = session.CameraController;
                cameraController.SetTarget(session.Hero != null ? session.Hero.transform : null);
                cameraController.Follow();
                session.StatusText.text = practice
                    ? $"练习赛 · {hero.Name}\n点按普攻随移动方向 · 拖动瞄准 · 拖回取消"
                    : $"训练场 · {hero.Name}\n本地试用 / 暂无移动与战斗";
                session.LoadingBackground.enabled = false;
                session.ExitButton.interactable = true;
                session.Loading = false;
                if (practice) BrawlSkillViewHelper.Create(session, self.HeroMenuFont, hero, self.Fiber().GetSingleton<BrawlSkillConfigCategory>());
            }
            catch (Exception exception)
            {
                session.Loading = false;
                Log.Error(exception);
                await Cleanup(session);
                self = owner;
                if (self != null)
                {
                    self.TrainingSession = null;
                    self.HeroTrialView.SetActive(false);
                    self.HeroDetailText.text += $"\n\n进入{(practice ? "练习赛" : "训练场")}失败：{exception.Message}";
                }
            }
        }

        public static async ETTask Exit(LobbyPanelComponent self)
        {
            BrawlTrainingSession session = self.TrainingSession;
            if (session == null || session.Loading || session.Closing) return;
            EntityRef<LobbyPanelComponent> owner = self;
            await Cleanup(session);
            self = owner;
            if (self == null) return;
            self.TrainingSession = null;
            self.HeroTrialView.SetActive(false);
        }

        public static void Cancel(LobbyPanelComponent self)
        {
            BrawlTrainingSession session = self.TrainingSession;
            if (session == null) return;
            session.CancelRequested = true;
            // 加载中的场景必须等到句柄完成后才能清理；Enter 会负责这条路径。
            if (!session.Loading && !session.Closing) Cleanup(session).Coroutine();
            self.TrainingSession = null;
        }

        private static void HideLobby(LobbyPanelComponent self, BrawlTrainingSession session)
        {
            GameObject lobby = self.UIBase.OwnerRectTransform.gameObject;
            session.LobbyGroup = lobby.GetComponent<CanvasGroup>();
            session.CreatedLobbyGroup = session.LobbyGroup == null;
            if (session.CreatedLobbyGroup) session.LobbyGroup = lobby.AddComponent<CanvasGroup>();
            session.PreviousAlpha = session.LobbyGroup.alpha;
            session.PreviousInteractable = session.LobbyGroup.interactable;
            session.PreviousBlocksRaycasts = session.LobbyGroup.blocksRaycasts;
            session.LobbyGroup.alpha = 0;
            session.LobbyGroup.interactable = false;
            session.LobbyGroup.blocksRaycasts = false;
        }

        public static void ValidateLevel(BrawlLevelConfig level, BrawlLevelStyleConfig style)
        {
            if (style == null) throw new InvalidOperationException($"关卡 {level.Id} 的 StyleId={level.StyleId} 不存在，请检查风格表并导出。");
            if (level.RowCount < 1 || level.RowCount > 100 || level.ColumnCount < 1 || level.ColumnCount > 100 ||
                level.MapGrid == null || level.MapGrid.Length != level.RowCount * level.ColumnCount)
                throw new InvalidOperationException($"关卡 {level.Id} 的行列数或 MapGrid 长度非法。");
            foreach (int tile in level.MapGrid)
                if (!Enum.IsDefined(typeof(BrawlTileType), tile)) throw new InvalidOperationException($"未知地形编号：{tile}");
            if (level.SpawnPoints == null || level.SpawnPoints.Length == 0 || level.SpawnPoints[0] < 0 || level.SpawnPoints[0] >= level.MapGrid.Length)
                throw new InvalidOperationException($"关卡 {level.Id} 缺少有效的玩家出生点。");
            int spawnTile = level.MapGrid[level.SpawnPoints[0]];
            if (spawnTile != (int)BrawlTileType.Ground && spawnTile != (int)BrawlTileType.Grass)
                throw new InvalidOperationException("玩家出生点必须在平地或草丛上。");
        }

        public static Vector3 CellPosition(int index, BrawlLevelConfig level, int cellSize)
        {
            Unity.Mathematics.float2 position = BrawlTerrainMovementHelper.CellPosition(level, index, cellSize);
            return new Vector3(position.x, 0, position.y);
        }

        private static BrawlNPCConfig[] ResolveNPCs(BrawlLevelConfig level, BrawlNPCConfigCategory category)
        {
            int count = level.NPCIds?.Length ?? 0;
            if (count != (level.NPCPoints?.Length ?? 0))
                throw new InvalidOperationException($"关卡 {level.Id} 的 NPCIds 与 NPCPoints 必须等长。");
            BrawlNPCConfig[] configs = new BrawlNPCConfig[count];
            HashSet<int> occupied = new();
            for (int i = 0; i < count; ++i)
            {
                int point = level.NPCPoints[i];
                if (point < 0 || point >= level.MapGrid.Length || !occupied.Add(point) ||
                    Array.IndexOf(level.SpawnPoints, point) >= 0)
                    throw new InvalidOperationException($"关卡 {level.Id} 的 NPC 出生格 {point} 越界、重复或与玩家重叠。");
                int tile = level.MapGrid[point];
                if (tile != (int)BrawlTileType.Ground && tile != (int)BrawlTileType.Grass)
                    throw new InvalidOperationException($"关卡 {level.Id} 的 NPC 出生格 {point} 必须在平地或草丛上。");
                BrawlNPCConfig config = category.GetOrDefault(level.NPCIds[i]);
                if (config == null || string.IsNullOrWhiteSpace(config.ModelPath))
                    throw new InvalidOperationException($"NPC {level.NPCIds[i]} 不存在或缺少 ModelPath，请检查 NPC 表并导出。");
                configs[i] = config;
            }
            return configs;
        }

        private static async ETTask LoadCharacters(BrawlTrainingSession session, BrawlHeroConfig hero, BrawlLevelConfig level,
            BrawlNPCConfig[] npcs)
        {
            session.Hero = await BrawlModelHelper.InstantiateModel(session, hero.ModelPath, session.RuntimeRoot.transform,
                $"TrainingHero_{hero.Id}", CellPosition(level.SpawnPoints[0], level, session.CellSize));
            if (session.CancelRequested) return;
            // 保留 Prefab 的专属武器和控制器，不驱动动画参数；禁止根运动改变出生位置。
            foreach (Animator animator in session.Hero.GetComponentsInChildren<Animator>(true)) animator.applyRootMotion = false;
            session.NPCRoot = new GameObject("NPCs");
            session.NPCRoot.transform.SetParent(session.RuntimeRoot.transform, false);
            for (int i = 0; i < npcs.Length; ++i)
            {
                if (session.CancelRequested) return;
                GameObject npc = await BrawlModelHelper.InstantiateModel(session, npcs[i].ModelPath, session.NPCRoot.transform,
                    $"NPC_{i}_{npcs[i].Id}", CellPosition(level.NPCPoints[i], level, session.CellSize));
                if (session.CancelRequested) return;
                foreach (Animator animator in npc.GetComponentsInChildren<Animator>(true)) animator.applyRootMotion = false;
            }
        }

        private static async ETTask LoadPracticeUnits(BrawlTrainingSession session)
        {
            session.NPCRoot = new GameObject("ServerUnits");
            session.NPCRoot.transform.SetParent(session.RuntimeRoot.transform, false);
            foreach (BrawlPracticeUnitView view in session.PracticeUnits.ToArray())
            {
                if (session.CancelRequested) return;
                await BrawlUnitRosterViewHelper.LoadModel(session, view);
            }
        }
        public static string TerrainAddress(BrawlLevelStyleConfig style, int tile) => tile switch
        {
            (int)BrawlTileType.Ground => "",
            (int)BrawlTileType.Wall => style.ModelPath1,
            (int)BrawlTileType.Grass => style.ModelPath2,
            (int)BrawlTileType.BulletPassableObstacle => style.ModelPath3,
            (int)BrawlTileType.Water => style.ModelPath4,
            _ => throw new InvalidOperationException($"未知地形编号：{tile}"),
        };

        public static async ETTask LoadTerrain(BrawlTrainingSession session, BrawlLevelConfig level, BrawlLevelStyleConfig style)
        {
            ValidateLevel(level, style);
            session.TerrainRoot = new GameObject("Terrain");
            session.TerrainRoot.transform.SetParent(session.RuntimeRoot.transform, false);
            for (int index = 0; index < level.MapGrid.Length; ++index)
            {
                if (session.CancelRequested) return;
                string address = TerrainAddress(style, level.MapGrid[index]);
                if (string.IsNullOrWhiteSpace(address)) continue;
                await BrawlModelHelper.InstantiateModel(session, address, session.TerrainRoot.transform, $"Tile_{index}_{address}",
                    CellPosition(index, level, session.CellSize));
                // 保留 Prefab 原有旋转和缩放。
            }
        }

        public static void CreatePreview(BrawlTrainingSession session, BrawlLevelConfig level)
        {
            // 暂时接管画面，退出时恢复；不移动或修改原相机的 Transform。
            foreach (Camera camera in Camera.allCameras)
            {
                session.DisabledCameras.Add(camera);
                camera.enabled = false;
            }
            foreach (AudioListener listener in UnityEngine.Object.FindObjectsOfType<AudioListener>())
            {
                if (!listener.enabled) continue;
                session.DisabledListeners.Add(listener);
                listener.enabled = false;
            }
            session.RuntimeRoot = new GameObject("BrawlTrainingRuntime");
            SceneManager.MoveGameObjectToScene(session.RuntimeRoot, session.SceneHandle.SceneObject);

            GameObject cameraObject = new("BrawlCamera", typeof(Camera));
            cameraObject.transform.SetParent(session.RuntimeRoot.transform, false);
            Camera preview = cameraObject.GetComponent<Camera>();
            preview.clearFlags = CameraClearFlags.SolidColor;
            preview.backgroundColor = new Color(0.12f, 0.17f, 0.23f);
            session.BattleCamera = preview;

            GameObject lightObject = new("TrainingLight", typeof(Light));
            lightObject.transform.SetParent(session.RuntimeRoot.transform, false);
            lightObject.transform.rotation = Quaternion.Euler(50, -30, 0);
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1;
        }

        private static void CreateOverlay(LobbyPanelComponent self, BrawlTrainingSession session)
        {
            session.Overlay = new GameObject("BrawlTrainingUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            // 独立 Overlay 不依赖被隐藏的大厅，也不依赖训练场里的相机。
            Canvas canvas = session.Overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            CanvasScaler scaler = session.Overlay.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            session.LoadingBackground = session.Overlay.AddComponent<Image>();
            session.LoadingBackground.color = new Color(0.04f, 0.06f, 0.10f);

            GameObject statusObject = new("Status", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform statusRect = (RectTransform)statusObject.transform;
            statusRect.SetParent(session.Overlay.transform, false);
            statusRect.anchorMin = statusRect.anchorMax = new Vector2(0, 1);
            statusRect.pivot = new Vector2(0, 1);
            statusRect.anchoredPosition = new Vector2(24, -20);
            statusRect.sizeDelta = new Vector2(750, 100);
            session.StatusText = statusObject.GetComponent<TextMeshProUGUI>();
            session.StatusText.font = self.HeroMenuFont;
            session.StatusText.fontSize = 26;
            session.StatusText.color = Color.white;
            session.StatusText.raycastTarget = false;
            session.StatusText.richText = false;
            session.StatusText.text = session.IsPractice ? "正在请求练习赛……" : "正在进入训练场……";

            GameObject exitObject = new("ExitTraining", typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform exitRect = (RectTransform)exitObject.transform;
            exitRect.SetParent(session.Overlay.transform, false);
            exitRect.anchorMin = exitRect.anchorMax = Vector2.one;
            exitRect.pivot = Vector2.one;
            exitRect.anchoredPosition = new Vector2(-24, -24);
            exitRect.sizeDelta = new Vector2(180, 60);
            Image image = exitObject.GetComponent<Image>();
            image.color = new Color(0.18f, 0.28f, 0.43f);
            session.ExitButton = exitObject.GetComponent<Button>();
            session.ExitButton.targetGraphic = image;
            session.ExitButton.interactable = false;
            EntityRef<LobbyPanelComponent> owner = self;
            session.ExitButton.onClick.AddListener(() =>
            {
                LobbyPanelComponent current = owner;
                if (current != null) Exit(current).Coroutine();
            });
            GameObject labelObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform labelRect = (RectTransform)labelObject.transform;
            labelRect.SetParent(exitRect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.font = self.HeroMenuFont;
            label.fontSize = 26;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.text = session.IsPractice ? "退出练习赛" : "退出训练场";
        }

        public static async ETTask Cleanup(BrawlTrainingSession session)
        {
            if (session.Closing) return;
            session.Closing = true;
            try
            {
                if (session.ExitButton != null) session.ExitButton.interactable = false;
                if (session.NormalAttackButton != null) session.NormalAttackButton.interactable = false;
                if (session.SuperSkillButton != null) session.SuperSkillButton.interactable = false;
                BrawlSkillJoystickHelper.CancelAll(session);
                if (session.StatusText != null) session.StatusText.text = "正在返回大厅……";
                BrawlCameraComponent cameraController = session.CameraController;
                cameraController?.Dispose();
                session.CameraController = default;
                session.BattleCamera = null;
                BrawlPracticeClientComponent movement = session.MovementClient;
                movement?.Dispose();
                session.MovementClient = default;
                if (session.IsPractice) await BrawlPracticeClientHelper.Exit(session);
                if (session.PreviousScene.IsValid() && session.PreviousScene.isLoaded) SceneManager.SetActiveScene(session.PreviousScene);
                if (session.RuntimeRoot != null)
                {
                    session.RuntimeRoot.SetActive(false);
                    UnityEngine.Object.Destroy(session.RuntimeRoot);
                }
                SceneHandle handle = session.SceneHandle;
                if (handle != null && handle.IsValid)
                {
                    if (handle.SceneObject.IsValid() && handle.SceneObject.isLoaded)
                    {
                        UnloadSceneOperation operation = handle.UnloadAsync();
                        await operation.Task;
                        if (operation.Status != EOperationStatus.Succeed) Log.Error($"训练场卸载失败：{operation.Error}");
                    }
                    if (handle.IsValid) handle.Release();
                }
            }
            catch (Exception exception) { Log.Error(exception); }
            finally
            {
                foreach (AssetHandle modelHandle in session.ModelHandles.Values)
                    if (modelHandle.IsValid) modelHandle.Release();
                session.ModelHandles.Clear();
                session.TerrainRoot = null;
                session.Hero = null;
                session.NPCRoot = null;
                session.PracticeUnits.Clear();
                session.PracticeRoomId = 0;
                foreach (Camera camera in session.DisabledCameras) if (camera != null) camera.enabled = true;
                foreach (AudioListener listener in session.DisabledListeners) if (listener != null) listener.enabled = true;
                session.DisabledCameras.Clear();
                session.DisabledListeners.Clear();
                if (session.LobbyGroup != null)
                {
                    session.LobbyGroup.alpha = session.PreviousAlpha;
                    session.LobbyGroup.interactable = session.PreviousInteractable;
                    session.LobbyGroup.blocksRaycasts = session.PreviousBlocksRaycasts;
                    // 留给大厅统一销毁，避免同帧再次进入时复用到等待 Destroy 的组件。
                }
                if (session.ExitButton != null) session.ExitButton.onClick.RemoveAllListeners();
                if (session.NormalAttackButton != null) session.NormalAttackButton.onClick.RemoveAllListeners();
                session.SkillAimPreview = null;
                BrawlProjectileViewHelper.Clear(session);
                if (session.Overlay != null) UnityEngine.Object.Destroy(session.Overlay);
                session.SceneHandle = null;
            }
        }
    }
}
