namespace ET.Server
{
    public static partial class GateSessionKeyComponentSystem
    {
        public static void Add(this GateSessionKeyComponent self, long key, string account, long accountId)
        {
            self.sessionKey.Add(key, (account, accountId));
            self.TimeoutRemoveKey(key).Coroutine();
        }

        public static (string Account, long AccountId) Consume(this GateSessionKeyComponent self, long key)
        {
            if (!self.sessionKey.TryGetValue(key, out var ticket)) return default;
            self.sessionKey.Remove(key);
            return ticket;
        }

        public static void Remove(this GateSessionKeyComponent self, long key)
        {
            self.sessionKey.Remove(key);
        }

        private static async ETTask TimeoutRemoveKey(this GateSessionKeyComponent self, long key)
        {
            EntityRef<GateSessionKeyComponent> selfRef = self;
            await self.Root().TimerComponent.WaitAsync(LoginAccountPolicy.GateKeyLifetimeMilliseconds);
            self = selfRef;
            self?.sessionKey.Remove(key);
        }
    }
}
