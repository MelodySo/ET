using System;
using System.Collections.Generic;

namespace ET.Server
{
    public static class BrawlStateSyncHelper
    {
        public static void FillInitial(StateComponent states, List<BrawlStateValue> output)
        {
            foreach (StateId id in Enum.GetValues(typeof(StateId)))
                Add(output, id, states.Has(id));
        }

        public static void FillChanges(Unit unit, List<BrawlUnitStateChanges> output)
        {
            StateComponent states = unit.GetComponent<StateComponent>();
            if (states.PendingChanges.Count == 0) return;
            BrawlUnitStateChanges change = BrawlUnitStateChanges.Create();
            change.UnitId = unit.Id;
            foreach (var entry in states.PendingChanges) Add(change.States, entry.Key, entry.Value);
            output.Add(change);
            states.PendingChanges.Clear();
        }

        private static void Add(List<BrawlStateValue> output, StateId id, bool value)
        {
            BrawlStateValue item = BrawlStateValue.Create();
            item.StateId = (int)id;
            item.Value = value;
            output.Add(item);
        }
    }
}
