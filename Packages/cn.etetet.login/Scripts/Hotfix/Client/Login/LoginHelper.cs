namespace ET.Client
{
    public static class LoginHelper
    {
        public static async ETTask Login(Scene root, string address, string account, string password)
        {
            EntityRef<Scene> rootRef = root;
            ClientSenderComponent previous = root.GetComponent<ClientSenderComponent>();
            if (previous != null) await previous.DisposeAsync();
            root = rootRef;
            root.GetComponent<PlayerComponent>().MyId = 0;
            
            ClientSenderComponent clientSenderComponent = root.AddComponent<ClientSenderComponent>();
            
            EntityRef<ClientSenderComponent> senderRef = clientSenderComponent;
            long playerId;
            try
            {
                playerId = await clientSenderComponent.LoginAsync(address, account, password);
            }
            catch
            {
                clientSenderComponent = senderRef;
                if (clientSenderComponent != null) await clientSenderComponent.DisposeAsync();
                throw;
            }

            root = rootRef;
            root.GetComponent<PlayerComponent>().MyId = playerId;
            
            await EventSystem.Instance.PublishAsync(root, new LoginFinish());
        }
    }
}
