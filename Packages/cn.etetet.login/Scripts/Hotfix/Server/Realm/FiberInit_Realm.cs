using System;
using System.Collections.Generic;
using System.Net;

namespace ET.Server
{
    [Invoke(SceneType.Realm)]
    public class FiberInit_Realm: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ProcessInnerSender>();
            root.AddComponent<MessageSender>();
            root.AddComponent<DBManagerComponent>();
            EntityRef<Scene> rootRef = root;
            await LoginAccountHelper.InitializeAsync(root.GetComponent<DBManagerComponent>().GetZoneDB(root.Fiber().Zone).database);
            root = rootRef;

            AddressSingleton addressSingleton = root.GetSingleton<AddressSingleton>();
            int outerPort = AddressHelper.GetSceneOuterPort(root.Fiber(), root.Name.GetSceneConfigName());

            IPEndPoint innerIPOuterPort = new Address(addressSingleton.InnerIP, outerPort);
            
            NetComponent netComponent = root.AddComponent<NetComponent, IKcpTransport>(new UdpTransport(innerIPOuterPort));
            
            // 注册服务发现
            ServiceDiscoveryProxy serviceDiscoveryProxy = root.AddComponent<ServiceDiscoveryProxy>();
            EntityRef<ServiceDiscoveryProxy> serviceDiscoveryProxyComponentRef = serviceDiscoveryProxy;

            await serviceDiscoveryProxy.RegisterToServiceDiscovery
            (
                new StringKV()
                {
                    { ServiceMetaKey.InnerIPOuterPort, $"{netComponent.GetBindPoint()}" }
                }
            );
            
            // 订阅跟realm属于同一个zone的Gate
            serviceDiscoveryProxy = serviceDiscoveryProxyComponentRef;
            await serviceDiscoveryProxy.SubscribeServiceChange("Gate", 
                new StringKV()
                {
                    {ServiceMetaKey.SceneType, SceneTypeSingleton.Instance.GetSceneName(SceneType.Gate)},
                    { ServiceMetaKey.Zone, $"{fiberInit.Fiber.Zone}" }
                });
        }
    }
}
