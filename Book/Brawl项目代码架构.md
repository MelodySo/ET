# Brawl Demo：当前代码架构与面试定位

核查日期：2026-10-16。对象是本机这份 ET 工程，不是上游所有 ET 版本。原稿依据源码、生成配置和已有日志整理；本日后续已实现雪莉／柯尔特战斗 AI，并同步更新下列 AI 说明。最新编译和手测范围见 AI 交接首节，未启动服务或运行游戏，演示效果仍由用户验收。

配套阅读：[AI 协作与个人复盘](C:/Work/UnityProjects/ET/Book/Brawl_AI协作与个人复盘.md)、[面试速读](C:/Work/Website/zhaoye/面试题和复习/AI使用/ET_Brawl面试速读.md)、[客户端战斗表现](C:/Work/UnityProjects/ET/Book/Brawl客户端战斗表现.md)、[公牛 AI 交接](C:/Work/UnityProjects/ET/Book/Braw_AI开发交接.md)。相机、弹丸特效的历史实现记录仍留在[原交接](C:/Work/UnityProjects/ET/Book/Brawl开发交接.md)，不搬迁。

## 1. 用一分钟描述项目

这是基于 ET 的小地图、服务器权威战斗 Demo。Unity 客户端负责输入、场景与资源加载、快照插值、角色动画和技能表现；服务器负责单位创建、移动碰撞、技能合法性、弹丸命中、属性、死亡复活及 Bot 决策。配置由 Excel 经 Luban 导出，共享数据结构和数学逻辑放在不依赖 Unity 场景对象的层。

当前练习赛是一名玩家加一个战斗 Bot，雪莉和柯尔特互为对手；房间与登录 Session 关联，运行在 Gate 所在 Fiber 中。**不要把它说成已经完成独立战斗服、多人匹配、分布式房间迁移、断线重连或大规模并发验证。** 框架具备的基础设施不等于此 Demo 已经接入的功能。

```text
Unity 输入/UI
  ├─ 本地瞄准范围：立即更新，只负责预览
  └─ 网络请求：进入/退出、移动意图、施法方向与槽位
          ↓
Gate Session → 练习赛房间 Scene → Unit / 弹丸 / 生命周期
          ↓ 固定步长模拟、碰撞、伤害、AI
     权威快照与表现通知
          ↓
Unity 快照缓冲 → 延迟插值位置 → 模型/动画/特效/相机
```

## 2. 包与代码层次

| 包/层 | 在本项目中的职责 |
| --- | --- |
| `cn.etetet.core` | Entity/组件、Fiber、ETTask、消息与基础生命周期 |
| `cn.etetet.loader` | 启动、CodeMode、配置及代码加载等基础入口 |
| `cn.etetet.login` | 路由查询、Realm 认证、Gate 登录、MongoDB 账号 |
| `cn.etetet.brawl` | Brawl Excel 源表、关卡编辑器、地图/角色/动画/指示器资源、地形类型 |
| `cn.etetet.statesync` | 当前 Brawl 业务主体：房间、移动、技能、弹丸、AI、客户端 UI 与表现 |
| `cn.etetet.config` | 配置导出工具以及实际生成的配置类/数据入口 |
| `cn.etetet.behaviortree` | 通用行为树数据、执行分发；具体 Brawl 节点由业务包实现 |
| `cn.etetet.test` | ET 自身的测试发现、执行和结果汇总 |
| `cn.etetet.unitybridge` / `harness` | 编辑器自动化桥接 / AI 开发规则与技能路由，不是战斗运行逻辑 |
| `Model` / `Hotfix` | 数据、组件声明 / 业务执行逻辑；`Share` 为共享部分，另有 Client/Server |
| `ModelView` / `HotfixView` | Unity 表现数据与实际执行逻辑，允许操作 GameObject、Animator、UI |
| `Editor` | 关卡编辑、资源生成与校验，不是服务器运行代码 |

ET 的 Entity/组件/System 组织不是 Unity DOTS 的代称；不能因为采用 ECS 风格就声称用了 Burst、Jobs 或连续内存并行计算。服务器不通过 Unity GameObject/PhysX 执行这套战斗。

Fiber 提供业务执行上下文，异步等待期间其他任务仍可推进，不能把“同 Fiber”理解为整个异步流程不会发生状态变化。`EntityRef<T>` 根据 InstanceId 检查旧引用是否仍对应有效实体，适用于等待后重新获取对象；它不是弱引用或线程锁。源码：[EntityRef.cs](C:/Work/UnityProjects/ET/Packages/cn.etetet.core/Scripts/Core/Share/Entity/EntityRef.cs)。

## 3. 登录与 MongoDB：服务器基础应该怎么讲

客户端入口：[Main2NetClient_LoginHandler.Run](C:/Work/UnityProjects/ET/Packages/cn.etetet.login/Scripts/Hotfix/Client/NetClient/Main2NetClient_LoginHandler.cs)。

1. `RouterAddressComponent.Init` 查询 HTTP `/get_router`，获取路由和 Realm 地址信息。
2. 创建到 Realm 的 Router Session，发送 `C2R_Login`。
3. [C2R_LoginHandler.Run](C:/Work/UnityProjects/ET/Packages/cn.etetet.login/Scripts/Hotfix/Server/Realm/C2R_LoginHandler.cs) 调用 `LoginAccountHelper.AuthenticateAsync` 查询 MongoDB；本地 Demo 约定首次登录自动注册。
4. 账号唯一索引防止重复创建；并发注册发生重复键时重新读取并验证，而不是覆盖已有密码。密码使用带盐 PBKDF2 哈希，见 [LoginAccountHelper](C:/Work/UnityProjects/ET/Packages/cn.etetet.login/Scripts/Hotfix/Server/Realm/LoginAccountHelper.cs)、[LoginPasswordHelper](C:/Work/UnityProjects/ET/Packages/cn.etetet.login/Scripts/Hotfix/Server/Realm/LoginPasswordHelper.cs)。
5. Realm 选择 Gate，请求登录 Key，并传递持久化 AccountId；客户端携 Key 发送 `C2G_LoginGate`，建立后续业务 Session。

MongoDB 用来持久化账号，**不是每 50ms 保存坐标、不是用数据库进行战斗同步**。当前房间是内存状态。

`/get_router` 连接被拒绝，只能说明对应地址/端口当前不可达，不应直接认定“密码错误”或“数据库错误”。先检查服务是否退出、HTTP 是否监听，再看启动日志、MongoDB 状态。启动终端出现 `>` 是 ET 控制台提示，不等于 HTTP 和所有场景均成功启动；也不要随意输入非支持命令。

安全边界：客户端记住账号密码用了 PlayerPrefs，其中密码不是安全存储；自动注册也只是 Demo 策略。不要对外宣称具备生产级认证、安全存储、完整限流和运维能力。

## 4. 进入场景：本地训练场与联网练习赛不同

主要入口：[BrawlHeroMenuHelper](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/BrawlHeroMenuHelper.cs) 的 `OnTrainingClicked` / `OnPracticeClicked` → [BrawlTrainingHelper.Enter](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/BrawlTrainingHelper.cs)。

| 分支 | 当前作用 |
| --- | --- |
| 训练场，关卡 `10101` | 保留较早的本地场景/模型预览路径；不要误说成已完整接入服务器战斗的离线版本 |
| 练习赛，玩法 `102`、关卡 `10201` | 调用 `BrawlPracticeClientHelper.Enter`，服务器创建玩家、与玩家互配的雪莉／柯尔特 Bot 和 NPC，随后以服务器返回的房间/单位数据加载表现 |

训练场的场景加载本身可以本地执行，不代表当前应用从启动到登录都不需要服务器。面试展示联网移动/射击/AI，应进入**练习赛**。

客户端加载过程：

1. 确定英雄和关卡，练习赛先完成进入请求，拿到 `EntryId/RoomId/MyUnitId`、单位列表及关卡信息。
2. 读取 `BrawlLevel.SceneAsset`，按当前 `AddressByFileName` 约定取场景文件名作为地址。
3. `YooAssets.GetPackage("DefaultPackage").LoadSceneAsync(address, LoadSceneMode.Additive)`，等待成功后激活场景。不是跳入原 MMO 示例 Map2。
4. 根据 `StyleId` 找地形模型；按 `MapGrid` 逐格创建非空模型，模型地址来自风格配置。
5. [BrawlModelHelper.InstantiateModel](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/BrawlModelHelper.cs) 按配置加载英雄/NPC，缓存资源句柄，创建实例并绑定角色动画；练习赛使用服务器单位名册。
6. 绑定相机、移动快照、弹丸与瞄准预览；后续单位新增/死亡重生继续同步表现。

`Cleanup` 不只是 Destroy 模型：还要阻止重复清理、取消手势/旧异步流程、退出服务器房间、清除移动/弹丸/动画表现、恢复大厅场景和相机、卸载附加场景、释放模型句柄。等待资源加载期间用户退出，也是必须处理的路径。

## 5. 服务器房间与单位所有权

入口：[BrawlPracticeHelper.Enter / Exit](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Scripts/Hotfix/Server/Brawl/BrawlPracticeHelper.cs)。

```text
登录 Session
└─ BrawlPracticeSessionComponent：EntryId、MyUnitId、输入序列等
   └─ 练习赛 Room Scene（当前仍在同一 Gate Fiber）
      ├─ UnitComponent → 玩家 / Bot / NPC / 出生点单位
      ├─ BrawlProjectileRoomComponent → 活动弹丸、通知、ClearedGrass
      └─ BrawlLifecycleRoomComponent → 参与者槽位、复活点、NPC待复活队列
```

`EntryId` 区分进入批次：相同批次/英雄可返回已有结果，旧退出消息不能清理新房间。服务器校验玩法开启、人数、出生点、数组、模型无关的几何配置等。当前协议名虽然是 `C2G`，也不表示已经部署了独立战斗服。

[BrawlUnitFactory.Create](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Scripts/Hotfix/Server/Brawl/BrawlUnitFactory.cs) 创建逻辑单位，按英雄/NPC 配置绑定属性、状态和生命周期；英雄还有移动、技能、弹药与大招充能，配置了行为树的 Bot 额外挂 AI 组件。NPC 不因有血量就必须有移动、AI 或 Animator。

生命周期入口：[BrawlLifecycleHelper.Initialize / Tick](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Scripts/Hotfix/Server/Brawl/BrawlLifecycleHelper.cs) 和 [BrawlUnitLifeHelper](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Scripts/Hotfix/Server/Brawl/BrawlUnitLifeHelper.cs)。英雄死亡等待后销毁旧实体，在绑定出生点创建新实体；`MyUnitId` 随之改变。NPC 按配置决定死亡停留和原地复活。不能让旧输入/动画/异步加载作用到新实体。

`PropertyComponent` 管生命、速度、弹药等数值；`StateComponent` 管 Dead、Spawning、无敌、受控等状态；Animator 仅解释结果。**不是用 Animator 状态或 AnimationEvent 决定死亡、移动是否合法。**

## 6. 移动：权威状态同步，不是帧同步

服务端核心：[BrawlPracticeMovementHelper.SetInput / Update](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Scripts/Hotfix/Server/Brawl/BrawlPracticeMovementHelper.cs)。客户端：[BrawlPracticeMovementViewHelper.Update / Receive / SendInput / Stop](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/BrawlPracticeMovementViewHelper.cs)。

| 概念 | 当前规则与意义 |
| --- | --- |
| 模拟步长 | 50ms，即每秒 20 个逻辑步，不是移动速度 |
| 速度 | 当前暂用 `MoveSpeedTier` 直接表示米/秒；3m/s 每步无阻挡位移为 0.15m |
| 输入 | 客户端发方向和序列，不提交一个可直接采信的世界坐标 |
| 输入频率 | 常规约 20Hz，开始/停止等状态变化可立即发送；服务器另有限流 |
| 输入保活 | 超过 500ms 没有有效更新则停止，避免断输入后一直跑 |
| 补步 | 累积时间推进，单次最多补 5 步，防止卡顿造成无上限追赶 |
| 位置快照 | 正常约 20Hz；一次 Update 补多步后发送最终移动快照，并非每个补步必发一包 |
| 客户端显示 | 延后 2 Tick，约 100ms 的插值缓冲，保存最多 32 份历史；不是总网络延迟仅100ms |
| 预测/回滚 | 当前没有本地预测、服务端对账重演或客户端外推；收到确认序列也不等于已经实现预测 |

常量来源：[BrawlMovementSettings](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Scripts/Model/Share/BrawlMovementSettings.cs)。服务器检查 EntryId、输入序列、有限数值、范围和频率，只让 Session 操作自己的单位。

摇杆：[BrawlJoystickHelper](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/BrawlJoystickHelper.cs)，`DeadZone=0.2f`、`HandleRange=60`。12 UI 单位内不移动，死区外归一化为全速方向；摇杆视觉位置仍可连续变化。WASD 对角线也应归一化，避免 √2 倍速度。

### 6.1 主要协议字段

源定义：[StateSync_C_10700.proto](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Proto/StateSync_C_10700.proto)。下面是业务意义，不等于对实际网络字节格式的说明。

| 消息/字段 | 含义 |
| --- | --- |
| `C2G_BrawlPracticeEnter` | 选择英雄并请求进入；`EntryId` 为本次进入标识 |
| `G2C_BrawlPracticeEnter` | 返回 `RoomId/LevelId/MyUnitId/CellSize/Units` 等初始状态 |
| `C2G_BrawlMoveInput` | `Sequence` 为输入序号；`X/Z` 为移动意图；`FacingX/Z` 为面向意图，不是位置 |
| `G2C_BrawlMoveSnapshot` | `Tick` 为模拟时间轴；`LastInputSequence` 为已处理输入序号；`Units` 为权威运动状态 |
| 快照的 `Properties/States` | 数值/状态同步，不应从模型是否播放某动画反推这些值 |
| `HasRoster/Roster/MyUnitId/LifeEvents` | 单位名册及生命阶段变化，支持旧实体移除、新实体出现与主角替换 |
| `C2G_BrawlCastSkill` | 发送槽位 `Slot`、方向 `X/Z` 和 `Sequence`；不由客户端指定最终伤害 |
| `G2C_BrawlProjectileSnapshot` | 弹丸状态、事件、已清草格；`Sequence` 能区分同一 Tick 的多次发送 |

`EntryId/RoomId/Tick/Sequence/UnitId` 分别解决不同层面的身份、时序和生命周期问题，不应该合并成一个“消息编号”。

### 6.2 碰撞、滑墙和中心坐标

共享地形：[BrawlTerrainMovementHelper](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Scripts/Hotfix/Share/BrawlTerrainMovementHelper.cs)。格索引 `row * ColumnCount + column`；格中心坐标：

```csharp
x = (column - (ColumnCount - 1) / 2f) * CellSize;
z = (row    - (RowCount    - 1) / 2f) * CellSize;
```

现在 `CellSize=1`，20×20 地图格中心 -9.5 到9.5，外边界 -10到10。世界原点在关卡中心，不改变表中一维索引。编辑器扩图仍保留原有左下角格数据，不等于已实现中心锚定扩图。

移动检查墙、水、地图边缘，以及当前配置为 `BlocksMovement=true` 的 NPC；平地和草可走。英雄半径读表，目前0.3；高度1.2是预留几何属性，当前地面移动是 XZ 二维圆形检查，不是完整3D胶囊物理。英雄之间暂不互相阻挡。

[BrawlUnitMovementHelper.Move / CanOccupy](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Scripts/Hotfix/Server/Brawl/BrawlUnitMovementHelper.cs) 采用细分位移并分 X/Z 轴限位，让法向分量停住、可行的切向继续移动。NPC 几何通过 [BrawlBlockGeometryHelper](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Scripts/Hotfix/Share/BrawlBlockGeometryHelper.cs) 统一计算：`CollisionWidth=0` 时 `CollisionLength` 是圆柱直径，否则为矩形完整长宽；Pivot 是世界距离偏移。

这是易解释、共享规则的首版几何移动，不是通用刚体碰撞引擎。分轴顺序有偏向，贴墙实际速度可能降低；不能宣称任意形状和高速物体都已解决。

## 7. 技能、弹药与大招

链路：[BrawlSkillCastHelper.Cast / TryStart](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Scripts/Hotfix/Server/Brawl/BrawlSkillCastHelper.cs) → `BrawlSkillEmitterHelper.Create` → [BrawlSkillSequenceHelper.Start / Tick](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Scripts/Hotfix/Server/Brawl/BrawlSkillSequenceHelper.cs)。

服务器根据英雄和 Normal/Super 槽位选择技能，验证存活/出生/受控、方向、冷却、资源、发射器和碰撞规则，再扣资源并创建发射计划。拒绝的请求不生成权威弹丸。当前不能把“摇杆松手”直接当作“技能成功施放”。

| 实现 | 主要行为 |
| --- | --- |
| `BrawlShotgunEmitter.Emit` | 扇形散射，一次技能一轮多弹；当前雪莉普攻5枚，不是5次技能 |
| `BrawlRevolverEmitter.Emit` | 条带范围内偏移发射、分时连发；柯尔特普攻6枚、大招12枚 |
| `BrawlSkillSequenceHelper.Tick` | 按开始时间加每发绝对延时判断，不把0.08333秒逐次向50ms取整累加；执行仍受Tick精度限制 |
| `BrawlAmmoHelper` | 普攻用连续浮点弹药值表示装填进度，满一格才能消耗；一次技能扣一格，连发期间暂停装填 |
| `BrawlSuperChargeHelper` | 有效造成伤害后按弹丸配置回充；大招消耗一格，死亡保存到参与者数据、重生恢复 |

首次入场与重生的普攻弹药均为容量的1/3，由 BrawlAmmoHelper 显式写入 PropertyComponent；小数装填进度不会因为一次射击被全部清零。出生期间暂停装填的现有规则不变。默认大招自动恢复速率为0。无敌目标虽可能发生碰撞通知，但没有实际扣血就不能据此充能。

发射时锁定本次方向，后续连发从英雄当时的位置发出。弹丸数值按配置和施放等级形成快照。雪莉普攻当前有特定策略：普通霰弹分支不使用配置的普通冷却，而主要依赖弹药等约束；不要把表内每个字段都说成所有发射器一致生效。

当前雪莉 `10101/10102`、柯尔特 `40101/40102` 已有对应发射与碰撞规则；文字描述不代表实现，例如雪莉大招描述里的击退还没有完成。

## 8. 弹丸与伤害：不靠 Unity Trigger 判定

入口：[BrawlProjectileHelper.Tick / SendSnapshot](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Scripts/Hotfix/Server/Brawl/BrawlProjectileHelper.cs)、[BrawlProjectileCollisionHelper.Advance](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Scripts/Hotfix/Server/Brawl/BrawlProjectileCollisionHelper.cs)。弹丸是房间下独立逻辑实体，不是给每颗子弹创建完整英雄 Unit。

1. 用速度、步长、剩余射程和 Lifetime 限定本次移动线段。
2. 收集地形和单位候选，进行 XZ 连续扫掠，考虑目标从上一位置到当前位置的相对运动，减少穿透薄目标。
3. 圆形目标使用半径扩张；矩形使用圆与矩形的扩张几何，处理圆角而非简单无限扩大AABB。
4. 按接触先后排序，同距处理墙等阻挡优先；每颗弹丸对同一单位/地形去重。
5. 按碰撞规则决定“阻挡弹丸”和“执行命中事件”，这两个结果相互独立。
6. [BrawlProjectileHitHelper.Apply / ApplyEvent](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Scripts/Hotfix/Server/Brawl/BrawlProjectileHitHelper.cs) 执行 Damage，处理无敌、减伤、生命下限、死亡检查与充能。
7. 阻挡、有效命中数上限、射程或寿命结束分别形成原因，客户端据此表现。

`MaxHitCount=0` 表示不以命中次数限制结束，不代表穿透所有墙；`Lifetime=0` 表示没有额外寿命限制，不代表弹丸永不销毁。当前仍有射程与阻挡规则。

碰撞配置：[BrawlCollisionRuleHelper](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Scripts/Hotfix/Share/BrawlCollisionRuleHelper.cs)。`RequiredLayerMask` 必须全部满足，`ExcludedLayerMask` 任一命中即排除，`RelationMask` 区分敌对/非敌对。同组匹配行的 `BlockProjectile`、`ApplyHitEvents` 分别取或，不能因为多行命中重复扣血。移动阻挡与弹丸阻挡不是同一个开关。

草被摧毁保存在房间的 `ClearedGrass` 集合，不改共享 `BrawlLevel.MapGrid`，否则一个房间会污染其他房间。当前客户端清草回调还未完成模型移除，不能演示成“已经清掉可见草丛”。

客户端：[BrawlProjectileViewHelper](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/BrawlProjectileViewHelper.cs) 缓冲快照，按渲染时间线呈现并对事件去重；[BrawlProjectileVisualHelper](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/BrawlProjectileVisualHelper.cs) 负责飞行、命中、自然消失特效。资源缓存不等于对象池，当前仍有 Instantiate/Destroy，未实现完整特效池。

## 9. 雪莉／柯尔特战斗 AI 与寻路

配置资源：[ShellyCombat.asset](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Settings/BrawlAI/ShellyCombat.asset)、[ColtCombat.asset](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Settings/BrawlAI/ColtCombat.asset)。分别可编辑、共用节点；由资源导出相应 CodeMode 配置工厂，服务器按英雄 ID 选树。旧 BullChase 为历史资源，不参与当前练习赛配对。

```text
BrawlAIRoot
└─ Selector
   ├─ 有进行中的连发 → Idle 等待结束
   ├─ 大招距离／视线条件 → UseSkill(Super)
   ├─ 普攻距离／视线条件 → UseSkill(NormalAttack)
   ├─ 普攻位置畅通 → Idle 等待装填／冷却
   └─ Chase
```

`BTHelper.RunTree` / `BTDispatcher.Handle` 分发具体 Handler。本项目这条树采用返回整数，0表示成功、非0表示失败；不要未经核对就套用另一套库的 Success/Failure/Running 机制。

[BrawlAIHelper.Tick / GetTarget / Chase / Idle](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Scripts/Hotfix/Server/Brawl/BrawlAIHelper.cs) 在服务器模拟步内执行。没有 5 米感知上限，追击有效存活目标；每个技能按自身 `SkillRangeParam1 - RangeMargin` 独立判断距离，余量默认 0.5 米且结果不小于 0。当前雪莉两种技能 7.17 米，柯尔特普攻 8.5 米、大招 11.5 米；先大招再普攻。自身不能行动或目标死亡／无效时停止，目标出生期间追踪但不攻击。移动只输出意图，最终位置仍走 `BrawlUnitMovementHelper`。

普通／大招分开用宽 0.2 米的有限矩形检查墙体遮挡，并从各自技能的弹丸 `CollisionRuleGroupId` 解析规则，复用 `BrawlCollisionRuleHelper.Match` 的标签／阵营匹配，仅以 `BlockProjectile` 决定阻挡。草丛不在本轮视线候选中；查询不执行 `ApplyHitEvents`，不做伤害或破墙。位置不满足时继续寻路，满足但缺弹／冷却时停住。AI 经 `BrawlAICombatHelper` 调已有 `BrawlSkillCastHelper.TryStart`，复用弹药、充能、连发、伤害与施法表现通知；不模拟每颗子弹能否命中，也没有预测瞄准、侧移、逃跑或复杂仇恨系统。

寻路：[BrawlPathfindingHelper.Find / CanTraverse](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Scripts/Hotfix/Server/Brawl/BrawlPathfindingHelper.cs)。

- 先尝试真实几何直达，成功就不做 A*。
- 否则用八方向网格 A*，欧氏距离启发式，排序集合维护开放节点；记录代价、父节点和关闭集合。
- 从实际站位连接附近可达格中心，不直接传送到某个格中心；边是否可走沿用角色半径、墙、水、边界和 NPC 阻挡检查，避免只看格子类型导致斜穿墙角。
- 追击节点默认约250ms重算，搜索节点上限10000；目标改变、路径失效等处理在当前逻辑中完成。跟随路径时跳过可直达的中间点，临近终点缩短位移，避免越过后震荡。
- 没有路径就停住，之后重新尝试；当前不用 Unity NavMesh。

性能边界：当前搜索分配数组/集合、几何检查扫描单位，适合少量 Bot 的原型，不能声称已验证数百 Bot。后续可评估空间索引、复用搜索缓冲、重规划预算和动态障碍版本；网格中心搜索也可能漏掉几何上可行但没有合适中心节点的窄路径。

## 10. 客户端表现：这部分最适合客户端面试展开

### 10.1 移动动画、朝向、原生步幅

[BrawlCharacterAnimationHelper.Bind / Reset / Update / Clear](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/BrawlCharacterAnimationHelper.cs)：

- 各角色 Prefab 使用自己的 Override 模板，保留固定武器和对应动作资源；运行时再创建独立 AnimatorOverrideController，避免修改共享资源污染其他角色。
- 通用控制器分 Locomotion、UpperBody、FullBody；上半身和全身槽位是扩展契约，不代表所有攻击和死亡动画已经接通。
- 按**渲染模型实际位移**计算速度和移动方向，再转换到角色本地前后左右；顶墙有输入但无位移时不应持续跑步。
- 朝向与移动方向分离，平滑转向；初始化、重生或位置大幅跳变时清除旧速度，防止一次跳变被误判成高速奔跑。
- 最新移动 Clip 改用可读取原生根位移速度的 RM 版本，但 `applyRootMotion=false`。只利用动画步幅信息调播放率，不让 Animator 推动角色位置。
- 目标播放率约为 `实际显示速度 / 模型上的原生动作速度`。原生速度要考虑 Clip、Avatar.humanScale 和实例缩放，不能直接拿 Clip.averageSpeed 当最终世界米/秒。

这是步频匹配，不是脚部 IK，不保证所有转向、BlendTree 过渡、地形和动画都完全无脚滑。当前攻击表现通知与开火/死亡动作驱动仍未完整接通；`SetFacing` 等接口留有扩展空间。

**配置与文档不一致提醒：** 动画交接提到公牛 `SalarymanDefault`，但本次核对的已生成英雄201配置仍为 `BasicMaleDefault`。当前运行时由 `ModelPath` 决定加载哪一个；面试不要按文字记录断言一定是 Salaryman。此次未改表或模型。

### 10.2 技能瞄准预览

[BrawlSkillAimVisualHelper.Initialize / Show / Hide / Clear](C:/Work/UnityProjects/ET/Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/BrawlSkillAimVisualHelper.cs) 接入 `IBrawlSkillAimPreview`。使用自有 Prefab、网格与 URP Shader，借鉴资源包思路而不直接依赖它的运行时代码。

立即响应本地摇杆，跟随已显示的角色位置；四顶点网格配合 Shader 表达矩形/扇形/圆形，形状或尺寸变化时才重建几何，材质参数用 MaterialPropertyBlock。显示预览不是移动预测，也不是服务器许可。

`SkillRangeType`：1矩形、2扇形、3圆。`SkillCastRangeType` 是另一组含义：0回退技能范围、1矩形、2抛物线；当前抛物线不支持，不能把两个字段里的2混为一谈。圆形尚无当前业务验证入口。不做遇墙截断，取消/失焦/死亡/出生/退出时隐藏并清理。

相机仍见原交接：`BrawlCameraComponentSystem` 在 LateUpdate 跟随角色渲染位置，方向不随角色转身，输入通过 `BrawlCameraInputHelper` 映射到相机水平朝向。相机具体数值以最新配置为准，不背历史文档初值。

## 11. 配置表快速定位

源表目录：[Luban/Config/Datas](C:/Work/UnityProjects/ET/Packages/cn.etetet.brawl/Luban/Config/Datas)。本轮为了避免额外读取和改动 Excel，字段从实际生成类核对，生成数据不一定等于尚未导出的工作簿。

| 表/生成类 | 核心字段及用途 |
| --- | --- |
| `BrawlMode` | `Id/Name/Description/TimeLimitSeconds/Enabled/TeamSize/TeamCount`；总人数=TeamSize×TeamCount，当前练习赛1×2，不再依赖IsTeamMode |
| `BrawlLevel` | `Id/ModeId/SceneAsset/StyleId/RowCount/ColumnCount/MapGrid/SpawnPoints/NPCPoints/NPCIds`；NPC位置与ID平行数组 |
| `BrawlLevelStyle` | `ModelPath1..4`；与地形枚举对应，0平地当前默认不生成模型，3保留类型不代表编辑器完整支持 |
| `BrawlHero` | `ModelPath/NormalAttackSkillId/SuperSkillId/SpecialSkillId/BaseHp/HpGrowthPerLevel/MoveSpeedTier/CollisionRadius/CollisionHeight/Level`，另有Rarity/Role/UnlockPrice等展示或成长字段 |
| `BrawlNPC` | `NPCType/Faction/BaseHp/CollisionLength/CollisionWidth/CollisionHeight/PivotX/PivotZ/UnitLayerMask/BlocksMovement/DeathDuration/RespawnDelay/ComponentTypes/ComponentValues` |
| `BrawlGlobal` | `CellSize/MaxLevel/HeroSpawnProtectionDuration/HeroSpawnDuration/HeroDeathDuration/RedRespawnPointNpcId/BlueRespawnPointNpcId`，以及客户端相机字段 |
| `BrawlSkill` | `ProjectileId/ProjectilesPerVolley/EmitterType/EmitterParam1..3/AmmoCapacity/ReloadTime/CooldownTime/SkillRangeType/SkillRangeParam1..2/SkillCastRangeType/SkillCastParam1..2` |
| `BrawlProjectile` | `Radius/Speed/Value/ValueGrowthPerLevel/MaxHitCount/Lifetime/HitEvent1..2及参数/SuperChargeOnHit/CollisionRuleGroupId/ModelPath/ImpactEffectPath/ExpireEffectPath` |
| `BrawlCollisionRule` | `GroupId/RequiredLayerMask/ExcludedLayerMask/RelationMask/BlockProjectile/ApplyHitEvents` |

`BrawlGlobal` 用 Luban `mode="one"`，不是为了一个全局数据再循环查找许多Id。源定义：[BrawlGlobal.xml](C:/Work/UnityProjects/ET/Packages/cn.etetet.brawl/Luban/Config/Defines/BrawlGlobal.xml)。

生成类可从[ClientServer 配置目录](C:/Work/UnityProjects/ET/Packages/cn.etetet.config/CodeMode/Model/ClientServer/Config/ET)快速查看；另有 Client/Server 分组。`##group=c` 的显示文本/资源路径不必导出服务器；业务几何和规则两端需要时保持共享。不要手改生成类来代替改源表、定义和导出。

`BrawlLevelEditorWindow` 操作关卡工作簿，提供网格/出生点/NPC编辑、撤销重做与保存检查；行列最大100，出生点数量要求准确等于玩法人数。Excel 是关卡数据来源，不在游戏每帧读单元格。见[编辑器入口](C:/Work/UnityProjects/ET/Packages/cn.etetet.brawl/Scripts/Editor/Share/Level/BrawlLevelEditorWindow.cs)。

## 12. 验证证据与当前不能承诺的事情

| 证据 | 能说明什么 / 不能说明什么 |
| --- | --- |
| 本次源码与配置核查 | 能定位上述实现；不能代替本机最新运行结果 |
| `Temp/BrawlCharacterAnimation-build.log`、`Temp/BrawlSkillAim-build.log` | 已有0错误构建记录；不代表后续所有修改都通过 |
| `Temp/BrawlAnimation-speed-build.log` | 最新步幅调整完整构建曾失败：MSB3027/MSB3021，dotnet进程占用ET.Core.dll；不能当成功记录 |
| 动画交接中的资源采样 | 21角色84移动槽位及步幅采样有检查记录；不是联网Play验收 |
| AI交接中的回归记录 | 记载AI/生命周期2 Passed、0 Failed；其引用的final日志本次未找到，未重新运行，不升级为当前版本实测结论 |
| 现有 `Statesync_Brawl*_Test` | 有碰撞、弹丸、弹药、状态、生命周期、AI等用例；用例存在不等于本次全通过 |

面试时明确：没有客户端预测/回滚，没有完整多人匹配与重连，没有大规模压力数据，没有完整 Bot 攻击；客户端清草、完整攻击/受击/死亡动画、特效池还存在未完成项。诚实指出这些，并解释先做权威逻辑闭环的顺序，比把 Demo 描述成已上线产品更可信。
