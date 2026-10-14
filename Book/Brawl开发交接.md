# Brawl Demo 开发交接

更新日期：2026-10-15。此文件用于新会话恢复上下文，不代表授权立即实施所有待办。

## 快速恢复：当前实现与下一阶段

- **当前生命周期以紧接下方的2026-10-15章节为准**：英雄死亡销毁后创建新实体，在绑定复活点出生；NPC销毁后延迟原地重生。下面旧章节的NPC出生无敌、Revive原实体、尸体仍阻挡和不自动复活均为历史记录，已被本轮替代。

- 最新状态拆分已编码：属性只保留数值；StateId/StateComponent管理眩晕、击飞、无敌、已死亡，出生/复活保护3秒，死亡后保留实体并禁止行动，服务器同步最终bool。详见下一节“状态组件、出生保护与死亡标记”；本节以下旧记录的“尚未处理死亡”不再代表当前行为。

- 已实现：服务器权威移动、NPC 阻挡、移动方向与人物面向独立同步、普攻/大招技能摇杆与预览接口、雪莉5枚弹丸及服务器位置同步、弹丸连续扫掠与阻挡/有效命中计数销毁、阵营。详见下文最新增量，历史阶段记录不代表当前行为。
- 最新用户已授权文档完成后直接编码；属性组件、弹丸等级成长、HitEvent=1扣血和生命状态同步已接入，构建状态见下方“属性与扣血实现”。本版不处理单位死亡/复活、护盾、柯尔特连发、CD/弹药/装填、瞄准范围mesh和动画。
- 下方属性/伤害方案保留设计缘由；其“尚未实现/建议”措辞为前一轮讨论记录，实际落地差异以紧接着的最新实现章节为准。
- 移动与面向已完成部分见“技能摇杆与独立面向”；属性枚举、修正项及伤害流程见“属性系统方案”和“弹丸数值与扣血方案”。后续开发先读这三处，勿从早期技能草案重新推导需求。

## 最新增量：出生、死亡与重生（2026-10-15）

### 已确认且已实现的规则

- 英雄（主角/Bot）初次进场与每次复活均先进入Spawning阶段2秒，不能移动/攻击、不受弹丸碰撞；结束后进入3秒Invincible。无敌期间可以行动，子弹仍可碰撞，但不扣血。
- HP归零只触发一次Died通知，进入Dead阶段3秒，停止移动/攻击。到时销毁旧Unit，立即在绑定复活点创建满血的新Unit；随后出生2秒、无敌3秒，即死亡到可操作共5秒。复活不是旧实体加血，UnitId会改变。
- NPC读取DeathDuration，死亡后保留到期再销毁。RespawnDelay从实际销毁时刻开始计时，0表示不再重生；劫匪3秒死亡加5秒等待，共8秒后在原始种植位置创建新Unit。NPC没有服务器Spawning过程，没有出生/复活无敌；未来客户端可自行播放和打断出生动画。
- 所有死亡单位立即退出弹丸碰撞和移动阻挡查询。墙/木桶配置死亡0.2秒、不重生，其模型保留至销毁以便未来接死亡表现。当前雪莉TargetMask仍为敌方，不能靠雪莉打掉墙/桶，不修改技能目标规则来测试生命周期。
- 水晶仅保留配置扩展，不实现拾取或重生；复活点NPC也不参与NPC自动重生流程。

### 组件、绑定与配置

- Unit默认挂BrawlSpawnComponent/BrawlDeathComponent，保存原始位置、出生结束时刻、死亡期限等。StateId新增Spawning=500，已有100/200/300/400保持；状态来源-1保护、-2死亡、-3出生。删除旧Revive入口，统一由BrawlUnitFactory创建新实体。
- BrawlLifecycleRoomComponent保存在房间Scene：保存模拟时刻、参与者记录、long[]复活点UnitId、NPC待重生记录、生命周期通知与名单变化标记。System/Helper只执行逻辑，不持有静态待重生集合。NPC记录仅ConfigId/Faction/原位置/ReadyAt，不引用已销毁Unit。
- 玩家/Bot的BrawlParticipant保存在房间中，保存英雄配置、玩家ID、阵营、绑定复活点ID和当前UnitId。死亡重建后更新当前UnitId，绑定不变；快照同步新的MyUnitId，客户端切换模型和输入目标。
- 关卡SpawnPoints是英雄复活点唯一来源，不再允许NPCIds里重复刷NPCType=7。按前TeamSize个红方、后TeamSize个蓝方创建复活点NPC，运行时覆盖其Faction；组内顺序对应参与者索引。当前练习赛仍仅1v1，尚未扩展多人入场。
- 复活点组件类型为2，仅服务英雄；配置沿用用户已有BrawlUnitComponentReborn.xlsx，XML注册生成BrawlRespawnPointConfig/Category，目前仅Id/Name。NPC原地重生不创建复活点NPC。
- BrawlGlobal已有HeroSpawnProtectionDuration=3、HeroSpawnDuration=2、HeroDeathDuration=3，本轮额外配置RedRespawnPointNpcId=2001、BlueRespawnPointNpcId=2002，避免代码硬编码资源ID。NPC消费DeathDuration/RespawnDelay。时间配置单位秒，内部使用房间50ms Tick的模拟毫秒，未对齐的期限在下一Tick处理。
- 配置和协议经ExcelExporter/Proto2CS正式生成，未手改生成文件、meta或csproj；全量配置导出可能包含用户此前已修改的其他表。

### 同步与客户端边界

- 入场UnitInfo增加SpawnCompleteAt/DeathDestroyAt。移动快照增加MyUnitId、HasRoster、Roster与LifeEvents；实体增删时发送当前全量名单，状态与生命继续沿原快照同步。
- LifeEvents传递SpawnStarted/SpawnCompleted/Died/Removed及服务器时刻，避免死亡时间为0时只剩最终状态而丢失通知。客户端暂保存在TrainingSession.LastLifeEvents（最近一批）；未来表现层需要在收包处消费，不能把此列表当永久事件历史。
- BrawlUnitRosterViewHelper按新名单创建/销毁模型，异步加载结束后检查取消与移除状态；相同资源复用句柄并等待完成。英雄Spawning期间隐藏人物模型，完成后显示；死亡期间保留模型直到服务器移除。
- 本轮没有倒计时UI、出生特效、死亡动画或客户端状态机，也没有NPC AI、柯尔特技能、弹药/CD的新实现。看不到倒地动作不代表死亡计时没生效。

### 验证与接手

- 新增Statesync_BrawlLifecycle_Test：英雄2秒出生/3秒保护、单次死亡通知、死亡3秒后新实体及固定复活点、NPC按3+5秒原位置重生、零延迟不重生、死墙立即解除移动阻挡。更新State/Property和旧NPC配置构造测试以适配生命周期契约。
- 测试仅编译，未执行；没有启动服务器或Unity Play。用户自行运行验证：新局英雄2秒后显示；击杀Bot后3秒移除、出生2秒后显示；劫匪死亡3秒移除、再5秒原地出现且可立即受伤。退出/重进应不残留前房间模型或待重生任务。
- 最终验证（2026-10-15）：dotnet build ET.sln成功，0错误、14个原有HybridCLR编辑器CS0649警告，耗时46.60秒，日志Temp/BrawlLifecycle-build-final.log。UnityBridge错误日志查询Logs为空；业务代码/文档git diff --check通过。现有大量未提交修改继续保留，未提交Git。

## 当前目标与协作方式

- ET 上的荒野乱斗风格面试 Demo；Unity 2022.3.62f3，VS 2026。
- 先读根 AGENTS.md、harness 规范及目标包 AGENTS.md；命令使用 pwsh。
- 用户自己操作 Unity 测试；节省 token，不主动进行耗时游戏测试或读取整个资源目录/Excel。唯一编译入口 `dotnet build ET.sln`。
- 不手改 .meta / .csproj / Luban 生成代码。已有大量未提交改动（资源、配置、先前移动代码），不可清理或覆盖。此次没有提交 Git。
- 保留已有临时验证入口，不删除 `Brawl/Verification/RunTerrain`。

## 最新增量：状态组件、出生保护与死亡标记（2026-10-14）

- 用户明确授权拆分属性与逻辑状态，不实现客户端表现/逻辑状态机。PropertyId移除IsStunned/IsAirborne，900/910永久保留不用；PropertyComponent删除GetBool及布尔修正支持，只处理数值。当前属性枚举共28项。
- 新增StateId：Stunned=100、Airborne=200、Invincible=300、Dead=400；均为最终bool，默认false，编号不要求连续。StateComponent挂载到所有练习赛Unit（英雄、Bot与NPC），保存Sources、Values及PendingChanges。
- 来源结构为“状态ID -> 来源实例ID -> 到期模拟毫秒”。AddSource同状态/来源幂等更新期限；RemoveSource可移除某状态某来源，也可移除某来源的全部状态；Expire按服务器时间清除已到期来源。任一有效来源存在则为true，只有最终bool变化才记录待同步项，来源数量变化不额外发状态变化。
- 来源ID不使用Buff配置ID。生命周期保留负来源：-1出生保护、-2死亡；未来Buff实例使用独立正ID。0到期值表示永久，等待来源主动移除。状态不拥有Buff规则，叠层、免疫、驱散等仍由未来Buff逻辑处理。
- BrawlUnitLifeHelper.OnSpawn给有正生命值的单位添加3秒Invincible，常量SpawnProtectionMilliseconds=3000来自本轮用户要求；当前建房单位均从模拟时间0开始，包括NPC。每个50ms Tick用Tick×50毫秒处理到期，使用模拟时间、不使用客户端计时。动态出生或复活时必须传当前房间模拟时间，不能继续传0。
- BrawlUnitLifeHelper.Revive提供恢复MaxHealth、清除生命周期死亡来源、重新开始3秒保护的入口，当前无自动调用者。只处理生命和保护；不负责位置、Buff清理、重生等待或NPC配置。出生保护到期只移除其自身来源，不覆盖其它无敌来源。
- BrawlProjectileHitHelper遇到Invincible或Dead不扣血，但原弹丸阻挡/有效命中计数仍执行。致命扣血立即调用CheckDeath，标记Dead并清零移动方向/实际速度；每Tick另核对HP，保障其它属性设置入口将HP改为0时最迟下一Tick标记死亡。
- Dead/Stunned/Airborne统一通过CannotAct禁止服务端移动意图执行和普攻。无敌不禁止行动。死亡后单位、模型与阻挡组件均保留，目前尸体仍参与原碰撞/阻挡查询，但不再受伤。死亡后加血不自动复活，必须走复活流程。
- 不实现死亡持续时间、尸体自动销毁、自动复活、死亡/重生配置列、Animator或客户端状态机。未来用户可能在NPC表增加死亡/重生时间，本轮不擅自定义列。
- 入场BrawlPracticeUnitInfo.States为四项全量状态（包括false）；后续G2C_BrawlMoveSnapshot.States只包含最终bool变化，按外层EntryId/RoomId/Tick过滤旧消息，沿用当前有序可靠Session传输。多次Tick追赶期间同一状态合并为最新值，这里同步状态而非技能/动画事件。
- 客户端现有BrawlPracticeUnitView增加Dictionary<StateId,bool> States，直接应用服务端全量/变化；当前客户端没有完整Unit实例，不为这一步额外创建一套实体。客户端不计算Buff关系、不计时解除无敌、不自行判断死亡、不新增表现逻辑。
- 新增Statesync_BrawlState_Test：多来源叠加、按来源清除多个状态、无敌到期边界、其它无敌来源保留、死亡保留实体并禁止行动、复活保护、全量和false变化同步。旧属性测试改为拒绝废弃900/910，并校验致命伤害标记死亡；原碰撞测试补齐StateComponent契约。用例只编译，不主动运行。
- 最终验证：协议通过Proto2CS重新导出；Unity自动生成新增脚本.meta和工程引用。最后一次dotnet build ET.sln通过，0错误、14个已有HybridCLR编辑器CS0649警告，耗时27.08秒，日志Temp/BrawlState-build-verified.log；第一次完整构建日志Temp/BrawlState-build-final.log（1分00.33秒）。业务/协议/文档diff空白检查通过。本轮未修改Excel，未启动服务器/Play，新增用例已编译但未执行。

本轮回归建议：启动新服务器并新建雪莉练习赛，查看客户端PracticeUnits中States：出生Invincible=true，3秒模拟时间后false；保护期间命中不掉血但弹丸仍消失，到期后正常扣血；HP降到0时Dead=true，模型保留，死亡单位不能移动/发射。不应期待自动复活或死亡动画。

## 最新增量：属性与扣血实现（2026-10-14）

- 用户在文档完成后明确授权继续代码实现。PropertyComponent挂载到练习赛英雄/Bot/NPC，PropertyId采用下方30个显式编号。初始化要求传入字典，遗漏输入为0/false；提供GetInt/GetFloat/GetBool及Set，校验未知ID、非法类型和非有限值，禁止直接写派生结果。
- PropertyModifier为按值保存的struct，字段Id/SourceInstanceId/Property/Value；当前只有加法，不引入没有使用场景的Operation枚举。修正项保存于组件字典；AddModifier/UpdateModifier/RemoveModifier/RemoveSource立即重算，不依赖连续编号。当前没有脏队列或属性变化事件；即时重算保证下一次读取有效，之后有批量需求再扩展。
- 重算按显式依赖计算最大生命、移速、生命恢复、装填速率和超级技能恢复；最终值下限0。当前Health/Shield/Ammo/SuperCharge独立保存且不接受持续修正项，资源变化用Set，不会因重算最大值而重置。最大生命变化暂保留绝对Health，不自动补血或截断；后续再确定玩法规则。
- IsStunned/IsAirborne支持多个来源各加1，按汇总大于0解释，移除一个来源不覆盖其它来源。英雄移动每Tick从属性读取MoveSpeed，受控制时禁止主动移动，普攻请求受控制时忽略。不新增Buff生命周期或强制位移算法。
- BrawlHero.xlsx在Q列新增Level，现有5行均为1；备份Temp/BrawlHero.before-property-level.xlsx。创建英雄时以BaseHp+(Level-1)*HpGrowthPerLevel初始化BaseMaxHealth与Health，基础移速沿用当前MoveSpeedTier规则。NPC暂固定1级，使用NPC.BaseHp初始化，不做NPC等级成长。无攻击力属性。
- IBrawlSkillEmitter.Emit增加level参数；BrawlEffectValueHelper计算Value+(Level-1)*ValueGrowthPerLevel。发射器输出BrawlProjectileLaunch.EffectValue，由BrawlSkillCastHelper复制到BrawlProjectile.EffectValue；同时保存两个HitEvent类型。弹丸Awake重置新增字段。数值是生成时快照，不随英雄后续等级变化。
- BrawlProjectileCollisionHelper只在有效且未重复命中的接触调用BrawlProjectileHitHelper。HitEvent=1按Max(0,Health-EffectValue*(1-Clamp(DamageReductionRatio,0,1)))扣血；HitEvent=0/未实现类型不执行效果。命中计数与阻挡保持独立，同一弹丸对同一单位只执行一次，两组事件仍按配置顺序执行。雪莉墙体仅阻挡，不在生效目标中所以不扣墙血。
- Health最低0。当前不触发死亡、不销毁模型/墙体、不禁止零血量单位行动，也不处理护盾、回血、弹药或超级技能充能；这些字段仅建立数据接口。
- 入场BrawlPracticeUnitInfo新增Level/Health/MaxHealth；移动快照增加独立Properties列表，同步所有单位（含静止NPC），没有给NPC制造移动组件。客户端BrawlPracticeUnitView保存这三个值，尚未新增血条/飘字，表现层不得自行扣血。
- 配置和Proto均通过正式工具导出；源表中用户已有的BrawlGlobal.MaxLevel=12及关卡修改也随全量导出生成，未修改这两份源表。当前仅校验英雄等级至少1，最高等级规则未接入。Unity自动生成新增.meta与工程引用，未手改.csproj。
- 新增Statesync_BrawlProperty_Test，扩展弹丸发射与碰撞测试：修正项移除/来源移除、两个眩晕来源、类型/只读约束、成长与减伤、重算不重置HP、100%减伤、零血量不销毁、同弹丸去重/不同弹丸分别扣血。按协作约定只编译，不主动运行游戏或自动化测试。
- 最终验证：用户停服后dotnet build ET.sln成功，0错误、14个既有HybridCLR编辑器CS0649警告，耗时1分40.52秒；客户端与服务端程序集均生成。日志Temp/BrawlProperty-build-final.log。之前Unity Console Error查询为空；本轮未启动服务器/Play，也未执行自动化用例。业务代码与文档diff空白检查通过，Luban生成文件仍保留模板自带尾随空格，未手改生成代码。首次构建的DLL占用已解决，失败日志Temp/BrawlProperty-build.log保留供诊断。

当前手动验证：重启最新服务器并新建雪莉练习赛，检查客户端PracticeUnits中的Level/Health/MaxHealth。1级每枚有效命中扣300，墙/桶阻挡但不扣血；劫匪与Bot扣血，水晶/出生点不受影响。多枚各自结算，血量最低0且模型保留。修改英雄Level并重新导出/编译后，新局生命与新生弹丸按等级成长。

## 已有流程

- MongoDB 账号登录，首次登录自动注册（本地 Demo）；大厅英雄列表、详情和试用入口已存在。
- 训练场：本地展示，关卡 10101，场景读取配置；加载地形、英雄和 NPC 模型，无本地移动。正常登录仍需要服务器。
- 练习赛：玩法 102、关卡 10201，1 人一队、2 队、时限 0；Gate 建房，服务端创建玩家、固定公牛 Bot（英雄 201）及配置 NPC。
- 进入 RPC 返回单位快照，客户端依快照加载模型；退出释放房间、场景和资源。当前只有一个真实玩家，不是多人匹配实现。
- 网格索引 = row * ColumnCount + column；世界 X/Z = column/row * BrawlGlobal.CellSize，当前格长约定 1 米。美术缩放由用户处理，不再额外缩放 Prefab。

## 上一轮移动完成

- 玩家和 Bot 的 BrawlMoveComponent.Radius 读取各自 BrawlHero.CollisionRadius，不再使用固定 0.5。
- 出生点分别使用玩家、Bot 配置半径检查，拒绝非有限或非正半径；不再以统一半径检查所有出生点。
- 摇杆常量：`Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/BrawlJoystickHelper.cs` 的 `DeadZone=0.2f`；HandleRange=60，对应中心 12 UI 局部单位。
- 死区内及边界停止，死区外归一化方向，全速移动；旋钮本身仍随手指位置显示。服务器也归一化非零输入，不使用模拟力度。
- 不重新归一化碰墙后的剩余位移，保留自然滑墙减速。
- 未修改 Excel 或重新导出；用户已完成配置导出。未新增协议、类、包依赖、异步流程或 Unity 物理碰撞。

## 移动现状与代码入口

statesync 包：

- `Scripts/Hotfix/Server/Brawl/BrawlPracticeHelper.cs`：建房、出生验证、创建移动组件。
- `Scripts/Hotfix/Server/Brawl/BrawlPracticeMovementHelper.cs`：输入、服务器固定步长、位置快照。
- `Scripts/Hotfix/Share/BrawlTerrainMovementHelper.cs`：XZ 圆形与地形格碰撞、边界、滑动。
- `Scripts/Model/Share/BrawlMoveComponent.cs`：方向、速度、半径、实际速度数据。
- `Scripts/Model/Share/BrawlMovementSettings.cs`：50ms Tick、500ms 输入超时、最多追赶 5 步、2 Tick 插值缓冲等。
- `Scripts/HotfixView/Client/YIUISystem/Lobby/`：BrawlJoystickHelper、BrawlPracticeClientComponentSystem、BrawlPracticeMovementViewHelper 等输入与表现逻辑。

服务器权威；WASD/摇杆发送意图，约 20Hz，起停及时发送。速度暂按 MoveSpeedTier 米/秒。服务端固定 50ms 步进，正常约 20Hz 快照，静止仍同步玩家/Bot；客户端插值，不预测、不外推、不控制 Animator。输入超时自动停止。Bot 有移动组件但无 AI。地形墙、水、地图边缘及带阻挡组件的 NPC 阻挡移动；平地、草和无阻挡组件的单位可通过，英雄之间可重合。

协议：C2G_BrawlMoveInput 的 EntryId 区分进入批次、Sequence 为输入序号、X/Z 是方向而非位置。G2C_BrawlMoveSnapshot 的 RoomId/EntryId 防止旧房间消息污染、Tick 是模拟步编号、LastInputSequence 为服务器已接收输入序号（不等同逐条输入执行确认），Units 中 UnitId/X/Z 是权威位置。连续日志是状态同步产生，不一定是异常。

## 碰撞配置约定

- 英雄 CollisionRadius=0.3 米，CollisionHeight=1.2 米（脚底向上完整高度）；半径与高度独立。配置半径不再额外乘 CellSize 或模型缩放。
- 高度已导出，但当前平面移动不使用，也没有为此添加碰撞组件。
- NPC 表：CollisionLength / CollisionHeight / CollisionWidth / PivotX / PivotZ，float、双端字段。
- 已实现约定：Width=0 为圆柱，Length 表示直径；Width>0 为长方体，Length/Width 为完整 X/Z 尺寸、Height 为完整 Y 高度。PivotX/Z 表示相对单位原点的碰撞中心偏移（米），不是世界坐标。当前平面碰撞不使用高度；NPC 服务器旋转为 identity，矩形轴对齐，不随模型旋转或缩放。
- 最新已核对源表及 Client/Server/ClientServer 生成配置：101 墙 1×1、高 1；201 木桶直径 1、高 1（Width=0）；301 劫匪直径 0.6、高 1。英雄半径仍以各自最新导出配置为准。
- 旧墙 Width=0.3，地形墙厚 1，英雄沿 Z 轴停止位置相差 (1-0.3)/2=0.35 米。用户已修正墙尺寸，不应在代码硬加 0.35 或修改美术资源。
- Unity 顶栏“配置生成”调用 ET/Excel/ExcelExporter，仅导出配置源码，不执行 dotnet build ET.sln 或重启独立服务器。本次发现源文件 02:05 已更新而 Bin/ET.Config.dll 仍为 01:43，需编译后重启服务器并新建练习赛；Unity 自身刷新不代表独立服务器 DLL 更新。

## 2026-10-14 NPC 阻挡实现

- 用户确认英雄与英雄之间不碰撞，可以重合；英雄与 NPC 是否可重合取决于 NPC 是否挂载阻挡组件。
- `BrawlUnitComponentBlock` 保存 ConfigId、CanBeDestroyed、Length/Width/Height/Pivot。用户明确可摧毁属性保留在阻挡组件，不再拆分。该 bool 不决定是否阻挡，攻击和摧毁仍未实现。
- `BrawlNPCBlockHelper` 按 `BrawlNPC.ComponentTypes` 与 `ComponentValues` 下标配对挂载；`BrawlUnitComponentType.Block=1`，值为配置行 ID。已校验等长、重复/未知类型、配置行存在性及有限有效尺寸。
- `BrawlUnitMovementHelper` 在分轴小步内共同约束地形和 NPC；`BrawlBlockGeometryHelper` 处理圆/矩形连续限位。主角/Bot 共用，NPC 无组件则跳过。无缓存，移除组件或销毁 NPC 后立即解除阻挡。建房后检查玩家/Bot 是否与阻挡 NPC 重叠。
- 新表 Id/Name/CanBeDestroyed 已注册并通过 ExcelExporter 导出。未手改 Excel 或生成代码。最新源表墙引用阻挡配置 1，木桶引用 2（均 true），配置 3 为 false，劫匪无组件。
- 本轮只编译，用户自行 Unity 回归；新增两组 ATestHandler 用例与 statesync/Test.md，未执行自动化测试。没有新依赖/协议/静态可变状态，未手改 .meta/.csproj。

## 本轮技能实施范围（优先于下方历史设计）

### 最新增量：弹丸阻挡、碰撞销毁与阵营

- 用户授权碰撞销毁，明确 **不计算伤害**。本节覆盖此前“弹丸穿墙/穿单位”的历史记录；雪莉仍5枚，无CD/装填，柯尔特不实现。
- BrawlNPC 表 Faction 已由用户修改并重新导出：墙101/桶201/劫匪301/水晶1001为0，出生点2001/2002为3。NPCType分别2/2/1/4/7/7，尺寸保持配置。源表未手工修改。
- 阵营枚举 NeutralHostile=0、Red=1、Blue=2、NeutralFriendly=3；两个0不敌对，3与各阵营不敌对，其余不同阵营敌对。暂不做第二阵营参数。服务器练习英雄1、Bot2、NPC读取表；BrawlPracticeUnitComponent.Faction 取代原 TeamId，入场快照同步。弹丸保存生成时所有者阵营并同步到客户端，排除发射者自身。
- BlockMask/TargetMask 使用独立枚举。雪莉7表示两类墙和敌军阻挡，TargetMask1仅敌方作战单位有效。NPCType1参与阵营筛选；NPCType2结合实际阻挡组件分类两类墙，墙/木桶不会因0阵营算敌军。水晶、出生点等非作战NPC跳过。
- BrawlProjectileGeometryHelper 实现移动圆对圆/轴对齐矩形连续扫掠，矩形为精确圆角扩张而非简单扩大AABB；包括弹丸半径和NPC Pivot。BrawlProjectileCollisionHelper 收集整段路径的接触，按先近后远、同时墙优先处理；地形只检测 Wall，水/草/可穿弹障碍不挡。
- 单位保存本Tick移动前的位置，对作战单位使用相对运动扫掠，检测横穿弹道；出生重叠立即检查。停止时弹丸中心落在首次接触点，Finished后不再判定，保留该帧快照并在下一Tick移除，客户端沿原插值时间线销毁Sphere。
- 有效命中记录每枚弹丸自身 HitCount/HitUnits，同一单位每枚只计一次；达到正数MaxHitCount终止，0不因次数终止。BlockMask阻挡独立终止，即使MaxHitCount0也挡。没有调用HitEvent、读取Value/成长或修改生命值，墙/桶/单位均不被摧毁。
- 新增 Statesync_BrawlProjectileCollision_Test，修正NPC旧测试构造函数以适配新增Faction。配置及Proto已重新导出，源码/配置构建通过；运行时测试仍由用户执行，未启动服务器或Play、未运行自动化用例。
- 本轮最终 dotnet build ET.sln 成功：0错误、4个MSB3026文件占用重试警告，随后DLL复制成功，耗时42.24秒。服务器与客户端程序集均已生成；日志 Temp/BrawlCollision-build-final.log。UnityBridge最后的Console查询超时，不作为Unity运行测试通过证据。git diff --check 无空白错误。

### 已完成：技能摇杆与独立面向（当前约定）

- 用户确认雪莉普攻第一阶段基本可用。本轮在此基础上添加移动协议独立面向、普攻/大招共用技能摇杆；不做范围 mesh/特效/动画、不实现柯尔特。原“普攻只能沿移动方向”和“大招禁用”被本节取代。
- 方向分三份：移动输入 X/Z；人物面向 FacingX/Z；技能有效释放时 C2G_BrawlNormalAttack.X/Z。移动输入/移动快照/入场快照新增独立面向，服务器保存到 BrawlMoveComponent.FacingDirection。快照另含 MoveX/Z（移动意图，不是实际碰墙速度）。不根据移动或技能拖动覆盖面向，默认 +Z。
- BrawlPracticeMovementViewHelper.SetFacingDirection 为以后技能控制面向预留，当前不调用、不做模型旋转。以后移动向北、面向东可同时传输；连发期间的锁定/解除规则留后续实现。
- 普攻/大招复用 BrawlSkillJoystickHelper 和 BrawlSkillAimHelper；独立 pointer/手势状态。以按下点作为拖动中心，行程60 UI单位，死区比例0.2，阈值12 UI单位（含边界）。未出过死区时松手等同点按：实时读取当前移动方向；静止取最近非零移动方向，初始 +Z，不取面向。
- 出死区则更新预览；在外面松手按瞄准方向释放。出过死区又回到里面立即隐藏预览，最终在里面松手取消；再次拖出可恢复释放。松手会重新采样坐标，按取消/失焦/退出清理，防止跨手势遗留或重复发送。
- IBrawlSkillAimPreview 提供 ShowOrUpdate/Hide，BrawlSkillAimPreviewData 带槽位、技能ID、完整配置、世界原点和XZ方向。用 BrawlSkillJoystickHelper.SetPreview 注册表现实现；默认未注册，不生成范围 mesh。英雄移动时接口原点跟随，准备过程不改面向、不发服务器消息。
- BrawlSkillViewHelper 建按钮，BrawlSkillJoystickHelper 处理手势，BrawlSkillInputHelper 发有效普攻请求，三者单向调用；修复用户反馈 CCD001 类级循环依赖。大招可拖动但仍未实现技能，不发送大招请求。
- 用户拟将未来柯尔特 CD 设为0.8秒（0.16×(6−1)），本轮不改 Excel、不实现 CD 或柯尔特连发。
- 新增 Statesync_BrawlSkillAim_Test；按已有协作方式只编译，用例和 Unity 游戏回归未运行。CCD001 已修复，Unity Console Error 查询为空。用户停服解除 DLL 占用后，最终 dotnet build ET.sln 成功：0 错误、14 个 HybridCLR 编辑器 CS0649 警告，耗时 25.82 秒；客户端与服务器均已生成，日志 Temp/BrawlAim-build-final.log。git diff --check 无空白错误。

### 第一阶段实现记录（历史：碰撞和摇杆行为已由上文覆盖）

- 用户最终缩小范围：仅雪莉 10101；柯尔特等做完雪莉再做。雪莉为每轮 **5** 枚，用户已确认“6个Sphere”为笔误。无 CD、无弹药消耗、无装填；按普攻按钮即可释放。
- 已确认 XZ 平面；重复 Speed 已删除，Emitter 拼写已更正。最新导出雪莉速度 8 米/秒、半径 0.05 米、射程 7.67 米、总夹角 30 度、轮数 1。
- 不做弹丸碰撞、命中事件、伤害或墙体摧毁，弹丸穿墙/穿单位。已有英雄移动阻挡保留。Lifetime 尚不实现，弹丸中心沿各自方向走满射程后移除。
- 服务器练习赛 UI 显示普攻和大招按钮；大招禁用并注明未实现。本地训练场仍是展示流程。当前简易方向：初始 +Z，之后沿最近非零移动方向；独立瞄准、动画及上下身分离未实现。
- IBrawlSkillEmitter + BrawlShotgunEmitter 为无状态多态发射算法，输出局部偏移/方向/速度/半径/射程；五枚均匀分布 -15/-7.5/0/7.5/15 度。其它发射器类型只预留枚举，未实现类型不释放。
- BrawlSkillCastHelper 按服务器英雄的 NormalAttackSkillId 取配置，不接受客户端指定技能/弹丸数/伤害；检查 EntryId、单调请求序号、有限非零方向。BrawlProjectile 是房间 BrawlProjectileRoomComponent 的子 Entity，不混入英雄/NPC UnitComponent。
- BrawlProjectileHelper 使用现有 50ms 时钟更新，每步位移裁剪到剩余射程。首次立即发送出生快照，随后全量位置快照，包含 EntryId/RoomId/Tick/独立Sequence；缺席代表删除，终点保留一 Tick 快照，同 Tick 多次点击不会互相覆盖。
- 客户端 BrawlProjectileViewHelper 为每个服务器弹丸 ID 创建 Sphere，直径 2×Radius，关闭并移除 Collider。显示高度取英雄 CollisionHeight/2，不参与 XZ 逻辑。沿服务器快照插值，出生/删除按同一显示时间线，退出清理，旧房间/乱序快照忽略。
- BrawlSkill.xml 已注册技能与弹丸分页并成功导出 Client/Server/ClientServer。源表尾 N22 及弹丸分页 J21:Q21 中纯空格造成空 Id 导出失败，已清除；未改实际技能数值。备份在 Temp/BrawlSkill.before-tail-cleanup.xlsx。
- 协议已通过 Proto2CS 导出；新增 Statesync_BrawlProjectile_Test 覆盖散射与射程裁剪，只编译不执行游戏测试。构建结果见文末验证状态。

## 属性系统方案（用户已认可，尚未实现）

### 数据归属与初始化

- 给需要属性的 Unit 挂载 PropertyComponent，保存初始/基础值、修正项、汇总值与最终值，以及 Health 等运行时状态。Entity/Component 只存数据，计算、修改与通知由 System/Helper 承担。
- 初始化必须传入属性参数；未给定的输入属性默认0/false，填充完后统一计算派生属性。等级等有业务约束的必填项须校验，不应悄悄用默认0代替合法等级。初始化 MaxHealth 不意味着自动填满 Health，出生时要显式给出当前生命值。
- 枚举表示属性键，不表示数组下标。每个成员显式指定稳定编号；允许不连续、任意调整声明位置，新增编号不能撞号，删除成员后保留该编号不用，避免旧配置/存档误解释。禁止通过枚举顺序推导依赖关系。
- int/float/bool 是属性的业务类型，访问接口需维护类型约束；不要因为修正项用 float 就允许等级、小数弹药和布尔状态随意混用。
- 不包含攻击力属性：攻击效果的基础数值与等级成长配置在弹丸表中，见下一节。

### 最终属性枚举列表

以下作为后续 PropertyId 的设计清单，尚未写入 C#。BonusRatio 表示当前各来源汇总后的加成比例，0.2表示加20%，-0.5表示减50%。

| 编号 | 枚举名 | 类型 | 含义 |
| --- | --- | --- | --- |
| 100 | Level | int | 英雄等级 |
| 200 | BaseMaxHealth | float | 基础最大生命值 |
| 210 | MaxHealthBonus | float | 最大生命值固定加成汇总 |
| 220 | MaxHealthBonusRatio | float | 最大生命值百分比加成汇总 |
| 230 | MaxHealth | float | 计算后的最大生命值 |
| 240 | Health | float | 当前生命值，独立运行时状态 |
| 300 | BaseHealthRegenRate | float | 基础生命恢复，生命/秒 |
| 310 | HealthRegenRateBonus | float | 生命恢复固定加成汇总 |
| 320 | HealthRegenRateBonusRatio | float | 生命恢复百分比加成汇总 |
| 330 | HealthRegenRate | float | 当前生命恢复，生命/秒 |
| 400 | Shield | float | 当前护盾值 |
| 410 | DamageReductionRatio | float | 减伤比例，0.2表示减伤20% |
| 500 | BaseMoveSpeed | float | 基础移动速度，米/秒 |
| 510 | MoveSpeedBonus | float | 移动速度固定加成汇总 |
| 520 | MoveSpeedBonusRatio | float | 移动速度百分比加成汇总 |
| 530 | MoveSpeed | float | 最终移动速度能力，不是当前实际位移速度 |
| 600 | MaxAmmo | int | 最大弹药格数 |
| 610 | Ammo | float | 当前弹药，整数部分为可用格，小数部分为下一格进度 |
| 700 | BaseReloadRate | float | 基础装填速率，格/秒 |
| 710 | ReloadRateBonus | float | 装填速率固定加成汇总 |
| 720 | ReloadRateBonusRatio | float | 装填速率百分比加成汇总 |
| 730 | ReloadRate | float | 当前装填速率，格/秒 |
| 800 | MaxSuperCharge | float | 最大超级技能进度值 |
| 810 | SuperCharge | float | 当前超级技能进度，达到最大值即可释放 |
| 820 | BaseSuperChargeRegenRate | float | 基础超级技能自动恢复，进度/秒 |
| 830 | SuperChargeRegenRateBonus | float | 超级技能恢复固定加成汇总 |
| 840 | SuperChargeRegenRateBonusRatio | float | 超级技能恢复百分比加成汇总 |
| 850 | SuperChargeRegenRate | float | 当前超级技能自动恢复，进度/秒 |
| 900 | 已废弃 | — | 旧IsStunned迁到StateId.Stunned，不复用此属性编号 |
| 910 | 已废弃 | — | 旧IsAirborne迁到StateId.Airborne，不复用此属性编号 |

### 修正项集合与计算规则

- PropertyComponent 内维护按唯一修正项ID索引的字典。修正项是普通数据记录，可采用 PropertyModifier 类（落地时遵循项目普通类规则），不是每项一个 Entity。建议字段：Id、SourceInstanceId、Property、Operation、Value。
- SourceInstanceId 标识某一次 Buff/装备/被动来源实例；同类两个 Buff 不能共用一个来源ID。移除单项按 Id，移除整个来源按 SourceInstanceId。字典删除不需要连续编号，也无需移动其它元素。
- 未来 BuffComponent 管理 Buff 的持续时间、层数、到期和来源；通过属性接口添加/更新/移除修正项。属性系统负责数值汇总，装备和被动也走同一入口，不将全部属性算法绑在 BuffComponent 内。
- MoveSpeedBonusRatio 是一个 float 汇总值，不是数组；每个 Buff 的贡献保存在修正项字典。对该比例属性添加0.2表示增加20个百分点，不是把已有比例乘1.2。显式“固定加成/比例加成”属性与通用乘法操作不能重复结算同一效果。
- 通用派生公式：最终值 = (基础值 + 固定加成汇总) × (1 + 百分比加成汇总)。适用于 MaxHealth、MoveSpeed、HealthRegenRate、ReloadRate、SuperChargeRegenRate。有效范围按具体属性约束；移动速度不应因总减速超过100%而变成负数。
- 例：基础移动速度300、固定加成50、比例贡献-0.50/+0.20/+0.15/-0.40，汇总比例-0.55，结果为350×0.45=157.5。一个来源退出后，从剩余来源重新汇总，不恢复旧最终值，也不靠除回旧倍率。
- 基础值变化、修正项添加/移除/更新或层数变化时，标记相关属性及派生依赖为脏，在消费属性前完成重算；可合并同一批变更，只通知真正变化的结果。移动逻辑读取重算后的 MoveSpeed，停止移动不把它改成0。
- Health、Shield、Ammo、SuperCharge 是当前状态，不能因重算最大值而重置。最大生命变化后保留绝对血量、保留比例或补差值的规则尚待用户确定。
- ReloadTime 是秒/格；ReloadRate 是格/秒，正常正时间取倒数，0不能直接相除。基础恢复为0时，单纯百分比加成仍为0，需要固定恢复贡献才能自动恢复。
- 初期单位上的 Ammo/ReloadRate 对应普通攻击；将来多个技能独立装填时，分离到各技能运行时状态，不能共用一套当前弹药覆盖所有技能。
- IsStunned/IsAirborne 是汇总状态。未来多个控制效果共存时按来源或计数维护，某一个控制结束不能直接清除其它来源仍生效的状态。

### 出生、死亡、复活（规划，未实施）

- 有生命流程的英雄/NPC 可挂出生与死亡组件；复活点NPC上的复活组件负责复活队列、条件、位置和时机。水晶/出生点等非作战对象不应仅因也是 Unit 就强制具备全部战斗生命周期组件。
- 死亡与实体 Dispose 不等同；复活重用原实体还是重建、延迟、恢复哪些属性、客户端通知方式后续确认。本次不实现死亡、复活或护盾结算。

## 弹丸数值与扣血方案（本次文档讨论，未实现）

### 用户确定的方向与当前代码差距

- 英雄等级来自英雄配置，在生成英雄 Unit 时初始化 PropertyComponent.Level；发射时把 Level 作为数值参数传给发射器，由发射器计算每枚弹丸的实际效果值。不新增单位攻击力属性，不让客户端传入等级或伤害作为权威结果。
- 当前 BrawlProjectile 是服务器房间 BrawlProjectileRoomComponent 的子 Entity，代码在 statesync 的 `Scripts/Model/Server/Brawl/BrawlProjectile.cs`。它本身保存运动、阵营和命中数据，并非 Unit 加 ProjectileComponent，因此直接加一个字段即可，无需仅为该值再拆组件。
- 当前 `IBrawlSkillEmitter.Emit` 只有技能配置和弹丸配置参数；`BrawlProjectileLaunch` 只输出偏移、方向、速度、半径和射程，均尚未接入 Level/效果值。现有碰撞也尚未执行 HitEvent。
- 实施前核对英雄配置是否已添加并导出 Level；当前生成版英雄配置没有该字段，不应直接假定已可读取。文档更新不修改 Excel，不手工修改生成代码。

### 建议的数值传递与存储

1. 创建英雄时校验配置等级不小于1，并初始化 Level。NPC 属性初始化按对应NPC配置和其等级规则处理，不直接套英雄表。
2. 发射时读取服务器 Level，作为发射器输入；建议使用简单线性公式 `EffectValue = Value + (Level - 1) * ValueGrowthPerLevel`。1级就是 Value。若以后换成长公式，统一调整公共数值计算 Helper，避免各发射器复制不同算法。
3. 建议给 BrawlProjectileLaunch 与 BrawlProjectile 增加 `float EffectValue`：前者携带发射器输出，BrawlSkillCastHelper 创建弹丸时复制到后者。Awake/回收初始化也必须重置新增字段。这里仅建议，尚未改接口或字段。
4. EffectValue 保存生成时的效果基础值快照；HitEvent=1时它就是尚未计算目标减伤的攻击力。保留通用名称，后续治疗/护盾事件可自行解释。不需要每帧反查发射者，发射者离场也不会让已生成弹丸丢失数值。
5. 建议已发射弹丸不随英雄后续升级变化；未来连发技能是整次施法锁定等级，还是每发重新取等级，实施连发时再确定。本版雪莉同时发射，不存在该差异。
6. 命中有效目标且事件为伤害时，由服务器命中事件/属性逻辑读取目标当前减伤并扣血；发射器不修改目标生命。阻挡与生效筛选继续独立，不能对只阻挡、不在 TargetMask 中的墙自动扣血。后续再设计生命同步，展示 Sphere 本身不需要同步伤害字段。

### 扣血公式修正与例子

暂不涉及护盾时，建议初版限定减伤比例在0到1之间：

```text
攻击力 = Value + (Level - 1) × ValueGrowthPerLevel
减伤比例 = Clamp(目标.DamageReductionRatio, 0, 1)
本次实际伤害 = Max(0, 弹丸.EffectValue) × (1 - 减伤比例)
目标.Health = Max(0, 目标.Health - 本次实际伤害)
```

雪莉单枚 Value=300、成长30：1级300、2级330、5级420。2级弹丸击中当前生命1000、减伤20%的目标，本次伤害330×0.8=264，剩余736。五枚分别命中分别结算，不将一次技能预先乘5后扣一次。

用户提出的 `(当前生命 - 攻击力) × (1 - 减伤比例)` 不正确：它会把剩余生命也乘上减伤系数，甚至在100%减伤时把生命变成0。应对攻击力应用减伤，再从当前生命扣除。护盾先后顺序、免疫、增伤、死亡触发、伤害取整等尚未约定，不能在这一步擅自补齐。

## 技能后续设计（历史讨论，未全部实施）

以下保留前期讨论；若与上方当前状态冲突，以上方为准。碰撞已实现；冷却、数值、柯尔特连发仍未实现。此处旧的待确认问题中，Speed重复、Emitter拼写、XZ平面和Faction语义已解决，不要再次询问。

本节依据前期会话粘贴的表格整理，保留供后续阶段参考，不代表本轮实际行为。

### 范围与分工

- 初版仅做雪莉普攻 10101、柯尔特普攻 40101 的服务器逻辑。其他普攻、大招、回旋镖、近战、计时 Lifetime 行为不实现；表结构可为后续保留字段。
- 不做表现、Animator 或上下身分层。设计上将瞄准/攻击方向与移动方向分离，以便后续上半身攻击、下半身按面向和移动方向播放动画；当前不增加动画实现。
- 发射器是可复用的独立 C# 算法模块，以接口或抽象类提供多态实现，不需要定义成 Entity/Component。接收技能范围、数量、发射参数和弹丸参数，输出每发的计划时间、局部初始位置、方向、初始速度、加速度等描述。
- 范围字段是发射器输入，由具体发射器解释；不是统一的直接伤害区域，不擅自拆成三套范围表。实际命中来自弹丸。
- 调度和施法状态由运行时 owner 保存，不存入共享发射器可变字段。到每发计划时刻再读取英雄当前位置，将局部发射位置转换为世界位置；不得在释放瞬间一次性锁死所有弹丸的世界出生点。
- 弹丸是独立 Entity；使用 Unit 加弹丸组件是可选实现方案，尚未落地。普通弹丸离开发射器后独立移动，不随英雄平移。禁止混用英雄的移动或 NPC 阻挡职责。

### 技能表约定与两种发射器

- ProjectilesPerVolley 表示每轮弹丸数；EmitterParam1 表示轮数，总数为两者相乘。用户选择此语义，不再沿用“一次释放总数”。
- 最新粘贴仍拼作 EmmiterType/EmmiterParam1/2/3，建议统一修正为 EmitterType/EmitterParam1/2/3；此处使用建议名，不表示源表已经改好。
- EmitterParam2 为轮间隔，EmitterParam3 为轮内弹丸间隔；当前两种技能均只有一轮。多轮的轮间隔从上轮开始还是结束计时尚未确定，后续实现多轮前确认，不影响初版。
- SkillRangeType：1 矩形，参数1/2为完整长度/宽度；2 扇形，参数1为射程、参数2为整个扇形夹角（度）；3 圆形，参数1为半径。参数2由具体形状解释，不按参数1是否为零判断是否有效。
- 初版两技能 SkillCastRangeType=0，沿用 SkillRangeType 及对应参数；独立施放范围、抛物线暂不实现。SkillType 尚未定义实际用途，不据此增加行为分支。
- 雪莉 10101：ProjectileId=10101，ProjectilesPerVolley=5，EmitterType=1，轮数1，轮间隔0，弹丸间隔0，扇形射程7.67米、总夹角30度，CooldownTime=0.5秒。五枚同时生成；均匀覆盖 -15/-7.5/0/7.5/15 度为当前建议的具体分布，用户确认了总夹角和霰弹发射方式，未单独确认采样是否含两端。
- 柯尔特 40101：ProjectileId=40101，ProjectilesPerVolley=6，EmitterType=2，轮数1，轮间隔0，弹丸间隔0.16秒，矩形长9米、宽0.7米，CooldownTime=0.5秒。
- 柯尔特发射器局部 +Z 为前，+X 为右。顺序左1、右1、左2、右2、左3、右3，左右各三枚，所有弹丸平行向前。弹丸外缘贴矩形侧边，半径0.04米时中心偏移为 X=±(0.7/2-0.04)=±0.31米。局部前向起点按发射器原点，垂直出生高度仍待确认。
- 两种技能释放时锁定瞄准方向，连发期间出生位置跟随英雄当时的位置。左右偏移沿锁定瞄准方向对应的右向量计算，不随移动方向旋转。
- 柯尔特第一发于释放时发出，计划时刻为0/0.16/0.32/0.48/0.64/0.80秒。服务器50ms固定步长实现时需保留累计计划时间，不能把0.16直接逐发取整成0.20；需明确并验证步内生成及剩余时间推进，避免新生弹丸多走完整一帧。

### 弹药、冷却与数值

- AmmoCapacity 命名已认可。最终设计初始满弹，一次释放消耗一格（柯尔特六连发合计一格），逐格装填，整次发射结束后开始装填；再次开火如何影响装填进度留待装填阶段讨论。
- 初版无限弹药，不消耗、不装填，不让 AmmoCapacity/ReloadTime 限制释放。仍保留冷却与发射中的互斥：CooldownTime 从释放开始计时，一次连发未结束不能开始下一次。
- 两技能容量均3，ReloadTime 分别1.5/1.6秒，作为未来配置保留。本版冷却均0.5秒。柯尔特六连发持续0.8秒，实际下一次释放需同时满足冷却结束和上一轮发射完成。
- Value/ValueGrowthPerLevel 保留通用名称，后续可供治疗、护盾等事件解释。最终成长依据英雄等级，公式待后续确定；初版固定一级直接使用 Value，不引入防御力。

### 弹丸与命中约定

- 用户按球体定义 Radius；当前英雄/NPC移动是XZ平面碰撞。初版弹丸是否也只做XZ扫掠，或使用三维球体与目标高度，需要实施前确认；不能把未确认的二维方案写成用户决定。
- 雪莉弹丸10101：Radius=0.05，BlockMask=7，TargetMask=1，MaxHitCount=1，Lifetime=0，Value=300，ValueGrowthPerLevel=30，HitEvent1=1，其参数均0，HitEvent2=0。
- 柯尔特弹丸40101：Radius=0.04，BlockMask=7，TargetMask=1，MaxHitCount=1，Lifetime=0，Value=360，ValueGrowthPerLevel=36，HitEvent1=1，其参数均0，HitEvent2=0。
- BlockMask 与 TargetMask 位值不同，必须定义独立枚举。BlockMask：0无、1不可摧毁墙、2可摧毁墙、4敌方、8友方、16可摧毁草。TargetMask：0无、1敌方、2友方、4可摧毁墙、8可摧毁草。掩码0表示空集合。
- 阻挡独立于有效命中次数。MaxHitCount=0只取消按有效命中次数销毁，不取消阻挡。想穿过某类对象需从 BlockMask 排除它，仍可在 TargetMask 保留效果。对草、其它技能的特殊行为暂不实现。
- 两种普攻碰地形墙或可摧毁墙：被挡并销毁，不对墙生效；碰敌方：先执行命中事件，再计一次有效命中并销毁；友方穿过。命中多个事件不等于多次命中计数。初版不会通过普通攻击摧毁墙。
- HitEvent=0为无事件，1为对命中单位造成伤害。两组事件可扩展，初版只需伤害事件，不提前实现治疗/护盾。
- 每枚弹丸独立计数；雪莉不同弹丸可以各自命中同一个敌人，不按整次释放去重。后续穿透弹丸对同一目标的重复命中规则留待相关功能阶段。
- Lifetime=0不使用计时销毁，到终点自动销毁；初版不实现非零Lifetime。每枚弹丸以实际生成位置确定自己的射程与终点，不能因英雄移动而平移已生成弹丸的终点。
- 使用完整位移线段的连续扫掠，计入弹丸半径，按最近碰撞处理；不能只测帧末位置。目标与墙重叠/同时接触时需规定阻挡优先，避免隔墙命中。既有50ms Tick 下高速弹丸可能单步跨过整个目标。
- NPC 阵营字段由用户补充，确切字段名、枚举和与房间队伍的关系尚待提供；不能自行假定所有NPC或劫匪均为敌方。需要明确阻挡NPC的墙体类别识别，并避免按NPC名字硬编码。

### 实施前待确认

1. 最新粘贴的弹丸表存在两个同名 Speed，且值不同：雪莉为3与8，柯尔特为4与10。不能生成同名字段，也不能猜其中一个是最高速度、加速度或废弃列。用户要求初版匀速（加速度0）；若前列为初速度，建议命名 InitialSpeed 并确认旧Speed列用途/删除及实际速度值。
2. 统一 Emitter 拼写，清除 BlockMask / ProjectilesPerVolley 表头可能存在的尾部空格。当前只记录建议，不改源表。
3. 确定二维/三维碰撞与弹丸出生高度、NPC 阵营语义。技能输入入口（如何触发/瞄准）与生命值/伤害结果的最小验证方式，实施时需明确；当前仍无完整技能或生命组件实现。
4. 配置校验仅针对可用/被引用的初版技能及弹丸；未完成占位行不允许释放，不要求本轮实现所有草稿行。射程按弹丸中心还是外缘停止需统一记录，不能擅自将半径从所有射程扣除。

## 明确留待后续，不要擅自实现

1. 攻击触发 NPC 伤害/摧毁，以及动态销毁通知；当前移除组件/销毁实体可解除阻挡，但没有完整摧毁玩法。
2. 旋转盒、垂直高度分层和动态障碍挤压处理，当前仅静态轴对齐 NPC。
3. Bot 移动 AI、寻路；先调查 ET 现有能力，沿用服务器权威移动入口。
4. 客户端预测、校正和真正多人房间广播。
5. 雪莉弹丸的阻挡/碰撞销毁已完成；属性方案已记录但未编码，伤害/生命、柯尔特连发、冷却/弹药/装填待后续。动画、上下身分层、其它技能和木桩复活仍留待后续。
6. 动态单位出生/销毁通知、对局结算、时限、防消极对局机制及匹配。
7. 将本地训练场从展示升级为可玩的流程，需用户确定方案。

## 验证状态

用户已反馈移动/NPC阻挡“测试没问题”。本轮技能已完成代码及配置/协议导出。首次构建的类级循环依赖已通过独立 BrawlSkillCastHelper 修正；用户打开 Unity 后自动刷新新增脚本与工程引用，未手改 .meta/.csproj。

最新碰撞版本 `dotnet build ET.sln` 成功：0错误、4个MSB3026文件占用重试警告，随后复制成功，耗时42.24秒；日志 `Temp/BrawlCollision-build-final.log`。之前技能第一阶段日志为 `Temp/BrawlSkill-build-final.log`（39.86秒），面向/摇杆阶段为 `Temp/BrawlAim-build-final.log`（25.82秒）。这些是编译记录，不代表运行时用例通过；未主动启动服务器/Unity Play，新增回归用例仅编译、尚未运行。本次仅更新交接文档，不重新编译。

当前测试入口：启动最新编译的服务器，Unity 中选择雪莉进入服务器练习赛。点击普攻生成5枚Sphere，取当前移动方向、静止取最近非零移动方向（初始+Z）；死区外松手按瞄准方向释放，出过死区后回到里面松手取消。大招按钮可拖动但不发技能。无CD/弹药/装填限制；弹丸被地形墙、带阻挡组件的墙/桶和敌方作战单位阻挡销毁，友方穿过，水晶/出生点不挡；未碰撞则走满7.67米移除，不扣血。检查高速扫掠、移动目标横穿、贴墙出生及退出重进无残留。原英雄移动阻挡保持正常。

NPC 阻挡本轮 `dotnet build ET.sln` 已通过：0 错误、0 警告，耗时 23.75 秒。首次编译发现测试名前缀需为 Statesync，修正后重编译通过。未启动服务器、Unity Play 或自动化用例，运行时回归由用户执行。
用户回归关注：1 米通道通过；墙/水/边缘停止和斜向滑动；摇杆死区内静止、死区外不同距离等速；松手/失焦停止；切英雄使用对应半径；可穿过 Bot/劫匪，但不能穿过带阻挡组件的墙/木桶。NPC 阻挡验证清单见 statesync/Test.md。

## 本地启动提示

MongoDB 服务需先启动；在项目根目录、管理员 PowerShell 中运行：

```powershell
dotnet Bin/ET.App.dll --SceneName=StateSync --Process=1 --StartConfig=Localhost --Console=1
```

Unity CodeMode 使用 Client。编译若提示 DLL 占用，请用户在服务器窗口 Ctrl+C 停止，不擅自杀进程；MongoDB 不用停止。
