using System.Collections.Generic;

namespace ET
{
    [ComponentOf(typeof(Unit))]
    public sealed class StateComponent : Entity, IAwake
    {
        // 状态 -> 来源实例 -> 到期的服务器模拟毫秒；0代表由来源主动移除。
        public readonly Dictionary<StateId, Dictionary<long, long>> Sources = new();
        public readonly Dictionary<StateId, bool> Values = new();
        public readonly Dictionary<StateId, bool> PendingChanges = new();
    }
}
