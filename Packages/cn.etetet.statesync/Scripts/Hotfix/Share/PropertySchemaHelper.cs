using System;

namespace ET
{
    public static class PropertySchemaHelper
    {
        public static bool IsInt(PropertyId id) => id == PropertyId.Level || id == PropertyId.MaxAmmo || id == PropertyId.MaxSuperAmmo;
        public static bool IsResource(PropertyId id) => id == PropertyId.Health || id == PropertyId.Shield ||
            id == PropertyId.Ammo || id == PropertyId.SuperAmmo;
        public static bool IsDerived(PropertyId id) => id == PropertyId.MaxHealth || id == PropertyId.MoveSpeed ||
            id == PropertyId.HealthRegenRate || id == PropertyId.ReloadRate || id == PropertyId.SuperChargeRegenRate;

        public static void Validate(PropertyId id, double value)
        {
            if (!Enum.IsDefined(typeof(PropertyId), id) || double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value) > float.MaxValue)
                throw new ArgumentException($"Invalid property: {id}={value}");
            if (IsInt(id) && (value != Math.Truncate(value) || value < int.MinValue || value > int.MaxValue))
                throw new ArgumentException($"Property {id} requires an int.");
        }
    }
}
