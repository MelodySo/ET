using UnityEngine;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>服务器单位快照及其本地模型，不承担战斗逻辑。</summary>
    [EnableClass]
    public sealed class BrawlPracticeUnitView
    {
        public long UnitId;
        public int ConfigId;
        public BrawlPracticeUnitKind Kind;
        public BrawlFaction Faction;
        public int Level;
        public float Health;
        public float MaxHealth;
        public int MaxAmmo;
        public float Ammo;
        public float ReloadRate;
        public int MaxSuperAmmo;
        public float SuperAmmo;
        public float SuperChargeRegenRate;
        public readonly Dictionary<StateId, bool> States = new();
        public Vector3 Position;
        public Vector2 MoveDirection;
        public Vector2 FacingDirection;
        public string ModelPath;
        public GameObject Model;
        public BrawlCharacterAnimationView Animation;
        public bool Removed;
        public bool ModelLoading;
        public bool ModelLoadFailed;
        public long SpawnCompleteAt;
        public long DeathDestroyAt;
        public readonly List<BrawlPositionSample> Samples = new();
    }
}
