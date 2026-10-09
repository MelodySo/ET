using System;
using System.Net;
using System.Net.Sockets;

namespace ET.Client
{
    [MessageHandler(SceneType.NetClient)]
    public class Main2NetClient_LoginHandler: MessageHandler<Scene, Main2NetClient_Login, NetClient2Main_Login>
    {
        protected override async ETTask Run(Scene root, Main2NetClient_Login request, NetClient2Main_Login response)
        {
            string account = request.Account;
            string password = request.Password;
            // 创建一个ETModel层的Session
            root.RemoveComponent<RouterAddressComponent>();
            // 获取路由跟realmDispatcher地址
            RouterAddressComponent routerAddressComponent = root.AddComponent<RouterAddressComponent, string>(request.Address);
            EntityRef<Scene> rootRef = root;
            EntityRef<RouterAddressComponent> routerAddressComponentRef = routerAddressComponent;
            await routerAddressComponent.Init();
            root = rootRef;
            routerAddressComponent = routerAddressComponentRef;
#if UNITY_WEBGL
            root.AddComponent<NetComponent, IKcpTransport>(new WebSocketTransport(routerAddressComponent.AddressFamily));
#else
            root.AddComponent<NetComponent, IKcpTransport>(new UdpTransport(routerAddressComponent.AddressFamily));
#endif
            root.GetComponent<FiberParentComponent>().ParentFiberId = request.OwnerFiberId;

            NetComponent netComponent = root.GetComponent<NetComponent>();
            EntityRef<NetComponent> netComponentRef = netComponent;
            IPEndPoint realmAddress = routerAddressComponent.GetRealmAddress(account);


            R2C_Login r2CLogin;
            using (Session session = await netComponent.CreateRouterSession(realmAddress, account, password))
            {
                C2R_Login c2RLogin = C2R_Login.Create();
                c2RLogin.Account = account;
                c2RLogin.Password = password;
                r2CLogin = (R2C_Login)await session.Call(c2RLogin);
            }

            if (r2CLogin.Error != ErrorCode.ERR_Success)
            {
                response.Error = r2CLogin.Error;
                response.Message = LoginAccountInputHelper.ErrorMessage(response.Error);
                return;
            }

            // 创建一个gate Session,并且保存到SessionComponent中
            netComponent = netComponentRef;
            Session gateSession = await netComponent.CreateRouterSession(NetworkHelper.ToIPEndPoint(r2CLogin.Address), account, password);
            gateSession.AddComponent<ClientSessionErrorComponent>();
            root = rootRef;
            root.AddComponent<SessionComponent>().Session = gateSession;
            C2G_LoginGate c2GLoginGate = C2G_LoginGate.Create();
            c2GLoginGate.Key = r2CLogin.Key;
            c2GLoginGate.GateId = r2CLogin.GateId;
            G2C_LoginGate g2CLoginGate = (G2C_LoginGate)await gateSession.Call(c2GLoginGate);

            if (g2CLoginGate.Error != ErrorCode.ERR_Success)
            {
                response.Error = g2CLoginGate.Error;
                response.Message = LoginAccountInputHelper.ErrorMessage(response.Error);
                return;
            }


            Log.Debug("登陆gate成功!");

            response.PlayerId = g2CLoginGate.PlayerId;
        }
    }
}
