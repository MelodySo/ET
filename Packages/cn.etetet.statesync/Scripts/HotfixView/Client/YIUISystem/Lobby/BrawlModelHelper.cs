using System;
using UnityEngine;
using YooAsset;
namespace ET.Client
{
    public static class BrawlModelHelper
    {
        public static async ETTask<GameObject> InstantiateModel(BrawlTrainingSession session, string address, Transform parent,
            string objectName, Vector3 position)
        {
            if (session.CancelRequested || session.Closing) return null;
            if (!session.ModelHandles.TryGetValue(address, out AssetHandle handle))
            {
                handle = YooAssets.GetPackage("DefaultPackage").LoadAssetAsync<GameObject>(address);
                // 先登记句柄，取消或加载失败时也由会话统一释放。
                session.ModelHandles.Add(address, handle);
            }
            // 多个新单位同时请求相同资源时，也必须等待已有句柄完成。
            await handle.Task;
            if (session.CancelRequested || session.Closing) return null;
            if (handle.Status != EOperationStatus.Succeed || handle.AssetObject is not GameObject prefab)
                throw new InvalidOperationException($"模型 {address} 加载失败：{handle.LastError}");
            GameObject instance = UnityEngine.Object.Instantiate(prefab, parent, false);
            instance.name = objectName;
            instance.transform.localPosition = position;
            try { BrawlCharacterAnimationHelper.Bind(session, instance); }
            catch
            {
                BrawlCharacterAnimationHelper.Release(session, instance);
                UnityEngine.Object.Destroy(instance);
                throw;
            }
            return instance;
        }

    }
}
