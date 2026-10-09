using System;


namespace ET.Server
{
	using System.Security.Cryptography;
	[MessageHandler(SceneType.Gate)]
	public class R2G_GetLoginKeyHandler : MessageHandler<Scene, R2G_GetLoginKey, G2R_GetLoginKey>
	{
		protected override async ETTask Run(Scene scene, R2G_GetLoginKey request, G2R_GetLoginKey response)
		{
			if (request.AccountId <= 0 || string.IsNullOrEmpty(request.Account))
			{
				response.Error = ErrorCode.ERR_LoginCredentials;
				return;
			}
			GateSessionKeyComponent keys = scene.GetComponent<GateSessionKeyComponent>();
			byte[] bytes = new byte[sizeof(long)];
			long key;
			using (RandomNumberGenerator random = RandomNumberGenerator.Create())
			{
				do
				{
					random.GetBytes(bytes);
					key = BitConverter.ToInt64(bytes, 0);
				} while (key == 0 || keys.sessionKey.ContainsKey(key));
			}
			keys.Add(key, request.Account, request.AccountId);
			response.Key = key;
			response.GateId = scene.Id;
			await ETTask.CompletedTask;
		}
	}
}
