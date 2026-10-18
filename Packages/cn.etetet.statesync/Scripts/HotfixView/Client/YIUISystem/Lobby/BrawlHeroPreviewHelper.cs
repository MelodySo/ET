using System;
using UnityEngine;
using UnityEngine.Rendering;
using YooAsset;

namespace ET.Client
{
    /// <summary>按需渲染英雄模型的待机定格；不保留每帧渲染的相机或战斗会话。</summary>
    public static class BrawlHeroPreviewHelper
    {
        private const int PreviewWidth = 600;
        private const int PreviewHeight = 800;
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
                view.PreviewTexture = Render(prefab);
                view.PreviewImage.texture = view.PreviewTexture;
                view.PreviewImage.enabled = true;
                view.PreviewStatus.text = "";
            }
            catch (Exception exception)
            {
                self = owner;
                if (self == null || self.HeroDetails != view || view.PreviewVersion != version || view.PreviewImage == null) return;
                view.PreviewStatus.text = "模型暂时无法显示";
                Log.Warning($"Brawl 英雄预览失败：{modelPath}\n{exception}");
            }
            finally
            {
                // 每次请求独占句柄；即使关闭页面，也等待本次加载结束后再释放。
                handle?.Release();
            }
        }

        public static void Release(BrawlHeroDetailView view)
        {
            ++view.PreviewVersion;
            // Unity UI 层级也可能先于大厅 Entity 销毁，纹理仍须独立释放。
            if (view.PreviewImage != null)
            {
                view.PreviewImage.texture = null;
                view.PreviewImage.enabled = false;
            }
            if (view.PreviewTexture != null)
            {
                view.PreviewTexture.Release();
                UnityEngine.Object.Destroy(view.PreviewTexture);
                view.PreviewTexture = null;
            }
        }

        private static RenderTexture Render(GameObject prefab)
        {
            // 临时舞台远离大厅；同步渲染结束立即停用，不参与下一帧的场景渲染与物理。
            GameObject stage = new("BrawlHeroPortraitStage");
            stage.hideFlags = HideFlags.HideAndDontSave;
            stage.SetActive(false);
            stage.transform.position = new Vector3(0, 10000, 0);
            RenderTexture texture = null;
            try
            {
                int layer = LayerMask.NameToLayer("UI");
                GameObject model = UnityEngine.Object.Instantiate(prefab, stage.transform, false);
                model.transform.localPosition = Vector3.zero;
                foreach (Transform child in model.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = layer;
                // 仅渲染模型。禁用展示包脚本、碰撞、声音、粒子与购买动画事件。
                foreach (MonoBehaviour script in model.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
                foreach (Collider collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                foreach (Rigidbody body in model.GetComponentsInChildren<Rigidbody>(true))
                {
                    body.isKinematic = true;
                    body.detectCollisions = false;
                }
                foreach (AudioSource source in model.GetComponentsInChildren<AudioSource>(true)) source.enabled = false;
                foreach (Camera childCamera in model.GetComponentsInChildren<Camera>(true)) childCamera.enabled = false;
                foreach (Light childLight in model.GetComponentsInChildren<Light>(true)) childLight.enabled = false;
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
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }
                stage.SetActive(true);
                SampleIdle(model);
                Bounds bounds = ModelBounds(model);

                GameObject cameraObject = new("PortraitCamera", typeof(Camera));
                cameraObject.transform.SetParent(stage.transform, false);
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.enabled = false;
                camera.cameraType = CameraType.Preview;
                camera.cullingMask = 1 << layer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.orthographic = true;
                camera.aspect = AspectRatio;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.useOcclusionCulling = false;
                float radius = Mathf.Max(bounds.extents.magnitude, 0.1f);
                float distance = radius * 4;
                Vector3 direction = new Vector3(0.24f, 0.08f, 1).normalized;
                camera.transform.position = bounds.center + direction * distance;
                camera.transform.rotation = Quaternion.LookRotation(-direction, Vector3.up);
                camera.nearClipPlane = Mathf.Max(radius * 0.01f, 0.001f);
                camera.farClipPlane = distance + radius * 2;
                camera.orthographicSize = FitSize(camera, bounds);

                GameObject lightObject = new("PortraitLight", typeof(Light));
                lightObject.transform.SetParent(stage.transform, false);
                Light light = lightObject.GetComponent<Light>();
                light.type = LightType.Point;
                light.cullingMask = camera.cullingMask;
                light.shadows = LightShadows.None;
                light.intensity = 3;
                light.range = radius * 8;
                light.transform.position = bounds.center + new Vector3(-1, 1.5f, 2) * radius;

                texture = new RenderTexture(PreviewWidth, PreviewHeight, 24, RenderTextureFormat.ARGB32)
                {
                    name = "BrawlHeroPortrait",
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };
                if (!texture.Create()) throw new InvalidOperationException("无法创建英雄预览纹理");
                if (GraphicsSettings.currentRenderPipeline == null)
                {
                    camera.targetTexture = texture;
                    camera.Render();
                    camera.targetTexture = null;
                }
                else
                {
                    // Unity 2022 URP 支持 StandardRequest，无须依赖 URP 的专有组件。
                    RenderPipeline.StandardRequest request = new() { destination = texture };
                    if (!RenderPipeline.SupportsRenderRequest(camera, request))
                        throw new InvalidOperationException("当前渲染管线不支持英雄预览请求");
                    RenderPipeline.SubmitRenderRequest(camera, request);
                }
                return texture;
            }
            catch
            {
                if (texture != null)
                {
                    texture.Release();
                    UnityEngine.Object.Destroy(texture);
                }
                throw;
            }
            finally
            {
                stage.SetActive(false);
                UnityEngine.Object.Destroy(stage);
            }
        }

        private static void SampleIdle(GameObject model)
        {
            foreach (Animator animator in model.GetComponentsInChildren<Animator>())
            {
                if (!animator.isActiveAndEnabled || animator.runtimeAnimatorController == null) continue;
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

        private static float FitSize(Camera camera, Bounds bounds)
        {
            float halfHeight = 0;
            // 将包围盒八角投到相机空间，避免武器、头顶或脚部超出预览。
            for (int i = 0; i < 8; ++i)
            {
                Vector3 point = bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 local = camera.transform.InverseTransformPoint(point);
                halfHeight = Mathf.Max(halfHeight, Mathf.Abs(local.y), Mathf.Abs(local.x) / AspectRatio);
            }
            return Mathf.Max(halfHeight * 1.08f, 0.1f);
        }
    }
}
