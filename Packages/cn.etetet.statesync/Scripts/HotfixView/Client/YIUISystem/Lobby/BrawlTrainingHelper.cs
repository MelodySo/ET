using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YooAsset;

namespace ET.Client
{
    /// <summary>第一版仅走本地场景展示，不调用原 MMO 的 EnterMap，不创建联网战斗单位。</summary>
    public static class BrawlTrainingHelper
    {
        // 当前入口按约定固定选用训练关卡，场景名称仍由配置决定。
        private const int TrainingLevelId = 10101;

        public static async ETTask Enter(LobbyPanelComponent self)
        {
            if (self.TrainingSession != null) return;
            EntityRef<LobbyPanelComponent> owner = self;
            BrawlTrainingSession session = new() { Loading = true, PreviousScene = SceneManager.GetActiveScene() };
            self.TrainingSession = session;
            try
            {
                BrawlHeroConfig hero = self.Fiber().GetSingleton<BrawlHeroConfigCategory>().GetOrDefault(self.SelectedHeroId);
                BrawlLevelConfig level = self.Fiber().GetSingleton<BrawlLevelConfigCategory>().GetOrDefault(TrainingLevelId);
                if (hero == null) throw new InvalidOperationException("请先选择英雄。");
                if (level == null || string.IsNullOrWhiteSpace(level.SceneAsset))
                    throw new InvalidOperationException($"缺少关卡 {TrainingLevelId} 或 SceneAsset，请先导出配置。");

                // 资源收集使用 AddressByFileName，同时兼容表内的项目相对路径和短名称。
                string address = Path.GetFileNameWithoutExtension(level.SceneAsset.Replace('\\', '/'));
                CreateOverlay(self, session);
                HideLobby(self, session);
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

                CreatePreview(session, hero.Id);
                session.StatusText.text = $"训练场 · {hero.Name}\n本地试用 / 暂无移动与战斗";
                session.LoadingBackground.enabled = false;
                session.ExitButton.interactable = true;
                session.Loading = false;
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
                    self.HeroDetailText.text += $"\n\n进入训练场失败：{exception.Message}";
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

        private static void CreatePreview(BrawlTrainingSession session, int heroId)
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
            GameObject hero = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            hero.name = $"TrainingHero_{heroId}";
            hero.transform.SetParent(session.RuntimeRoot.transform, false);
            hero.transform.position = new Vector3(0, 1, 0);
            // 只有可视占位，不参与物理、寻路、Grid 或服务端 Unit 生命周期。
            UnityEngine.Object.Destroy(hero.GetComponent<Collider>());

            GameObject cameraObject = new("TrainingCamera", typeof(Camera));
            cameraObject.transform.SetParent(session.RuntimeRoot.transform, false);
            cameraObject.transform.position = new Vector3(0, 5, -8);
            cameraObject.transform.LookAt(hero.transform.position);
            Camera preview = cameraObject.GetComponent<Camera>();
            preview.clearFlags = CameraClearFlags.SolidColor;
            preview.backgroundColor = new Color(0.12f, 0.17f, 0.23f);
            preview.nearClipPlane = 0.1f;
            preview.farClipPlane = 100;
            preview.orthographic = true;
            preview.orthographicSize = 4;

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
            session.StatusText.text = "正在进入训练场……";

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
            label.text = "退出训练场";
        }

        private static async ETTask Cleanup(BrawlTrainingSession session)
        {
            if (session.Closing) return;
            session.Closing = true;
            try
            {
                if (session.ExitButton != null) session.ExitButton.interactable = false;
                if (session.StatusText != null) session.StatusText.text = "正在返回大厅……";
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
                foreach (Camera camera in session.DisabledCameras) if (camera != null) camera.enabled = true;
                foreach (AudioListener listener in session.DisabledListeners) if (listener != null) listener.enabled = true;
                session.DisabledCameras.Clear();
                session.DisabledListeners.Clear();
                if (session.LobbyGroup != null)
                {
                    session.LobbyGroup.alpha = session.PreviousAlpha;
                    session.LobbyGroup.interactable = session.PreviousInteractable;
                    session.LobbyGroup.blocksRaycasts = session.PreviousBlocksRaycasts;
                    if (session.CreatedLobbyGroup) UnityEngine.Object.Destroy(session.LobbyGroup);
                }
                if (session.ExitButton != null) session.ExitButton.onClick.RemoveAllListeners();
                if (session.Overlay != null) UnityEngine.Object.Destroy(session.Overlay);
                session.SceneHandle = null;
            }
        }
    }
}
