using MemoryPack;
using System.Collections.Generic;

namespace ET
{
    [MemoryPackable]
    [Message(Opcode.RouterSync)]
    public partial class RouterSync : MessageObject
    {
        public static RouterSync Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<RouterSync>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public uint ConnectId { get; set; }
        [MemoryPackOrder(1)]
        public string Address { get; set; }
        public override void Dispose()
        {
            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.C2M_Reload)]
    [ResponseType(nameof(M2C_Reload))]
    public partial class C2M_Reload : MessageObject, ISessionRequest
    {
        public static C2M_Reload Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_Reload>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public string Account { get; set; }
        [MemoryPackOrder(2)]
        public string Password { get; set; }
        public override void Dispose()
        {
            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_Reload)]
    public partial class M2C_Reload : MessageObject, ISessionResponse
    {
        public static M2C_Reload Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_Reload>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        public override void Dispose()
        {
            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.G2C_TestHotfixMessage)]
    public partial class G2C_TestHotfixMessage : MessageObject, ISessionMessage
    {
        public static G2C_TestHotfixMessage Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_TestHotfixMessage>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public string Info { get; set; }
        public override void Dispose()
        {
            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.C2G_Benchmark)]
    [ResponseType(nameof(G2C_Benchmark))]
    public partial class C2G_Benchmark : MessageObject, ISessionRequest
    {
        public static C2G_Benchmark Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2G_Benchmark>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        public override void Dispose()
        {
            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.G2C_Benchmark)]
    public partial class G2C_Benchmark : MessageObject, ISessionResponse
    {
        public static G2C_Benchmark Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_Benchmark>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        public override void Dispose()
        {
            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.BrawlPracticeUnitInfo)]
    public partial class BrawlPracticeUnitInfo : MessageObject
    {
        public static BrawlPracticeUnitInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<BrawlPracticeUnitInfo>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long UnitId { get; set; }
        [MemoryPackOrder(1)]
        public int Kind { get; set; }
        [MemoryPackOrder(2)]
        public int ConfigId { get; set; }
        [MemoryPackOrder(3)]
        public int Faction { get; set; }
        [MemoryPackOrder(4)]
        public float X { get; set; }
        [MemoryPackOrder(5)]
        public float Y { get; set; }
        [MemoryPackOrder(6)]
        public float Z { get; set; }
        [MemoryPackOrder(7)]
        public float FacingX { get; set; }
        [MemoryPackOrder(8)]
        public float FacingZ { get; set; }
        [MemoryPackOrder(9)]
        public int Level { get; set; }
        [MemoryPackOrder(10)]
        public float Health { get; set; }
        [MemoryPackOrder(11)]
        public float MaxHealth { get; set; }
        [MemoryPackOrder(12)]
        public List<BrawlStateValue> States { get; set; } = new();

        [MemoryPackOrder(13)]
        public long SpawnCompleteAt { get; set; }
        [MemoryPackOrder(14)]
        public long DeathDestroyAt { get; set; }
        public override void Dispose()
        {
            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.C2G_BrawlPracticeEnter)]
    [ResponseType(nameof(G2C_BrawlPracticeEnter))]
    public partial class C2G_BrawlPracticeEnter : MessageObject, ISessionRequest
    {
        public static C2G_BrawlPracticeEnter Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2G_BrawlPracticeEnter>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int HeroId { get; set; }
        [MemoryPackOrder(2)]
        public long EntryId { get; set; }
        public override void Dispose()
        {
            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.G2C_BrawlPracticeEnter)]
    public partial class G2C_BrawlPracticeEnter : MessageObject, ISessionResponse
    {
        public static G2C_BrawlPracticeEnter Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_BrawlPracticeEnter>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public long RoomId { get; set; }
        [MemoryPackOrder(4)]
        public int LevelId { get; set; }
        [MemoryPackOrder(5)]
        public long MyUnitId { get; set; }
        [MemoryPackOrder(6)]
        public int CellSize { get; set; }
        [MemoryPackOrder(7)]
        public List<BrawlPracticeUnitInfo> Units { get; set; } = new();

        public override void Dispose()
        {
            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.C2G_BrawlPracticeExit)]
    [ResponseType(nameof(G2C_BrawlPracticeExit))]
    public partial class C2G_BrawlPracticeExit : MessageObject, ISessionRequest
    {
        public static C2G_BrawlPracticeExit Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2G_BrawlPracticeExit>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public long EntryId { get; set; }
        public override void Dispose()
        {
            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.G2C_BrawlPracticeExit)]
    public partial class G2C_BrawlPracticeExit : MessageObject, ISessionResponse
    {
        public static G2C_BrawlPracticeExit Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_BrawlPracticeExit>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        public override void Dispose()
        {
            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.C2G_BrawlMoveInput)]
    public partial class C2G_BrawlMoveInput : MessageObject, ISessionMessage
    {
        public static C2G_BrawlMoveInput Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2G_BrawlMoveInput>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long EntryId { get; set; }
        [MemoryPackOrder(1)]
        public long Sequence { get; set; }
        [MemoryPackOrder(2)]
        public float X { get; set; }
        [MemoryPackOrder(3)]
        public float Z { get; set; }
        /// <summary>
        /// X/Z 是移动意图；FacingX/Z 是独立的人物面向，不是技能摇杆瞄准方向。
        /// </summary>
        [MemoryPackOrder(4)]
        public float FacingX { get; set; }
        [MemoryPackOrder(5)]
        public float FacingZ { get; set; }
        public override void Dispose()
        {
            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.BrawlMoveState)]
    public partial class BrawlMoveState : MessageObject
    {
        public static BrawlMoveState Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<BrawlMoveState>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long UnitId { get; set; }
        [MemoryPackOrder(1)]
        public float X { get; set; }
        [MemoryPackOrder(2)]
        public float Z { get; set; }
        [MemoryPackOrder(3)]
        public float MoveX { get; set; }
        [MemoryPackOrder(4)]
        public float MoveZ { get; set; }
        [MemoryPackOrder(5)]
        public float FacingX { get; set; }
        [MemoryPackOrder(6)]
        public float FacingZ { get; set; }
        public override void Dispose()
        {
            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.G2C_BrawlMoveSnapshot)]
    public partial class G2C_BrawlMoveSnapshot : MessageObject, IMessage
    {
        public static G2C_BrawlMoveSnapshot Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_BrawlMoveSnapshot>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long EntryId { get; set; }
        [MemoryPackOrder(1)]
        public long RoomId { get; set; }
        [MemoryPackOrder(2)]
        public long Tick { get; set; }
        [MemoryPackOrder(3)]
        public long LastInputSequence { get; set; }
        [MemoryPackOrder(4)]
        public List<BrawlMoveState> Units { get; set; } = new();

        [MemoryPackOrder(5)]
        public List<BrawlPropertyState> Properties { get; set; } = new();

        [MemoryPackOrder(6)]
        public List<BrawlUnitStateChanges> States { get; set; } = new();

        [MemoryPackOrder(7)]
        public long MyUnitId { get; set; }
        [MemoryPackOrder(8)]
        public bool HasRoster { get; set; }
        [MemoryPackOrder(9)]
        public List<BrawlPracticeUnitInfo> Roster { get; set; } = new();

        [MemoryPackOrder(10)]
        public List<BrawlLifeEvent> LifeEvents { get; set; } = new();

        public override void Dispose()
        {
            ObjectPool.Recycle(this);
        }
    }

    // 服务器模拟毫秒；仅供未来表现，不由客户端驱动玩法计时。
    [MemoryPackable]
    [Message(Opcode.BrawlLifeEvent)]
    public partial class BrawlLifeEvent : MessageObject
    {
        public static BrawlLifeEvent Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<BrawlLifeEvent>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long UnitId { get; set; }
        [MemoryPackOrder(1)]
        public int Phase { get; set; }
        [MemoryPackOrder(2)]
        public long At { get; set; }
        [MemoryPackOrder(3)]
        public long EndsAt { get; set; }
        public override void Dispose()
        {
            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.BrawlStateValue)]
    public partial class BrawlStateValue : MessageObject
    {
        public static BrawlStateValue Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<BrawlStateValue>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int StateId { get; set; }
        [MemoryPackOrder(1)]
        public bool Value { get; set; }
        public override void Dispose()
        {
            ObjectPool.Recycle(this);
        }
    }

    // 仅最终布尔值发生变化时同步，不下发来源或计时器；顺序沿用外层Tick。
    [MemoryPackable]
    [Message(Opcode.BrawlUnitStateChanges)]
    public partial class BrawlUnitStateChanges : MessageObject
    {
        public static BrawlUnitStateChanges Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<BrawlUnitStateChanges>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long UnitId { get; set; }
        [MemoryPackOrder(1)]
        public List<BrawlStateValue> States { get; set; } = new();

        public override void Dispose()
        {
            ObjectPool.Recycle(this);
        }
    }

    // 当前练习房间所有单位的生命快照，包含不移动的 NPC。
    [MemoryPackable]
    [Message(Opcode.BrawlPropertyState)]
    public partial class BrawlPropertyState : MessageObject
    {
        public static BrawlPropertyState Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<BrawlPropertyState>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long UnitId { get; set; }
        [MemoryPackOrder(1)]
        public int Level { get; set; }
        [MemoryPackOrder(2)]
        public float Health { get; set; }
        [MemoryPackOrder(3)]
        public float MaxHealth { get; set; }
        public override void Dispose()
        {
            ObjectPool.Recycle(this);
        }
    }

    // 普攻释放意图；X/Z 为本次施放方向，独立于移动/人物面向。准备拖动不发送此消息。
    [MemoryPackable]
    [Message(Opcode.C2G_BrawlNormalAttack)]
    public partial class C2G_BrawlNormalAttack : MessageObject, ISessionMessage
    {
        public static C2G_BrawlNormalAttack Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2G_BrawlNormalAttack>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long EntryId { get; set; }
        [MemoryPackOrder(1)]
        public long Sequence { get; set; }
        [MemoryPackOrder(2)]
        public float X { get; set; }
        [MemoryPackOrder(3)]
        public float Z { get; set; }
        public override void Dispose()
        {
            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.BrawlProjectileState)]
    public partial class BrawlProjectileState : MessageObject
    {
        public static BrawlProjectileState Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<BrawlProjectileState>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long ProjectileId { get; set; }
        [MemoryPackOrder(1)]
        public int ConfigId { get; set; }
        [MemoryPackOrder(2)]
        public long OwnerUnitId { get; set; }
        [MemoryPackOrder(3)]
        public float X { get; set; }
        [MemoryPackOrder(4)]
        public float Y { get; set; }
        [MemoryPackOrder(5)]
        public float Z { get; set; }
        [MemoryPackOrder(6)]
        public float Radius { get; set; }
        [MemoryPackOrder(7)]
        public int Faction { get; set; }
        public override void Dispose()
        {
            ObjectPool.Recycle(this);
        }
    }

    // 房间所有在场弹丸的完整快照，缺席表示已移除；Sequence 区分同 Tick 多次释放。
    [MemoryPackable]
    [Message(Opcode.G2C_BrawlProjectileSnapshot)]
    public partial class G2C_BrawlProjectileSnapshot : MessageObject, IMessage
    {
        public static G2C_BrawlProjectileSnapshot Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_BrawlProjectileSnapshot>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long EntryId { get; set; }
        [MemoryPackOrder(1)]
        public long RoomId { get; set; }
        [MemoryPackOrder(2)]
        public long Sequence { get; set; }
        [MemoryPackOrder(3)]
        public long Tick { get; set; }
        [MemoryPackOrder(4)]
        public List<BrawlProjectileState> Projectiles { get; set; } = new();

        public override void Dispose()
        {
            ObjectPool.Recycle(this);
        }
    }

    public static partial class Opcode
    {
        public const ushort RouterSync = 10701;
        public const ushort C2M_Reload = 10702;
        public const ushort M2C_Reload = 10703;
        public const ushort G2C_TestHotfixMessage = 10704;
        public const ushort C2G_Benchmark = 10705;
        public const ushort G2C_Benchmark = 10706;
        public const ushort BrawlPracticeUnitInfo = 10707;
        public const ushort C2G_BrawlPracticeEnter = 10708;
        public const ushort G2C_BrawlPracticeEnter = 10709;
        public const ushort C2G_BrawlPracticeExit = 10710;
        public const ushort G2C_BrawlPracticeExit = 10711;
        public const ushort C2G_BrawlMoveInput = 10712;
        public const ushort BrawlMoveState = 10713;
        public const ushort G2C_BrawlMoveSnapshot = 10714;
        public const ushort BrawlLifeEvent = 10715;
        public const ushort BrawlStateValue = 10716;
        public const ushort BrawlUnitStateChanges = 10717;
        public const ushort BrawlPropertyState = 10718;
        public const ushort C2G_BrawlNormalAttack = 10719;
        public const ushort BrawlProjectileState = 10720;
        public const ushort G2C_BrawlProjectileSnapshot = 10721;
    }
}