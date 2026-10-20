# cn.etetet.statesync

## 地形与静态 NPC 实例化（2026-10-19）

- 客户端 `BrawlTerrainRenderHelper` 使用会话持有的批次和运行时材质，以8×8格分块调用DrawMeshInstanced；地形墙／草及静态NPC共用，不改变服务器格子、碰撞、AI或共享美术资源。
- `LoadTerrain`登记地形；`BrawlUnitRosterViewHelper`负责NPC模型加载登记、移除注销、根变换变化时回退；`BrawlPracticeClientComponentSystem.Update`提交绘制、Destroy同步释放材质，训练会话Cleanup也幂等清理。不得省略直接停止Play路径的资源释放。
- 编辑器Game／Scene视图通过本局持有的beginCameraRendering回调绘制，严格筛选本局BattleCamera或CameraType.SceneView；编辑器Update只确保订阅，不能重复提交绘制，支持暂停检查。Clear必须注销回调，避免旧房间残留；打包客户端不订阅该回调，仍由Update绘制BattleCamera。
- 已有清草显示通知接入移除；禁止客户端自行决定清草。原Renderer兼容回退，保留其组件和模型生命周期。新Shader／动态资源不能跳过支持性检查强制加入静态批次。
- `Brawl/Rendering/Render Diagnostics`可同房间切换对照及采样；实测、限制与代码入口见Book/Brawl客户端战斗表现.md第十三节和本包Test.md。

## 技能冷却（2026-10-18最新）

BrawlSkillCastHelper对所有发射器的普攻和大招统一读取技能CooldownTime，不再保留“雪莉普攻无CD”例外；本节覆盖下文历史无CD描述。普攻/大招独立冷却，从服务器成功施放开始计时，连发中仍不允许重入。边界回归在Statesync_BrawlRevolver_Test。

## 训练场联网迁移（2026-10-18）

训练场入口10101、练习赛入口10201统一走服务器练习房间；单机展示逻辑与RunTerrain已删除。人数和Bot由关卡引用的玩法决定（当前支持每组1人、1或2组），不是由按钮或关卡编号猜测。C2G_BrawlPracticeEnter新增LevelId，双端需要一起更新。本节覆盖下文历史“固定10201/公牛/仅本地展示”的说明。配置与验证状态见Book/Brawl开发交接.md最新章节。


## 柯尔特矩形射击走位（2026-10-18）

- 柯尔特行为树的活动连发与施法成功分支接 `BTBrawlFollowTarget`，按当前锁定施法方向及技能完整宽度纠正目标中心的横向偏差，纵向维持技能射程减 RangeMargin；不保存开火时双方偏移，不按单颗子弹判断。
- 连线遮挡仍取该技能弹丸的 BrawlCollisionRule.BlockProjectile；超出前向射程、连线或纠偏路径受阻时回退原 Chase。只写 DesiredDirection，不改 Position、施法方向或弹丸结算。
- Unity 菜单 `Brawl/AI/Enable Colt Moving Attacks` 定向升级并导出柯尔特树，重复执行保留已有节点参数；雪莉树不变。测试在 Statesync_BrawlCombatAI_Test；验收及历史覆盖关系见 Book/Braw_AI开发交接.md。

## 关卡中心原点（2026-10-15）

- 所有 Brawl 关卡世界原点为几何中心，格中心 X/Z = (column/row - (ColumnCount/RowCount - 1)/2) * CellSize。网格索引保持左下角起算，不修改 Excel。
- 统一转换在 BrawlTerrainMovementHelper.GridOffset/CellPosition；地形显示、服务端出生/复活点、NPC、地图移动碰撞及弹丸地形碰撞均已适配。坐标协议不变，双端需同时更新并重新进入关卡。
- 编辑器尺寸调整仍保留左下角数据，并非中心锚定扩图；手工场景物体不自动平移。本节覆盖历史左下角世界原点描述。

## 概述

状态同步包，包含 `ET.Model`、`ET.Hotfix` 对应的 DotNet 工程以及状态同步相关脚本、协议和资源。

## 详细文档

- **编译构建**：请查看 `/et-build` skill
- **架构规范**：请查看 `/et-code` skill

## 核心目录

| 路径 | 说明 |
|------|------|
| `DotNet~/Model` | `ET.Model.csproj` 与相关生成脚本 |
| `DotNet~/Hotfix` | `ET.Hotfix.csproj` 与相关生成脚本 |
| `Runtime/Editor` | `ET.Editor.asmdef` 通用编辑器程序集入口 |
| `Scripts` | 状态同步相关代码 |
| `Scripts/Editor` | 通用编辑器代码，通过 `.asmref` 汇入 `ET.Editor` |
| `Proto` | 协议定义 |

## Brawl 关卡编辑器

- 主包依赖 `cn.etetet.brawl`，以便 MainPackage/CodeMode 为其共享模型生成程序集引用。
- brawl 提供关卡编辑器与英雄配置；本包大厅挂载轻量英雄 UI，不改变原来的登录或进入 Map2 的流程。

## Brawl 英雄 UI

- `BrawlHeroMenuHelper` 在大厅 YIUIInitialize 创建动态 UGUI/TMP 子界面，Destroy 清理，随大厅隐藏；不修改 YIUIGen 或原 prefab。
- 状态归属 LobbyPanelComponent；事件闭包通过 EntityRef 获取存活 owner。
- 列表读取 BrawlHeroConfigCategory，暂以 UnlockPrice=0 表示默认已解锁，等级统一为 1；未接账号英雄资产或培养系统。
- 详情由 BrawlHeroDetailHelper 创建蓝色三列布局与技能弹窗，显示模型、定位、品质、描述、BaseHp + HpGrowthPerLevel、技能关联弹丸的 Value + ValueGrowthPerLevel；等级固定1／最高11，无升级入口。普通技能就是 NormalAttackSkillId，不展示 SpecialSkillId。
- BrawlHeroPreviewHelper 通过 YooAsset 加载英雄 ModelPath，复用 cn.etetet.yiui3ddisplay 的 UI3DDisplay + YIUI3DDisplayChild 持续渲染 600×800 RT，沿用 Prefab 原 Animator／Override 播放 Idle，横向拖拽绕 Y 轴旋转。RT 由 YIUI 管理，舞台／资源句柄／EntityRef／请求版本归属 LobbyPanelComponent.HeroDetails；切换／关页／销毁／进入练习赛时释放，退回大厅恢复。异步使用 EntityRef 和版本校验，禁止迟到加载覆盖新英雄。Root Motion、音效、粒子与动画事件关闭，不接战斗状态。
- 3DDisplay 依赖已加入主包；展示层为 YIUIConstHelper.Const.YIUI3DLayer（TagManager 的 YIUI3DLayer）。不能仅添加 Runtime 组件而漏掉 ModelView／HotfixView 的 CodeMode 引用。2026-10-19 实时预览本轮按用户要求未编译、未运行，验收清单见 Book/Brawl开发交接.md 最新章节。
- 已解锁、未解锁均可试用。训练场和练习赛均请求服务器创建房间。详情 UI 本轮未编译、未 Play，待用户验收；字段来源与清单位于 Book/Brawl开发交接.md 最新章节。

## 服务器练习赛第一版

- 入口固定玩法 102、关卡 10201、对手英雄 201（公牛），这是当前 Demo 约定；玩法表要求启用、TeamSize=1、TeamCount=2、TimeLimitSeconds=0。
- Gate 上通过 C2G_BrawlPracticeEnter / Exit 两组 RPC 进入和退出；进入返回完整单位快照。本轮没有动态出生/销毁通知、准备确认或战斗同步。
- BrawlPracticeSessionComponent 归属于登录 Session；其子 BrawlPractice Scene 与 Gate 共用 Fiber，包含 UnitComponent 及玩家、Bot、NPC 的真实服务端 Unit。退出或连接销毁时随实体树释放。
- 单位 ConfigId 按 Kind 指向 BrawlHero 或 BrawlNPC，禁止调用原 MMO Unit.Config() 解释这些 ID。玩家与 Bot 添加 BrawlMoveComponent；不添加原 MMO 移动、AI、数值、技能或 AOI 组件。
- 服务端校验关卡、出生点及 NPC，读取 BrawlGlobal.CellSize 计算世界坐标；客户端按快照的单位 ID、配置 ID 与坐标加载模型，不自行补出 Bot/NPC。
- EntryId 用于进入请求幂等与退出匹配；新进入替换该连接旧房间，迟到的旧退出不会删除新房间。当前不支持断线续局。
- 客户端共用 BrawlTrainingHelper 的场景、地形和退出 UI；练习赛显示服务器房间 ID，训练场同样关联服务器房间。资源加载失败尝试退出服务器房间并恢复大厅。

## 练习赛服务器权威移动

- 玩家和 Bot 共用 BrawlMoveComponent。速度暂按 BrawlHero.MoveSpeedTier 米/秒；半径读取各自 BrawlHero.CollisionRadius（米），出生检查也使用各自半径，必须为有限正数。CollisionHeight 为脚底以上完整高度，当前平面移动不使用。Bot 当前方向为零，不执行 AI 或寻路。NPC 不添加移动组件；挂载阻挡组件的 NPC 参与英雄移动限位。英雄之间可以重合。
- BrawlPracticeSessionComponent 的 Update 使用固定 50ms 累积步长，卡顿最多追赶 5 步；不使用可变帧时长直接计算大位移。
- BrawlTerrainMovementHelper 是无 Unity 物理依赖的共享几何算法：网格中心为列/行乘 CellSize；只阻挡 Wall、Water 和地图边缘。小步推进、圆与格子矩形的分轴连续限位实现滑墙；相切合法，不重新归一化滑动余量。出生时检查角色半径是否合法。
- C2G_BrawlMoveInput 只带 EntryId、输入序号、XZ 方向；服务器绑定当前连接的 MyUnitId，校验有限数与方向范围，非零方向安全归一化，不保留摇杆力度。0.5 秒未收有效输入自动停止。
- 移动输入拥有独立每秒 60 条限额，不计入 Gate 原有总数 20 条计数；每个 50ms 窗口最多接收 4 条非零方向，停止输入可以覆盖旧方向。
- 首版每个服务器移动 Tick 发送玩家与 Bot 的位置快照（静止时也发送，便于保持插值时间线），NPC 不重复同步。带 EntryId、RoomId、Tick、最后输入序号，避免旧房间/乱序快照污染。
- BrawlPracticeClientComponent 位于客户端根 Scene，通过 SceneType.Client 接收 IMessage，不能走原 MMO ICurrentMessage。每帧采集 WASD 或简易屏幕摇杆，约 20Hz 发送，开始/停止及时发送；失焦清零。
- 表现仅在已收到的快照间插值，初始缓冲 2 Tick，断流不外推、不预测、不控制 Animator。退出先停止输入并 Dispose 客户端组件，再退出服务器房间和卸载模型。
- 训练场同样使用服务器权威移动。
- 摇杆 BrawlJoystickHelper.DeadZone=0.2f，HandleRange=60，中心 12 UI 局部单位以内（含边界）停止，超过阈值全速；手柄视觉仍随拖动距离变化。碰墙滑动后的实际位移不强制恢复全速。
- BrawlNPCBlockHelper 按 NPC.ComponentTypes/ComponentValues 配对挂载组件，类型 BrawlUnitComponentType.Block=1，值为 BrawlUnitComponentBlockConfig.Id。拒绝未知/重复类型、不等长数组、缺失配置与非法阻挡尺寸。无配置即无阻挡。
- BrawlUnitComponentBlock 同时保存 CanBeDestroyed 与配置尺寸。用户明确当前不拆可摧毁组件；该 bool 不影响是否阻挡，本轮未实现伤害/摧毁。NPC 移除组件或销毁后不再阻挡。
- BrawlUnitMovementHelper 在每个分轴小步同时约束地形和带阻挡组件的 NPC，主角/Bot 共用。Width=0 为圆柱、Length 是直径；Width>0 为轴对齐矩形、Length/Width 为完整 X/Z 尺寸。中心为 Unit.Position.xz+Pivot，尺寸与偏移单位米，不乘 CellSize 或模型缩放；当前 NPC 服务器旋转恒为 identity，不支持旋转盒。出生时拒绝与阻挡 NPC 重叠。
- 几何/组件回归入口见 Test.md；测试自行构造配置。新会话交接与待办见 `Book/Brawl开发交接.md`（项目根目录）。不擅自加入英雄互撞、预测或动画。

## 雪莉普攻逐步实现（2026-10-14）

- 当前仅支持单轮霰弹发射器：服务器按所选英雄 NormalAttackSkillId 查技能与弹丸配置；雪莉 10101 每轮 5 枚，数量不硬编码。其它发射器枚举预留，未实现则忽略释放。
- IBrawlSkillEmitter 为独立算法接口，BrawlShotgunEmitter 生成局部初始数据；BrawlSkillCastHelper 负责验证输入与建实体。未来柯尔特需新增发射器及逐发调度，不能复用本轮同步生成来假装实现连发。
- 当前无 CD、弹药、装填、伤害、命中计数、Lifetime 计时、墙/单位碰撞；已有英雄移动阻挡不变。弹丸在 XZ 直线匀速飞行，中心走完配置射程后移除。
- BrawlProjectile 是 BrawlProjectileRoomComponent 下的独立 Entity，不放入英雄 UnitComponent，不进入英雄移动/出生快照。房间随 Session 销毁时一起释放。
- 弹丸与英雄共用 50ms Tick；施法先结算已有 Tick，再根据英雄当前服务器位置生成。出生与后续全量弹丸快照均带 EntryId/RoomId/Tick/Sequence，同 Tick 多次释放按 Sequence 区分，缺席表示删除。
- 练习赛 UI 显示普攻和禁用的大招按钮；普攻点击发送方向和序号，不发送技能 ID/伤害/弹丸数量。初始方向 +Z，之后取最近非零移动方向；独立瞄准和 Animator 暂不实现。
- 客户端用无 Collider 的 Sphere 显示，直径 2×Radius；高度取英雄 CollisionHeight/2 仅供显示，不参与平面计算。只插值服务器快照，不自行模拟弹丸；终点快照保留一 Tick，按显示时间线删除。退出清理按钮/采样/模型，旧房间及乱序快照忽略。

## 技能摇杆及面向扩展（2026-10-14）

- 移动、面向、技能施放方向是三个独立量。C2G_BrawlMoveInput 新增 FacingX/Z，BrawlMoveState 新增 MoveX/Z 和 FacingX/Z，入场单位快照也带 FacingX/Z。服务器分别归一化移动/面向，拒绝非法或零面向。默认面向 +Z，停止移动不清空面向，不根据瞄准自动转身。
- BrawlPracticeMovementViewHelper.SetFacingDirection 是未来技能规则的客户端入口；本轮无人调用，不实现柯尔特转身/锁定/动画/CD。客户端单位视图保存服务器返回的两个方向，暂不旋转模型。
- BrawlSkillJoystickHelper 是两技能按钮共用的手势输入，BrawlSkillAimHelper 是可测试的纯状态机。BrawlSkillViewHelper 仅建 UI，BrawlSkillInputHelper 单独发送，避免按钮创建与摇杆回调的类级循环依赖。
- 每个摇杆有独立 pointer 和 EverOutsideDeadZone。按下点作为拖动原点，行程 60、死区 20%，边界算内。未出死区松手按当前移动方向释放；静止回退最近非零移动方向，首次 +Z。出过死区又回到里面松手取消；在外面松手用瞄准方向。准备阶段只在客户端，无蓄力同步。
- BrawlMovementInputHelper 供移动更新和点按松手共用输入采样，保证点按不取过期网络帧或人物面向。普攻发有效释放包，大招只接手势，暂不发大招包。
- IBrawlSkillAimPreview.ShowOrUpdate/Hide 预留范围表现；通过 BrawlSkillJoystickHelper.SetPreview 接入。参数含槽位/技能ID/完整配置/世界原点/XZ方向，准备期间原点每帧跟随英雄。回死区、释放、失焦、退出均隐藏。不创建 mesh/范围材质/Animator。

## 弹丸碰撞与阵营（2026-10-14 最新增量）

- 本节取代之前无弹丸碰撞行为。BrawlPracticeUnitComponent.Faction 统一替代 TeamId，玩家 Red=1/Bot Blue=2，NPC 读取 BrawlNPC.Faction；入场快照和弹丸快照同步 Faction。弹丸创建时拷贝所有者阵营，排除 OwnerUnitId。
- BrawlFaction=NeutralHostile0/Red1/Blue2/NeutralFriendly3，不是 mask；同阵营不敌对，NeutralFriendly 不与任何阵营敌对，其余不同阵营敌对。两个 NeutralHostile 不敌对，第二阵营参数未实现。
- NPCTypeCombatant=1 才参与敌我目标筛选，Obstacle=2 且有 BrawlUnitComponentBlock 才按墙类处理；CanBeDestroyed 区分两种墙。Gem4/Spawn7 不参与本轮弹丸碰撞。所有 NPC 都保留配置阵营，但阵营不改变对象用途分类。
- BrawlProjectileBlockMask 和 BrawlProjectileTargetMask 是独立位值枚举，当前7/1用于雪莉。MaxHitCount=0只禁用次数终止，不忽略阻挡；有效命中按每弹丸每单位去重计数。当前不执行任何 HitEvent、伤害公式或生命/目标摧毁，不引入伤害占位流程。
- BrawlProjectileCollisionHelper 对实际移动路径收集接触并排序：先近后远，同点墙优先。圆目标用半径和，矩形使用精确圆角扩张，尺寸/Pivot来自配置。出生重叠立即检测；英雄/作战NPC相对本Tick前后位置扫掠，避免横穿漏判。水、草、可穿弹障碍不按墙处理；草掩码只预留。
- 碰撞设置 Finished，并同步接触位置；沿用下一Tick移除和客户端插值删除。Unit/NPC不会随弹丸销毁，原英雄移动阻挡逻辑不变。测试入口见 Test.md。

## 属性与弹丸扣血（2026-10-14 最新增量）

- 本节覆盖上述“不执行伤害”的历史记录。PropertyComponent挂到练习赛所有Unit，显式PropertyId编号与按来源管理的PropertyModifier字典；系统提供类型校验、输入设置与修正项增删改，变更时立即重算派生值。当前资源不因重算被重置，派生值禁止直接写入。
- 英雄Level读取BrawlHero新增配置，初始生命按BaseHp+(Level-1)*HpGrowthPerLevel；NPC暂1级、BaseHp。移速继续以MoveSpeedTier为基础，从PropertyId.MoveSpeed读取，眩晕/击飞禁止主动移动和普攻；不实现Buff产生机制。
- 发射器接收服务器英雄等级，输出EffectValue=Value+(Level-1)*ValueGrowthPerLevel，保存到独立弹丸Entity。有效首次接触执行两个HitEvent，其中1为伤害；扣血为Health-EffectValue*(1-Clamp(DamageReductionRatio,0,1))并限制最低0。没有攻击力属性，不计算护盾、死亡、复活、摧毁。
- 入场及每Tick快照同步Level/Health/MaxHealth，静止NPC也同步。客户端单位视图只保存权威结果，不增加血条。新测试仍由用户决定执行；文档见Book/Brawl开发交接.md。

## 状态拆分与生命标记（2026-10-14 最新）

- 本节覆盖之前属性内眩晕/击飞与“零血量仍可行动”的旧说明。PropertyId仅数值，900/910废弃；StateId定义Stunned100/Airborne200/Invincible300/Dead400，StateComponent按状态与来源实例维护最终bool及待同步变化。
- 所有练习赛Unit创建StateComponent。出生与Revive入口添加3秒无敌，按房间模拟毫秒到期；生命周期使用保留来源-1/-2，其它Buff来源应使用独立正ID。过期/移除只清理对应来源，不覆盖其它贡献。
- 伤害对无敌/死亡目标无效，弹丸原阻挡和计数不变；血量归零立即标记死亡并停止移动，Tick也核对HP。死亡/眩晕/击飞禁止服务端主动移动和攻击。死亡实体及原阻挡保留，不自动销毁/复活。
- 入场发送全量StateId/bool，后续随移动快照发送最终值变化；客户端仅保存到BrawlPracticeUnitView.States，不实现状态机/动画/本地倒计时。NPC死亡/重生配置列、自动调度、表现留后续。
- State相关测试见Statesync_BrawlState_Test；具体实现、边界和构建结果见根目录Book/Brawl开发交接.md。

## 出生、死亡与重生（2026-10-15 当前规则）

- 本节覆盖上方旧生命标记规则。所有练习赛Unit由BrawlUnitFactory创建，默认带Spawn/Death/State组件；StateId.Spawning=500。英雄出生2秒后无敌3秒，死亡3秒后销毁旧实体并在绑定复活点生成新实体；时长来自BrawlGlobal。
- NPC死亡按DeathDuration销毁，再从销毁时刻等待RespawnDelay后在原始位置重建；0表示永不重生。NPC没有Spawning阶段/保护，Gem和Spawn不做NPC重生。死单位立即退出移动和弹丸阻挡查询。
- 房间BrawlLifecycleRoomComponent保存参与者绑定、long[]复活点ID、纯值NPC待重生记录及通知；禁止静态System集合或保存已销毁实体引用。复活点只服务英雄，SpawnPoints前TeamSize个红方、其余蓝方；当前实际仍1v1。NPCIds不得重复配置出生点。
- 快照携带MyUnitId、变化时全量Roster和LifeEvents。客户端切换新实体模型，Spawning时隐藏人物；不实现动画/特效/倒计时/状态机。具体配置、测试和历史覆盖关系见Book/Brawl开发交接.md最新章节。

## 技能历史与训练场联网流程

- 柯尔特大招40102已实现（覆盖旧的仅充能说明）：复用Revolver，读取12发/0.08333间隔/12米/宽1配置。C2G_BrawlCastSkill按槽位选择英雄技能，普攻和大招分别冷却、共享连发互斥。成功施放大招仅扣SuperAmmo一格且保留小数；墙与敌人有效扣血后回充，碰撞仍销毁本枚。大招发射期间暂停普通装填，但大招充能不暂停。雪莉大招草稿仍未实现，详见交接文档。

- 超级技能充能（2026-10-15最新）：MaxSuperAmmo=800改为int容量、SuperAmmo=810保存连续储量，已有自动恢复属性组复用。英雄大招容量来自大招技能AmmoCapacity，初始0、死亡保存到房间参与者、重生恢复；NPC不挂BrawlSuperChargeComponent。
- 弹丸SuperChargeOnHit为每目标实际扣血后的固定充能，不看技能类型，不按伤害量缩放；HitUnits去重、双伤害事件仅充一次。当前NPC有效敌方受伤目标也可贡献，未来再加NPC资格筛选；无实际伤害/友方不充。死亡/已销毁所有者不充能，也不转交重生英雄。仅实现充能及同步显示，大招释放仍未实现。详见交接文档。

- 弹药最新规则（2026-10-15，覆盖无弹药旧说明）：普攻读技能AmmoCapacity/ReloadTime，首次满弹、重生容量1/3。Ammo保留连续装填进度，完整一格才能开火；一次技能仅扣一格并保留未完成装填进度，最后一发结束后逐格恢复。死亡/Spawning暂停装填，柯尔特一轮只扣一次。BrawlAmmoComponent记录积分时刻，资源值保存在PropertyComponent；GetDouble保持内部精度。快照同步MaxAmmo/Ammo/ReloadRate，客户端按钮仅显示权威数值；详见交接文档。

- 柯尔特普攻最新规则（2026-10-15）：Revolver=2对应BrawlRevolverEmitter，按配置单轮6发、160ms间隔、左右交替边缘偏移，方向锁定、位置随英雄。BrawlSkillCastComponent保存发射计划，SequenceHelper在服务器Tick执行绝对时刻；死亡/受控取消剩余发射。左轮读取CooldownTime（当前800ms），雪莉继续无CD，无弹药/装填。详细边界与验证见Book/Brawl开发交接.md。

- `BrawlTrainingHelper` / `BrawlTrainingSession` 是两个入口共用的联网场景加载与资源管理代码，名称保留，不再包含单机分支。删除本地英雄／NPC 创建和 `Brawl/Verification/RunTerrain` 入口。
- 训练场按钮请求 LevelId=10101，练习赛请求 LevelId=10201；C2G_BrawlPracticeEnter 携带 LevelId。服务器查关卡 ModeId，再查玩法；仅支持启用、无限时、每组1人的1组或2组布局。单组不解析对手、不建Bot；双组沿用英雄配对。
- NPC、英雄、复活点均由服务器创建并同步。血量、重生延迟、掉落沿用既有配置与逻辑，不在训练场写特殊数值。
- 两入口共用输入、技能、动画、相机及退出清理。登录连接仍必需，没有离线回退。异步加载保留 EntityRef、防重入、取消和资源释放。
- 本节及文件顶部最新迁移说明覆盖旧“本地训练场”记录。

## 账号登录界面

- 登录仍使用原有按钮，首次合法登录自动注册；账号规则和错误提示由 login 包提供。
- LoginPanelComponent.LoginInProgress 防止重复点击；失败显示提示且允许重试，不触发 LoginFinish。
- 不修改 YIUIGen 生成代码，业务界面逻辑修改 YIUISystem / YIUIComponent。

## 统一碰撞规则与通知（2026-10-15最新）

- 以Book/Brawl开发交接.md“统一碰撞规则、弹丸结束通知”为准，覆盖历史两套Mask和NPC阻挡组件规则。
- 单位BrawlPracticeUnitComponent携带UnitLayerMask/BlocksMovement，NPC读取表、英雄初始化Hero|Battle。死亡立即不阻挡。BrawlNPCBlockHelper不再挂组件1，只支持复活点组件2；旧类型代码暂保留未使用。
- 弹丸从CollisionRuleGroupId解析规则快照；标签全部满足、排除任一拒绝、阵营关系独立匹配，同组匹配结果取或且一次命中只执行一次事件组。普攻组1、大招组2来自表而非代码固定判断。
- 地形墙/草使用同一规则；房间ClearedGrass记录草清除，不改共享配置。地图、编辑器、出生点唯一来源均不改。
- BrawlProjectileNotice/BrawlProjectileEvent通知Impact和End，End细分Blocked/HitLimit/Range/Lifetime；每次接触一次Impact、每弹丸一次End。协议新增Events和完整ClearedGrass集合。客户端复制值数据、按EventId去重并等RenderTick回调IBrawlProjectilePresentation；本轮无特效实现，草模型也暂不隐藏。
- 源表配置与协议已正式导出；编译0错误、14个原有HybridCLR警告，Unity错误日志为空。测试仅编译未运行。

## 弹丸客户端表现（2026-10-15）

- BrawlProjectileVisualHelper加载ModelPath/ImpactEffectPath/ExpireEffectPath，BrawlProjectilePresentation为无状态适配器，调用显式传入BrawlTrainingSession。会话保存模板/配置/粒子实例，退出时清理。
- Impact逐接触播放；只有Range/Lifetime播放Expire，Blocked/HitLimit不播放。飞行位置/方向来自服务器；不使用客户端物理处理命中。
- 特效预制体不附带自主移动/定时销毁演示脚本；一次性特效要求ParticleSystem，以全部活动粒子结束回收。草模型和发射/人物动画仍未实现。

## 通用Brawl相机

- BrawlCameraComponent是Scene组件，持有Camera/Target与配置参数，System在LateUpdate按固定透视规则跟随目标。BrawlTrainingHelper公共入口创建并绑定，Roster变更后跟随新主角，退出释放。未来玩法复用该组件，不按模式ID分支。
- 参数来自客户端BrawlGlobal五个Camera字段；BrawlCameraInputHelper把移动/技能拖动方向转换到相机水平朝向。输入只旋转至XZ，不参与服务器相机计算。
