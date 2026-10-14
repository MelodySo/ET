using System;
using System.Collections.Generic;

namespace ET
{
    [EntitySystemOf(typeof(StateComponent))]
    public static partial class StateComponentSystem
    {
        [EntitySystem]
        private static void Awake(this StateComponent self)
        {
            self.Sources.Clear();
            self.Values.Clear();
            self.PendingChanges.Clear();
        }

        public static bool Has(this StateComponent self, StateId id)
        {
            Validate(id);
            return self.Values.TryGetValue(id, out bool value) && value;
        }

        /// <summary>同一状态/来源重复添加只更新期限；不同 Buff 实例必须使用不同来源ID。</summary>
        public static void AddSource(this StateComponent self, StateId id, long sourceId, long expiresAtMilliseconds = 0)
        {
            Validate(id);
            if (sourceId == 0 || expiresAtMilliseconds < 0) throw new ArgumentException("Invalid state source or expiry.");
            if (!self.Sources.TryGetValue(id, out Dictionary<long, long> sources))
            {
                sources = new Dictionary<long, long>();
                self.Sources.Add(id, sources);
            }
            sources[sourceId] = expiresAtMilliseconds;
            SetValue(self, id, true);
        }

        public static bool RemoveSource(this StateComponent self, StateId id, long sourceId)
        {
            Validate(id);
            if (!self.Sources.TryGetValue(id, out Dictionary<long, long> sources) || !sources.Remove(sourceId)) return false;
            if (sources.Count == 0)
            {
                self.Sources.Remove(id);
                SetValue(self, id, false);
            }
            return true;
        }

        public static int RemoveSource(this StateComponent self, long sourceId)
        {
            List<StateId> affected = new();
            foreach (var entry in self.Sources)
                if (entry.Value.ContainsKey(sourceId)) affected.Add(entry.Key);
            foreach (StateId id in affected) self.RemoveSource(id, sourceId);
            return affected.Count;
        }

        public static void Expire(this StateComponent self, long simulationMilliseconds)
        {
            if (simulationMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(simulationMilliseconds));
            List<(StateId State, long Source)> expired = new();
            foreach (var entry in self.Sources)
                foreach (var source in entry.Value)
                    if (source.Value > 0 && source.Value <= simulationMilliseconds) expired.Add((entry.Key, source.Key));
            foreach (var entry in expired) self.RemoveSource(entry.State, entry.Source);
        }

        private static void SetValue(StateComponent self, StateId id, bool value)
        {
            bool old = self.Values.TryGetValue(id, out bool previous) && previous;
            if (old == value) return;
            self.Values[id] = value;
            self.PendingChanges[id] = value;
        }

        private static void Validate(StateId id)
        {
            if (!Enum.IsDefined(typeof(StateId), id)) throw new ArgumentException($"Unknown state {id}.");
        }
    }
}
