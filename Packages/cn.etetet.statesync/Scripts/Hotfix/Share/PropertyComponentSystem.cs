using System;
using System.Collections.Generic;

namespace ET
{
    [EntitySystemOf(typeof(PropertyComponent))]
    public static partial class PropertyComponentSystem
    {
        [EntitySystem]
        private static void Awake(this PropertyComponent self, Dictionary<PropertyId, double> initial)
        {
            self.BaseValues.Clear();
            self.Values.Clear();
            self.Modifiers.Clear();
            if (initial == null) throw new ArgumentNullException(nameof(initial));
            foreach (var entry in initial)
            {
                ValidateInput(entry.Key, entry.Value);
                self.BaseValues.Add(entry.Key, entry.Value);
            }
            Recalculate(self);
        }

        public static float GetFloat(this PropertyComponent self, PropertyId id)
        {
            return (float)self.GetDouble(id);
        }

        /// <summary>连续资源积分保留内部double精度，避免每Tick转float累计误差。</summary>
        public static double GetDouble(this PropertyComponent self, PropertyId id)
        {
            PropertySchemaHelper.Validate(id, 0);
            if (PropertySchemaHelper.IsInt(id)) throw new ArgumentException($"{id} is not float.");
            return Read(self.Values, id);
        }

        public static int GetInt(this PropertyComponent self, PropertyId id)
        {
            if (!PropertySchemaHelper.IsInt(id)) throw new ArgumentException($"{id} is not int.");
            return checked((int)Read(self.Values, id));
        }

        /// <summary>设置输入属性/当前状态；派生结果只能由重算产生。</summary>
        public static void Set(this PropertyComponent self, PropertyId id, double value)
        {
            ValidateInput(id, value);
            bool existed = self.BaseValues.TryGetValue(id, out double old);
            self.BaseValues[id] = value;
            try { Recalculate(self); }
            catch
            {
                if (existed) self.BaseValues[id] = old; else self.BaseValues.Remove(id);
                throw;
            }
        }

        public static void AddModifier(this PropertyComponent self, PropertyModifier modifier)
        {
            ValidateInput(modifier.Property, modifier.Value);
            if (modifier.Id == 0 || modifier.SourceInstanceId == 0 || PropertySchemaHelper.IsResource(modifier.Property))
                throw new ArgumentException("Modifier requires an identity/source and cannot target current resources.");
            self.Modifiers.Add(modifier.Id, modifier);
            try { Recalculate(self); }
            catch { self.Modifiers.Remove(modifier.Id); throw; }
        }

        public static bool RemoveModifier(this PropertyComponent self, long id)
        {
            if (!self.Modifiers.Remove(id, out PropertyModifier old)) return false;
            try { Recalculate(self); }
            catch { self.Modifiers.Add(id, old); throw; }
            return true;
        }

        public static void UpdateModifier(this PropertyComponent self, long id, double value)
        {
            PropertyModifier old = self.Modifiers[id];
            ValidateInput(old.Property, value);
            PropertyModifier updated = old;
            updated.Value = value;
            self.Modifiers[id] = updated;
            try { Recalculate(self); }
            catch { self.Modifiers[id] = old; throw; }
        }

        public static int RemoveSource(this PropertyComponent self, long sourceInstanceId)
        {
            List<PropertyModifier> removed = new();
            foreach (PropertyModifier modifier in self.Modifiers.Values)
                if (modifier.SourceInstanceId == sourceInstanceId) removed.Add(modifier);
            foreach (PropertyModifier modifier in removed) self.Modifiers.Remove(modifier.Id);
            try { Recalculate(self); }
            catch
            {
                foreach (PropertyModifier modifier in removed) self.Modifiers.Add(modifier.Id, modifier);
                throw;
            }
            return removed.Count;
        }

        private static void ValidateInput(PropertyId id, double value)
        {
            PropertySchemaHelper.Validate(id, value);
            if (PropertySchemaHelper.IsDerived(id)) throw new ArgumentException($"{id} is derived and read-only.");
            if (PropertySchemaHelper.IsResource(id) && value < 0) throw new ArgumentException($"{id} cannot be negative.");
        }

        private static double Read(Dictionary<PropertyId, double> values, PropertyId id) => values.TryGetValue(id, out double value) ? value : 0;

        private static void Recalculate(PropertyComponent self)
        {
            // 先在临时集合校验整个结果，异常不能留下半更新的状态；状态值只从其输入复制。
            Dictionary<PropertyId, double> next = new(self.BaseValues);
            foreach (PropertyModifier modifier in self.Modifiers.Values)
                next[modifier.Property] = Read(next, modifier.Property) + modifier.Value;
            foreach (var entry in next)
            {
                PropertySchemaHelper.Validate(entry.Key, entry.Value);
            }
            Calculate(next, PropertyId.MaxHealth, PropertyId.BaseMaxHealth, PropertyId.MaxHealthBonus, PropertyId.MaxHealthBonusRatio);
            Calculate(next, PropertyId.MoveSpeed, PropertyId.BaseMoveSpeed, PropertyId.MoveSpeedBonus, PropertyId.MoveSpeedBonusRatio);
            Calculate(next, PropertyId.HealthRegenRate, PropertyId.BaseHealthRegenRate, PropertyId.HealthRegenRateBonus, PropertyId.HealthRegenRateBonusRatio);
            Calculate(next, PropertyId.ReloadRate, PropertyId.BaseReloadRate, PropertyId.ReloadRateBonus, PropertyId.ReloadRateBonusRatio);
            Calculate(next, PropertyId.SuperChargeRegenRate, PropertyId.BaseSuperChargeRegenRate, PropertyId.SuperChargeRegenRateBonus, PropertyId.SuperChargeRegenRateBonusRatio);
            self.Values.Clear();
            foreach (var entry in next) self.Values.Add(entry.Key, entry.Value);
        }

        private static void Calculate(Dictionary<PropertyId, double> values, PropertyId result, PropertyId basis, PropertyId flat, PropertyId ratio)
        {
            double value = Math.Max(0, (Read(values, basis) + Read(values, flat)) * (1 + Read(values, ratio)));
            PropertySchemaHelper.Validate(result, value);
            values[result] = value;
        }
    }
}
