using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;


namespace ET.Server
{
	[MessageSessionHandler(SceneType.Realm)]
	public class C2R_LoginHandler : MessageSessionHandler<C2R_Login, R2C_Login>
	{
		protected override async ETTask Run(Session session, C2R_Login request, R2C_Login response)
		{
			EntityRef<Session> sessionRef = session;
			Scene root = session.Root();
			EntityRef<Scene> rootRef = root;
			string account = LoginAccountInputHelper.Normalize(request.Account);
			var authentication = await LoginAccountHelper.AuthenticateAsync(
				root.GetComponent<DBManagerComponent>().GetZoneDB(root.Fiber().Zone).database, account, request.Password);
			if (authentication.Error != ErrorCode.ERR_Success)
			{
				response.Error = authentication.Error;
				response.Message = LoginAccountInputHelper.ErrorMessage(authentication.Error);
				return;
			}
			root = rootRef;
			ulong hash = (ulong)account.GetLongHashCode();
			
			ServiceDiscoveryProxy serviceDiscoveryProxy = root.GetComponent<ServiceDiscoveryProxy>();

			List<ServiceInfo> gates = serviceDiscoveryProxy.GetBySceneTypeAndZone(SceneType.Gate, root.Fiber().Zone);
			if (gates.Count == 0)
			{
				response.Error = ErrorCode.ERR_LoginUnavailable;
				response.Message = LoginAccountInputHelper.ErrorMessage(response.Error);
				return;
			}
			ServiceInfo gateServiceInfo = gates[(int)(hash % (ulong)gates.Count)];
			Log.Debug($"gate address: {gateServiceInfo.SceneName}");
			
			// 向gate请求一个key,客户端可以拿着这个key连接gate
			R2G_GetLoginKey r2GGetLoginKey = R2G_GetLoginKey.Create();
			r2GGetLoginKey.Account = account;
			r2GGetLoginKey.AccountId = authentication.AccountId;
			response.Address = gateServiceInfo.Metadata[ServiceMetaKey.InnerIPOuterPort];
			
			root = rootRef;
			MessageSender messageSender = root.GetComponent<MessageSender>();
			G2R_GetLoginKey g2RGetLoginKey = (G2R_GetLoginKey) await messageSender.Call(gateServiceInfo.ActorId, r2GGetLoginKey);
			if (g2RGetLoginKey.Error != ErrorCode.ERR_Success)
			{
				response.Error = g2RGetLoginKey.Error;
				response.Message = LoginAccountInputHelper.ErrorMessage(response.Error);
				return;
			}
			
			response.Key = g2RGetLoginKey.Key;
			response.GateId = g2RGetLoginKey.GateId;
			session = sessionRef;
			CloseSession(session).Coroutine();
		}

		private async ETTask CloseSession(Session session)
		{
			EntityRef<Session> sessionRef = session;
			await session.Root().TimerComponent.WaitAsync(1000);
			session = sessionRef;
			session?.Dispose();
		}
	}
}
