using System;
using UnityEngine;
using YIUIFramework;
using YooAsset;

namespace ET.Client
{
    /// <summary>复用 YIUI 3DDisplay 的实时 RT 与拖拽，业务侧只负责英雄加载、Idle 和构图。</summary>
    public static class BrawlHeroPreviewHelper
    {
        private const int PreviewWidth = 600;
        private const int PreviewHeight = 800;
        private const float StageHeight = 10000;
        private const float FramingPadding = 1.12f;
        public const float AspectRatio = (float)PreviewWidth / PreviewHeight;

        public static async ETTask Show(LobbyPanelComponent self, string modelPath)
        {
            BrawlHeroDetailView view = self.HeroDetails;
            Release(view);
            if (string.IsNullOrWhiteSpace(modelPath))
            {
                view.PreviewStatus.text = "暂无英雄模型";
                return;
            }
            int version = view.PreviewVersion;
            view.PreviewStatus.text = "模型加载中…";
            EntityRef<LobbyPanelComponent> owner = self;
            AssetHandle handle = null;
            try
            {
                handle = YooAssets.GetPackage("DefaultPackage").LoadAssetAsync<GameObject>(modelPath);
                await handle.Task;
                self = owner;
                if (self == null || self.HeroDetails != view || view.PreviewVersion != version || view.PreviewImage == null) return;
                if (handle.Status != EOperationStatus.Succeed || handle.AssetObject is not GameObject prefab)
                    throw new InvalidOperationException($"英雄预览模型 {modelPath} 加载失败：{handle.LastError}");
                CreateDisplay(self, view, prefab);
                // 实时展示期间必须保留句柄，避免模型正在使用的网格／材质／动画被卸载。
                view.PreviewHandle = handle;
                handle = null;
                view.PreviewStatus.text = "";
            }
            catch (Exception exception)
            {
                self = owner;
                if (self == null || self.HeroDetails != view || view.PreviewVersion != version || view.PreviewImage == null) return;
                Release(view);
                view.PreviewStatus.text = "模型暂时无法显示";
                Log.Warning($"Brawl 英雄预览失败：{modelPath}\n{exception}");
            }
            finally
            {
                // 迟到或失败的加载只释放自己的句柄，不能触碰新英雄的展示。
                handle?.Release();
            }
        }

        public static void Resume(LobbyPanelComponent self)
        {
            if (self.HeroDetails == null || self.HeroDetailView == null || !self.HeroDetailView.activeInHierarchy) return;
            BrawlHeroConfig hero = self.Fiber().GetSingleton<BrawlHeroConfigCategory>().GetOrDefault(self.SelectedHeroId);
            if (hero != null) Show(self, hero.ModelPath).Coroutine();
        }

        public static void Release(BrawlHeroDetailView view)
        {
            ++view.PreviewVersion;
            // 先由 YIUI 归还 RT，再销毁舞台，避免正在渲染的相机继续引用已释放纹理。
            YIUI3DDisplayChild child = view.PreviewDisplayChild;
            child?.Dispose();
            view.PreviewDisplayChild = default;
            if (view.PreviewDisplay != null)
            {
                view.PreviewDisplay.m_ShowObject = null;
                view.PreviewDisplay.m_LookCamera = null;
                view.PreviewDisplay.m_ShowCamera = null;
                view.PreviewDisplay.m_ShowCameraCtrl = null;
                view.PreviewDisplay.m_ShowLight = null;
                view.PreviewDisplay.m_FitScaleRoot = null;
            }
            if (view.PreviewImage != null)
            {
                view.PreviewImage.texture = null;
                view.PreviewImage.enabled = false;
            }
            if (view.PreviewStage != null)
            {
                view.PreviewStage.SetActive(false);
                UnityEngine.Object.Destroy(view.PreviewStage);
                view.PreviewStage = null;
            }
            view.PreviewHandle?.Release();
            view.PreviewHandle = null;
        }

        private static void CreateDisplay(LobbyPanelComponent owner, BrawlHeroDetailView view, GameObject prefab)
        {
            int layer = LayerMask.NameToLayer(YIUIConstHelper.Const.YIUI3DLayer);
            if (layer < 0) throw new InvalidOperationException($"缺少 YIUI 展示层：{YIUIConstHelper.Const.YIUI3DLayer}");

            // 舞台跟随预览控件启停／销毁，三维尺寸抵消 Canvas 缩放，位置远离游戏场景。
            GameObject stage = new("BrawlHeroPortraitStage");
            view.PreviewStage = stage;
            stage.hideFlags = HideFlags.HideAndDontSave;
            stage.SetActive(false);
            stage.transform.SetParent(view.PreviewImage.transform, false);
            Vector3 parentScale = view.PreviewImage.transform.lossyScale;
            stage.transform.localScale = new Vector3(1 / parentScale.x, 1 / parentScale.y, 1 / parentScale.z);
            stage.transform.SetPositionAndRotation(new Vector3(0, StageHeight, 0), Quaternion.identity);

            // 拖拽包装根，保留原 Prefab 的缩放、Animator、Override 和固定武器。
            GameObject pivot = new("HeroRotationPivot");
            pivot.transform.SetParent(stage.transform, false);
            GameObject model = UnityEngine.Object.Instantiate(prefab, pivot.transform, false);
            model.transform.localPosition = Vector3.zero;
            PrepareModel(model, layer);
            stage.SetActive(true);
            PlayIdle(model);
            Bounds bounds = RotationBounds(ModelBounds(model), pivot.transform.position);
            float radius = Mathf.Max(bounds.extents.magnitude, 0.1f);
            float distance = radius * 4;
            Vector3 direction = new Vector3(0.24f, 0.08f, 1).normalized;

            Camera camera = NewCamera("PortraitCamera", stage.transform);
            Camera lookCamera = NewCamera("PortraitFraming", pivot.transform);
            lookCamera.orthographic = true;
            lookCamera.aspect = AspectRatio;
            lookCamera.transform.SetPositionAndRotation(bounds.center + direction * distance,
                Quaternion.LookRotation(-direction, Vector3.up));
            lookCamera.nearClipPlane = Mathf.Max(radius * 0.01f, 0.001f);
            lookCamera.farClipPlane = distance + radius * 2;
            lookCamera.orthographicSize = FitSize(lookCamera, bounds);
            camera.aspect = AspectRatio;

            if (view.PreviewDisplay == null)
                view.PreviewDisplay = view.PreviewImage.gameObject.AddComponent<UI3DDisplay>();
            UI3DDisplay display = view.PreviewDisplay;
            display.m_AutoChangeSize = false;
            display.m_ShowImage = view.PreviewImage;
            display.m_ShowCamera = camera;
            display.m_ShowCameraCtrl = camera.gameObject.AddComponent<UI3DDisplayCamera>();
            display.m_FitScaleRoot = stage.transform;
            display.m_ShowObject = pivot;
            display.m_LookCamera = lookCamera;
            display.m_ResolutionX = PreviewWidth;
            display.m_ResolutionY = PreviewHeight;
            display.m_RenderTextureDepthBuffer = 24;
            display.m_CanDrag = true;
            display.m_DragDegreesPerWidth = 360;
            display.m_ShowRotation = Vector3.zero;
            display.m_ShowScale = Vector3.one;
            display.m_ShowOffset = Vector3.zero;
            display.m_AutoSyncLookCamera = false;
            display.m_AutoSetColliderLayer = false;
            display.m_MultipleTargetMode = false;
            display.m_UseLookCameraColor = false;
            display.OnClick = false;
            view.PreviewImage.raycastTarget = true;
            // 使用 3DDisplay 已有的“预设对象＋参考相机”初始化入口，不另建渲染／拖拽循环。
            view.PreviewDisplayChild = owner.AddChild<YIUI3DDisplayChild, UI3DDisplay>(display);

            // 3DDisplay 会为模型分配展示偏移，灯光在最终位置构图后设置，不跟随拖拽旋转。
            bounds = RotationBounds(ModelBounds(model), pivot.transform.position);
            GameObject lightObject = new("PortraitLight", typeof(Light));
            lightObject.transform.SetParent(stage.transform, false);
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Point;
            light.cullingMask = 1 << layer;
            light.shadows = LightShadows.None;
            light.intensity = 3;
            light.range = radius * 8;
            light.transform.position = bounds.center + new Vector3(-1, 1.5f, 2) * radius;
            display.m_ShowLight = light;
        }

        private static Camera NewCamera(string name, Transform parent)
        {
            GameObject cameraObject = new(name, typeof(Camera));
            cameraObject.transform.SetParent(parent, false);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = false;
            return camera;
        }

        private static void PrepareModel(GameObject model, int layer)
        {
            foreach (Transform child in model.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = layer;
            // 展示实例不参与物理、声音、购买包脚本或粒子播放。
            foreach (MonoBehaviour script in model.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
            foreach (Collider collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (Rigidbody body in model.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.detectCollisions = false;
            }
            foreach (AudioSource source in model.GetComponentsInChildren<AudioSource>(true)) source.enabled = false;
            foreach (Camera camera in model.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
            foreach (Light light in model.GetComponentsInChildren<Light>(true)) light.enabled = false;
            foreach (ParticleSystem particle in model.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = particle.main;
                main.playOnAwake = false;
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
                if (renderer is ParticleSystemRenderer || renderer is TrailRenderer) renderer.enabled = false;
            foreach (Animator animator in model.GetComponentsInChildren<Animator>(true))
            {
                animator.applyRootMotion = false;
                animator.fireEvents = false;
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
        }

        private static void PlayIdle(GameObject model)
        {
            foreach (Animator animator in model.GetComponentsInChildren<Animator>())
            {
                if (!animator.isActiveAndEnabled || animator.runtimeAnimatorController == null) continue;
                // Brawl 控制器进入零位移的 Idle；其它控制器直接运行原默认状态。
                if (animator.GetLayerIndex("Locomotion") == 0)
                {
                    animator.SetFloat("MoveX", 0);
                    animator.SetFloat("MoveZ", 0);
                    animator.SetFloat("LocomotionSpeed", 1);
                    animator.SetLayerWeight(0, 1);
                    for (int layer = 1; layer < animator.layerCount; ++layer) animator.SetLayerWeight(layer, 0);
                    animator.Play("Locomotion.Move", 0, 0);
                }
                animator.Update(0);
            }
        }

        private static Bounds ModelBounds(GameObject model)
        {
            Bounds bounds = default;
            bool found = false;
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || !(renderer is MeshRenderer || renderer is SkinnedMeshRenderer)) continue;
                if (!found) bounds = renderer.bounds;
                else bounds.Encapsulate(renderer.bounds);
                found = true;
            }
            if (!found) throw new InvalidOperationException("英雄模型没有可见网格");
            return bounds;
        }

        private static Bounds RotationBounds(Bounds bounds, Vector3 pivot)
        {
            // 按绕 Y 轴一周的包围范围构图，拖到背面／侧面时也为武器保留空间。
            float x = Mathf.Abs(bounds.center.x - pivot.x) + bounds.extents.x;
            float z = Mathf.Abs(bounds.center.z - pivot.z) + bounds.extents.z;
            float radius = Mathf.Sqrt(x * x + z * z);
            return new Bounds(new Vector3(pivot.x, bounds.center.y, pivot.z), new Vector3(radius * 2, bounds.size.y, radius * 2));
        }

        private static float FitSize(Camera camera, Bounds bounds)
        {
            float halfHeight = 0;
            for (int i = 0; i < 8; ++i)
            {
                Vector3 point = bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 local = camera.transform.InverseTransformPoint(point);
                halfHeight = Mathf.Max(halfHeight, Mathf.Abs(local.y), Mathf.Abs(local.x) / AspectRatio);
            }
            return Mathf.Max(halfHeight * FramingPadding, 0.1f);
        }
    }
}
