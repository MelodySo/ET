# Brawl Demo 开发交接

更新日期：2026-10-14。此文件用于新会话恢复上下文，不代表授权立即实施所有待办。

## 当前目标与协作方式

- ET 上的荒野乱斗风格面试 Demo；Unity 2022.3.62f3，VS 2026。
- 先读根 AGENTS.md、harness 规范及目标包 AGENTS.md；命令使用 pwsh。
- 用户自己操作 Unity 测试；节省 token，不主动进行耗时游戏测试或读取整个资源目录/Excel。唯一编译入口 `dotnet build ET.sln`。
- 不手改 .meta / .csproj / Luban 生成代码。已有大量未提交改动（资源、配置、先前移动代码），不可清理或覆盖。此次没有提交 Git。
- 保留已有临时验证入口，不删除 `Brawl/Verification/RunTerrain`。

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

### 后续增量：技能摇杆与独立面向（最新约定）

- 用户确认雪莉普攻第一阶段基本可用。本轮在此基础上添加移动协议独立面向、普攻/大招共用技能摇杆；不做范围 mesh/特效/动画、不实现柯尔特。原“普攻只能沿移动方向”和“大招禁用”被本节取代。
- 方向分三份：移动输入 X/Z；人物面向 FacingX/Z；技能有效释放时 C2G_BrawlNormalAttack.X/Z。移动输入/移动快照/入场快照新增独立面向，服务器保存到 BrawlMoveComponent.FacingDirection。快照另含 MoveX/Z（移动意图，不是实际碰墙速度）。不根据移动或技能拖动覆盖面向，默认 +Z。
- BrawlPracticeMovementViewHelper.SetFacingDirection 为以后技能控制面向预留，当前不调用、不做模型旋转。以后移动向北、面向东可同时传输；连发期间的锁定/解除规则留后续实现。
- 普攻/大招复用 BrawlSkillJoystickHelper 和 BrawlSkillAimHelper；独立 pointer/手势状态。以按下点作为拖动中心，行程60 UI单位，死区比例0.2，阈值12 UI单位（含边界）。未出过死区时松手等同点按：实时读取当前移动方向；静止取最近非零移动方向，初始 +Z，不取面向。
- 出死区则更新预览；在外面松手按瞄准方向释放。出过死区又回到里面立即隐藏预览，最终在里面松手取消；再次拖出可恢复释放。松手会重新采样坐标，按取消/失焦/退出清理，防止跨手势遗留或重复发送。
- IBrawlSkillAimPreview 提供 ShowOrUpdate/Hide，BrawlSkillAimPreviewData 带槽位、技能ID、完整配置、世界原点和XZ方向。用 BrawlSkillJoystickHelper.SetPreview 注册表现实现；默认未注册，不生成范围 mesh。英雄移动时接口原点跟随，准备过程不改面向、不发服务器消息。
- BrawlSkillViewHelper 建按钮，BrawlSkillJoystickHelper 处理手势，BrawlSkillInputHelper 发有效普攻请求，三者单向调用；修复用户反馈 CCD001 类级循环依赖。大招可拖动但仍未实现技能，不发送大招请求。
- 用户拟将未来柯尔特 CD 设为0.8秒（0.16×(6−1)），本轮不改 Excel、不实现 CD 或柯尔特连发。
- 新增 Statesync_BrawlSkillAim_Test；按已有协作方式只编译，用例和 Unity 游戏回归未运行。CCD001 已修复，Unity Console Error 查询为空。用户停服解除 DLL 占用后，最终 dotnet build ET.sln 成功：0 错误、14 个 HybridCLR 编辑器 CS0649 警告，耗时 25.82 秒；客户端与服务器均已生成，日志 Temp/BrawlAim-build-final.log。git diff --check 无空白错误。

### 第一阶段实现记录

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

## 技能后续设计（历史讨论，未全部实施）

以下保留前期讨论；若与上方本轮范围冲突，以上方为准。碰撞、冷却、数值、柯尔特连发均未在本轮实现。

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
5. 雪莉无碰撞弹丸已写入本轮代码；柯尔特连发、碰撞/伤害/生命、冷却/弹药/装填待后续。动画、上下身分层、其它技能和木桩复活仍留待后续。
6. 动态单位出生/销毁通知、对局结算、时限、防消极对局机制及匹配。
7. 将本地训练场从展示升级为可玩的流程，需用户确定方案。

## 验证状态

用户已反馈移动/NPC阻挡“测试没问题”。本轮技能已完成代码及配置/协议导出。首次构建的类级循环依赖已通过独立 BrawlSkillCastHelper 修正；用户打开 Unity 后自动刷新新增脚本与工程引用，未手改 .meta/.csproj。

技能最终 `dotnet build ET.sln` 成功：0 错误、14 个 HybridCLR 编辑器 CS0649 警告，耗时 39.86 秒；日志 `Temp/BrawlSkill-build-final.log`。客户端 Model/Hotfix/ModelView/HotfixView 与服务器 Model/Hotfix/Config 均已编译。`git diff --check` 无空白错误。未启动服务器/Unity Play，新增回归用例仅编译、尚未运行。

测试入口：启动本轮编译的服务器，Unity 中选择雪莉进入服务器练习赛。每次点击普攻应生成 5 枚 Sphere，方向取最近移动方向（初始 +Z）；无 CD/弹药/装填限制，可穿墙和单位，沿各自方向飞完 7.67 米后移除。大招按钮禁用。退出再进入检查无残留 Sphere，原英雄移动阻挡保持正常。

NPC 阻挡本轮 `dotnet build ET.sln` 已通过：0 错误、0 警告，耗时 23.75 秒。首次编译发现测试名前缀需为 Statesync，修正后重编译通过。未启动服务器、Unity Play 或自动化用例，运行时回归由用户执行。
用户回归关注：1 米通道通过；墙/水/边缘停止和斜向滑动；摇杆死区内静止、死区外不同距离等速；松手/失焦停止；切英雄使用对应半径；可穿过 Bot/劫匪，但不能穿过带阻挡组件的墙/木桶。NPC 阻挡验证清单见 statesync/Test.md。

## 本地启动提示

MongoDB 服务需先启动；在项目根目录、管理员 PowerShell 中运行：

```powershell
dotnet Bin/ET.App.dll --SceneName=StateSync --Process=1 --StartConfig=Localhost --Console=1
```

Unity CodeMode 使用 Client。编译若提示 DLL 占用，请用户在服务器窗口 Ctrl+C 停止，不擅自杀进程；MongoDB 不用停止。
