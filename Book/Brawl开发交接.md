# Brawl Demo 开发交接

## 2026-10-18 训练场统一联网

- 训练场按钮保持原 UI，按用户最终决定使用 **10101**；练习赛仍为 **10201**。删除本地展示分支、本地英雄/NPC 创建及 `Brawl/Verification/RunTerrain` 验证工具。`BrawlTrainingHelper/Session` 保留为联网共用资源管理代码，不再支持离线回退。
- `C2G_BrawlPracticeEnter` 新增 LevelId，双端协议已生成；服务器从关卡关联玩法，要求启用、无限时、TeamSize=1、TeamCount=1或2。单组只创建玩家；双组才查英雄配对、生成 Bot。重复 EntryId 同时核对英雄与关卡，新 EntryId 仍替换旧房间。
- 所有单位来自服务器，木桩沿用 NPC 表的血量、死亡保留与复活配置。当前源表 NPC 401 为5000血，死亡保留0.2秒、销毁后10秒重生；本次没有新增掉落逻辑或修改源表数值。
- 用户最终决定直接使用原关卡10101（ModeId=101，1人×1组、1个出生点）。复制的10202保留原配置，不作为UI入口；本次不改其玩法引用。
- 使用正式 Proto2CS、ExcelExporter 导出，保留源表已有英雄202草稿和NPC401等数据；没有手写生成配置。构建结果见 statesync/Test.md。未启动服务器或进入 Unity Play。
- 手测：加载更新后的双端代码和配置，选择训练场进入10101，确认只有玩家、配置NPC和复活点；测试移动、攻击木桩、木桩重生、玩家重生、退出后重进。再进入10201，确认雪莉／柯尔特互配及原有Bot行为正常。两个入口均需服务器连接。


## 已知问题记录入口

[brawl_issue.md](brawl_issue.md) 集中记录已实现功能中已发生、尚未解决的缺陷；后续新缺陷继续追加，修复时补充验证结果。未实现功能与待定设计属于 TODO，不混入 issue。当前 `BRAWL-ISSUE-001` 为复杂地图下练习赛卡顿：还原地图后暂时规避，寻路性能原因尚未采样确认，代码尚未修复。

## 2026-10-16 表现暂缓与讨论交接（TODO，尚未实施）

用户最新安排：暂缓音效、特效扩展及英雄表现打磨，后续再处理。本节保存未实现事项和短暂讨论的建议，不代表这些建议已经确认或授权立即开发；它们属于 TODO，不登记为已知缺陷。

### 已有基础，后续不要重复创建

- 角色固定武器动画映射、独立运行时 Override、待机／四向移动、按原生步幅匹配速度、出生／攻击／死亡动作已经接入。出生暂用相应武器 Idle；攻击走上半身，死亡走全身。具体资源与验证边界见 [客户端战斗表现](Brawl客户端战斗表现.md) 第九至十二节。
- 扇形、矩形及自身中心圆形的瞄准预览已经实现；圆形暂未接实际技能，不需要重新造网格实现。
- `G2C_BrawlSkillUse` 已提供 Started／Fired／Completed／Interrupted，包含 EventId、CastId、UnitId、SkillId、Tick 等。客户端校验房间、去重、按 RenderTick 消费，丢弃过期的开始／发射，保留结束／中断清理。
- 服务器同轮多颗弹丸只发一次 Fired；雪莉每轮一次，柯尔特按连发节奏逐次。玩家与 Bot 共用通知和动画入口。
- 弹丸飞行／接触／射程或寿命结束特效已有实现与模板预加载，仍通过 Instantiate／Destroy 管实例，尚无实例对象池。现有相机、弹丸资源映射等详细记录继续保留在本交接原章节。

### 尚未实现或仍需后续调校

- 开火、换弹、接触等技能声音；枪口火光／烟雾与其他施法特效。
- 专用起手／蓄力／持续施法表现、不同技能的动作层与动作映射。预留 Animator 槽位不代表业务已经接入；现有上半身枪械攻击入口不能直接代表未来所有英雄动作。
- 换弹和受击动作，眩晕／击飞／击倒等更完整的状态表现及打断优先级；无敌、出生、复活、死亡的附加视觉／声音反馈。
- 头顶血条、弹药／大招状态展示、伤害飘字、NPC 受击与销毁反馈；现有技能按钮的权威资源数值显示不等于头顶 UI 已实现。
- 角色朝向与上半身攻击的观感打磨、起停／急转／滑墙残余脚滑及实际武器握持校对。已有速度换算不等于逐帧足底锁定；不自动扩大为 IK 或装备系统。
- 高频弹丸／一次性特效实例池、音效实例池与并发限制；退出、死亡、重建及重复进场的资源回归验证。
- 自动逐格装填的表现需要单独定义，不能每次弹药数值增长就播放一整次换弹。仅实际扣血才能驱动受伤反馈，接触不等于伤害成功。

### 音效／特效简化方案（仅讨论建议）

1. 复用现有权威事件：Started 可接起手，Fired 接开火声与可选枪口效果，Completed／Interrupted 清理持续效果；弹丸 Impact／End 接接触及结束反馈。短促声音通常自然结束，持续声音／特效才按施法身份停止。
2. 动画、声音、特效是同一事件下的并列消费者，不把它们全部放进 PlayAttack，也不依赖购买动画的 AnimationEvent。客户端播放不产生子弹、不扣血、不决定施法成功。
3. 建议先用一份客户端 ScriptableObject 按 SkillId 关联开火 AudioClip、音量、可选特效 Prefab、挂点／偏移／是否跟随；具体配置载体尚未定稿。原弹丸 ModelPath／ImpactEffectPath／ExpireEffectPath 继续使用，不重复建立映射。
4. 挂点在模型加载时缓存。跟随效果使用角色当前显示姿态，落地／接触效果固定在通知给定的位置；死亡、退出和重建清理旧引用。枪口效果此前明确暂不做，配置可保持为空。
5. 入场只预加载本局需要的资源。少量 AudioSource 复用并限制同音效并发；Demo 首版建议 2D 声音，空间衰减后续按俯视相机调校。特效按资源复用，归还／再取时清空粒子和拖尾，退出统一释放。
6. 声音与特效沿用事件去重和显示时间线，不因迟到集中补播一串枪声。新消费者不能因单位没有 Animator 就完全失效。

以上音效、配置资产和池化均未编码、未创建资源，也未进行编译或运行验证。

## 2026-10-17 新英雄“桩”两段普攻（需求澄清，尚未实现）

用户在 2026-10-17 明确：这是同一次普攻的两段效果队列。忽略网络延迟，0 秒服务器接受施法并回 SkillUse，客户端收到后立即开始蓄力、显示环形金黄色特效；约 0.3 秒后再释放圆形特效，并在该阶段造成实际范围伤害。不能把完整技能仅概括为“延迟生成一个圆形弹丸”，遗漏第一段明确可见的表现。

仍只做统一范围伤害，不做蚂蚁、增益、边缘／中心伤害差异。第一段所谓“外圈”是金色环形视觉，不是额外伤害圈；不在第 0 秒提前扣血。数值引用中的 1400／1050 不是本 Demo 已确定的伤害配置，半径、统一伤害、弹药与装填等数值仍待落实。

### 建议的最小时序实现

| 阶段 | 服务器 | 客户端 |
| --- | --- | --- |
| 0 秒：开始 | 校验施法，消耗一次弹药，建立同一个 CastId 的延迟计划，通知 Started。 | 收到开始通知后立即进入蓄力、播放金色环形效果；此时不结算伤害。 |
| 约 0.3 秒：释放 | 延迟到期，执行一次圆形范围伤害，通知 Fired，随后完成本次计划。 | 收到 Fired 后结束蓄力阶段、播放第二段圆形释放效果。金环建议在此停止，具体淡出可由资源调校。 |
| 完成／中断 | 正常结束通知 Completed；释放前若按现有规则中断则取消尚未执行的伤害并通知 Interrupted。 | 完成时清理阶段状态，已发出的一次性释放特效自然播完；中断时清理金环，不播放未发生的第二段。 |

- 两段共享一个 SkillId／CastId、一次施法请求及一次资源消耗。客户端不为第二段再发送 UseSkill；第一段也不需要创建无伤害的服务器弹丸来占位。
- 在技能配置中保存释放延迟（暂 0.3 秒），在客户端表现配置中关联 Started 对应的蓄力／金环，以及 Fired 对应的圆形释放效果。现有两个事件足以标识这两个阶段，本阶段不必新增通用技能图编辑器或另起调度框架。
- 服务端计划负责伤害时刻，客户端只播放阶段表现。蓄力进度可以读取同一份延迟配置，但不能用客户端 0.3 秒计时回调决定服务器伤害或无视中断强行释放。
- 当前 BrawlSkillUseViewHelper 把所有事件排队等到 RenderTick，并未满足“收到立即表现”。建议为此技能的两段效果采用一致的收到即消费策略，沿用房间校验、事件去重和 CastId 匹配；不要仅提前 Started，却让 Fired 多等待插值缓冲。既有角色位置仍使用显示插值，不能为技能效果改变权威位置或移动规则；其他英雄继续保留原播放策略。
- 忽略网络时两个阶段相隔约 0.3 秒；实际网络下以已确认的阶段推进。若开始、释放通知因延迟同批到达，不强行再补一整段 0.3 秒蓄力。模型未就绪、死亡／重建与旧事件仍需做有限等待或丢弃，不能让旧金环挂到重生实体。

### 已核查的复用点

- `BrawlProjectileLaunch.DelayMilliseconds` 与 `BrawlSkillSequenceHelper` 已支持服务器模拟时间延迟，执行时取施法者当前位置。0.3 秒对应当前 50 毫秒步长的 6 Tick；客户端蓄力表现不能决定伤害时刻。
- 弹丸出生时调用 `BrawlProjectileCollisionHelper.Advance(..., 0)`，先检测当前重叠并执行命中，再判断射程结束。现有圆与圆／矩形几何支持零位移重叠查询。
- 第二段伤害可新增通用圆形单次范围发射器，延迟生成一个 Offset=0、Speed=0、Range=0、Radius=技能范围半径的逻辑弹丸，一次结算后立即 Finished，沿用已有结束通知与随后移除流程。这只负责第二段判定，不替代第一段蓄力与金环表现。Lifetime=0 在当前代码中表示不按寿命限制，不能把它当作立即结束的开关。
- 复用阵营／标签规则、HitUnits 去重、伤害成长／减伤／无敌／死亡判定、弹药消耗以及 Started／Fired／Completed／Interrupted 通知。伤害碰撞半径和指示器应取同一配置含义，避免两份半径不一致。身体碰到圆边缘是否命中，按现有碰撞体相交语义处理，而非只测目标中心。
- 当前只有 Shotgun／Revolver 发射器实现；Circle 范围枚举与预览已有，但新技能不能只填表就运行。建议新增独立的圆形范围发射器类型，不把预留 Knife／Boomerang 随意改作别的含义。

### 实现前需明确或检查

- 建议延迟期间允许移动，到点以角色当时的位置为圆心；死亡／受控沿用现有中断，施法成功开始时扣资源。此处是建议规则，尚未由用户逐项确认。
- 一次旋风斩只做一次范围采样，圆内各目标最多受伤一次；视觉转圈持续多久与伤害检测次数分离。MaxHitCount=0 在现有语义下为不限制总命中数量，并不表示禁用伤害。
- 需要专用 `BrawlCollisionRule` 规则组，避免首个目标的 BlockProjectile 或命中数上限提前终止整次范围结算。是否穿墙／伤墙／清草仍需用户定规则；不能直接复制枪弹遇墙结束的配置。若要求墙遮挡，应对各候选目标分别检查遮挡，不能因圆内一面墙就取消其他方向的命中。
- `TryStart` 当前普攻冷却只对 Revolver 显式使用配置；接入新类型时需明确旋风斩冷却并检查该分支，保持现有英雄行为不变。
- 现有调度把任何带延迟的发射标为 Sustained，客户端会锁向；自身中心圆形技能不依赖方向，需要区别“有前摇”与“必须锁向”。现有攻击统一上半身，桩的蓄力／旋转动作应按该技能单独映射，不能直接套枪械入口。金环和释放效果作为阶段表现独立管理，不因沿用通知就声称已支持。
- 本轮未改英雄／技能／弹丸源表、发射器、协议或任何运行代码。只完成源码核查和讨论记录；接入新英雄时还需明确模型及练习赛对手配对，不能默认复用雪莉／柯尔特双向配对规则。

## 2026-10-16 首次入场弹药修正

- 首次入场和重生现在统一为 `Ammo = MaxAmmo / 3`，保留小数，容量 3 时是 1/3。玩家和 Bot 共用；下方历史“首次满弹”的规则已被本节替代。
- 仍由 `BrawlAmmoHelper.Initialize` 显式设置 `PropertyComponent` 的 MaxAmmo、Ammo、BaseReloadRate；属性组件不自动决定各资源的初始比例。移除初始化链中不再需要的 isRespawn 参数。
- 出生 2 秒暂停装填、出生完成后恢复、连发及死亡期间暂停装填均保持现状。本轮不接开局倒计时或全员进场确认。
- 现有 Ammo／Revolver 用例同步更新首次一格及单次连发扣一格的断言；只编译，不运行服务器或 Unity Play。
- `dotnet build ET.sln` 通过：0 警告、0 错误，52.66 秒；日志 `Temp/BrawlAmmo-initial-third-build.log`。使用更新后的服务端重新进场验收；如场景加载期间出生已完成，弹药可能已按原规则继续恢复。

## 2026-10-16 战斗 Bot 最新更新

练习赛已改为雪莉 101 与柯尔特 401 互为运行时对手，分别使用独立行为树；没有 5 米追击限制，优先大招、其次普攻。最新调整为每个技能按自身射程减 0.5 米独立判断距离：当前雪莉两种技能 7.17 米、柯尔特普攻 8.5 米／大招 11.5 米。矩形视线通过各自弹丸规则组的 `BlockProjectile` 判断墙体，复用 `BrawlCollisionRuleHelper.Match`；当前配置允许大招穿过可摧毁墙。没有可用射击位置则继续复用原 A* 绕路。Bot 调用已有权威施法及表现通知，未改协议、Excel、发射器或弹丸结算。详细代码、资源菜单和手测清单见 [AI 开发交接](C:/Work/UnityProjects/ET/Book/Braw_AI开发交接.md) 首节。

最新技能射程／碰撞规则接入已完成资源导出和 `dotnet build ET.sln`：0 错误、14 个既有 HybridCLR 警告，74.48 秒，日志 `Temp/BrawlAI-skill-rules-build.log`。未启动服务器、进入 Unity Play 或执行测试。下方“公牛仅在 5 米内追击／没有攻击 AI”为历史快照，不代表当前实现；出生、攻击、死亡动作以客户端战斗表现第十二节为准。

## 2026-10-16 当前实现与面试资料入口（优先阅读）

本文件保留多轮历史记录，下方多处“最新”“尚未实现”只对应当时阶段，不能横向拼成当前状态。此次只查源码、生成配置、资源与既有日志并补文档，没有改代码/配置、构建或运行游戏。

- [Brawl项目代码架构](C:/Work/UnityProjects/ET/Book/Brawl项目代码架构.md)：按当前代码整理登录、进退场、服务器权威移动、技能/弹丸、属性/生命周期、Bot行为树/A*、客户端动画/预览及主要配置字段。
- [Brawl AI协作与个人复盘](C:/Work/UnityProjects/ET/Book/Brawl_AI协作与个人复盘.md)：以13年Unity客户端、首次深入ET/服务端为背景，说明能力观察、Codex/Harness/AGENTS/UnityBridge、测试和诚实的面试表达。
- [面试速读](C:/Work/Website/zhaoye/面试题和复习/AI使用/ET_Brawl面试速读.md)：30分钟复习、5分钟展示、常见追问与启动检查。
- 专题仍保留原处：[客户端战斗表现](C:/Work/UnityProjects/ET/Book/Brawl客户端战斗表现.md)、[公牛AI交接](C:/Work/UnityProjects/ET/Book/Braw_AI开发交接.md)。相机和已有弹丸特效不迁出本文件。

当前要点：练习赛102/10201为Session关联的服务器房间，仍在Gate Fiber；移动是50ms权威模拟加客户端2Tick插值，无客户端预测。雪莉/柯尔特普攻与大招、弹药/充能、死亡重生已有实现；公牛有5米内行为树追击及A*，尚无攻击AI。客户端已接移动动画和技能范围预览，但完整攻击/死亡动画、客户端草模型清除、特效池等仍未完成。不要再用历史“无限弹药/没有AI/尚未实现伤害”的文字概括当前版本。

验证口径：动画与指示器有之前0错误构建记录；最新步幅调整的`Temp/BrawlAnimation-speed-build.log`记录DLL占用导致完整构建失败，未在本轮补跑。AI专题有通过记录，但本轮未找到其引用的final日志，未重新执行。资源检查、构建和Play分别说明，不笼统宣称当前全部通过。

资源提醒：动画专题记载公牛SalarymanDefault，但本次已生成英雄201配置仍为BasicMaleDefault；运行时以实际加载配置为准。本轮未改源表、导出数据或Prefab。

## 关卡中心原点（2026-10-15 最新）

- 世界原点改为关卡几何中心，覆盖后文历史左下角坐标公式。格中心 X=(column-(ColumnCount-1)/2)*CellSize，Z 同理；Y 不变。偶数地图中心在格间，奇数地图中心在中央格。
- 例如 20×20、格长1：格中心从 -9.5 到 9.5，地图外边界 -10 到 10。网格索引、出生格/NPC格索引、Excel 和协议不改。
- BrawlTerrainMovementHelper 提供共用 GridOffset/CellPosition。客户端地形/本地训练人物、服务器NPC/出生验证/复活点、移动边界及地形碰撞、弹丸候选格查询及碰撞中心统一转换；NPC/英雄之间的世界坐标相对几何不需要另加偏移。
- BrawlTrainingVerification 原有入口保留并改用新坐标签名。相机跟随主角，无旧中心偏移。用户手工摆放场景物体不自动平移。
- 编辑器扩缩行列仍按原规则保留左下角数据；中心锚定扩图未实现。如需保持中心附近格子的世界位置，应另行设计奇偶尺寸变化和裁剪规则。
- 客户端和服务端必须同时更新，重启服务端并退出旧关卡重新进入。此次不进行 Unity Play/自动化游戏测试。首次构建因旧服务器占用 DLL 失败；用户停止服务器后重新执行 `dotnet build ET.sln` 已成功：0 警告、0 错误，耗时 24.16 秒（2026-10-15）。

更新日期：2026-10-15。此文件用于新会话恢复上下文，不代表授权立即实施所有待办。

## 快速恢复：当前实现与下一阶段

- 最新已接入统一碰撞规则、NPC标签/移动阻挡、房间草清除逻辑及弹丸碰撞/结束通知。雪莉和柯尔特普攻、大招均使用新规则；详见紧接下方“统一碰撞规则、弹丸结束通知”，旧Mask和雪莉大招草稿说明已过期。客户端弹丸表现已接入，见最上方弹丸表现章节。

- 柯尔特大招40102已接通，复用左轮发射器，12枚弹丸、消耗一格大招储量、命中回充及摧毁墙体。以“柯尔特大招释放”章节为准，下方充能阶段的“大招仍未实现”仅适用于历史记录及当前雪莉草稿大招。

- 最新已接入英雄大招充能、死亡保留和客户端数值同步，尚未接大招释放。规则见“超级技能充能”；NPC自身不拥有这套大招资源，但本版可给攻击它的英雄贡献充能。

- 最新已接入雪莉/柯尔特普攻弹药与装填，首次满弹、重生容量1/3；以“弹药与装填”章节为准，历史“无限弹药/不做装填”已过期。

- 柯尔特40101普攻已接入第二个独立发射器与服务器连发计划，当前验证结果见下方“柯尔特普攻”章节。历史“仅雪莉/不做柯尔特”不再代表当前范围。

- **当前生命周期以紧接下方的2026-10-15章节为准**：英雄死亡销毁后创建新实体，在绑定复活点出生；NPC销毁后延迟原地重生。下面旧章节的NPC出生无敌、Revive原实体、尸体仍阻挡和不自动复活均为历史记录，已被本轮替代。

- 最新状态拆分已编码：属性只保留数值；StateId/StateComponent管理眩晕、击飞、无敌、已死亡，出生/复活保护3秒，死亡后保留实体并禁止行动，服务器同步最终bool。详见下一节“状态组件、出生保护与死亡标记”；本节以下旧记录的“尚未处理死亡”不再代表当前行为。

- 已实现：服务器权威移动、NPC 阻挡、移动方向与人物面向独立同步、普攻/大招技能摇杆与预览接口、雪莉5枚弹丸及服务器位置同步、弹丸连续扫掠与阻挡/有效命中计数销毁、阵营。详见下文最新增量，历史阶段记录不代表当前行为。
- 最新用户已授权文档完成后直接编码；属性组件、弹丸等级成长、HitEvent=1扣血和生命状态同步已接入，构建状态见下方“属性与扣血实现”。本版不处理单位死亡/复活、护盾、柯尔特连发、CD/弹药/装填、瞄准范围mesh和动画。
- 下方属性/伤害方案保留设计缘由；其“尚未实现/建议”措辞为前一轮讨论记录，实际落地差异以紧接着的最新实现章节为准。
- 移动与面向已完成部分见“技能摇杆与独立面向”；属性枚举、修正项及伤害流程见“属性系统方案”和“弹丸数值与扣血方案”。后续开发先读这三处，勿从早期技能草案重新推导需求。

## 最新增量：通用Brawl透视跟随相机（2026-10-15）

- 用户要求所有Brawl玩法采用同一规则：透视、固定距离/角度/垂直FOV，始终跟随主角。新增Scene级BrawlCameraComponent及System，不依赖玩法ID或练习赛分支。当前训练场/练习赛公共入口均已接入；未来新增玩法应绑定同一组件，不复制相机算法。
- 原TrainingCamera改名BrawlCamera，移除正交投影、按地图尺寸定位和缩放的逻辑。相机在LateUpdate跟随角色渲染位置，不跟随角色转身，不做额外平滑、边界限位或自动缩放。
- BrawlGlobal.xlsx新增客户端参数（c组）：CameraDistance=18米（相机到观察点直线距离）、CameraPitch=45度（X轴俯角）、CameraYaw=0度（Y轴水平角，朝世界+Z）、CameraFieldOfView=45度（垂直FOV）、CameraTargetHeight=0.5米（相对角色原点的世界Y偏移）。值只是参考截图的初始调校，未宣称还原原游戏真实参数；用户可改表导出后重进场景调试。
- 相机旋转Quaternion.Euler(Pitch,Yaw,0)，观察点=角色世界位置+up*TargetHeight，相机位置=观察点-旋转后的forward*Distance。距离>0、俯角(0,90)、FOV(0,179)，所有参数必须有限；非法配置报错，不静默替换。
- 目标Transform由场景入口绑定，练习赛随MyUnitId/英雄模型更换重新绑定。死亡销毁至新模型加载完成期间保持最后镜头；出生阶段隐藏的英雄模型仍可被跟随。退出/加载失败释放相机组件并恢复原大厅相机。
- 移动键盘/摇杆以及技能拖动方向统一经BrawlCameraInputHelper映射至相机水平朝向的XZ坐标，默认Yaw=0与旧操作相同；点击施法仍使用当前/最后移动方向，拖回死区取消规则不变。预览接口传递世界方向。
- 对等高等深的箱子，45度俯角是顶面/正面视觉大小接近的初始选择；透视下还受观察位置、箱子尺寸和FOV影响，不保证所有屏幕位置投影面积严格相等。
- 验证：ExcelExporter成功；dotnet build ET.sln通过，0错误、14个已有HybridCLR警告，60.97秒。日志Temp/BrawlCamera-export.log、Temp/BrawlCamera-build.log。未启动Play或服务器；用户手测移动跟随、转身不转镜头、死亡重生切换及返回大厅恢复相机。
- 本轮只新增全局相机配置，未覆盖用户调换的墙模型路径；统一ExcelExporter会同时导出当前源表中的模型修正。池化和客户端草移除仍属于已记录的后续待办，本轮不顺带实现。

## 讨论记录：特效池暂缓、草清除表现待接（2026-10-15）

- 用户已手测当前弹丸表现基本正常。明确暂不实现对象池，本节仅记录方案，不代表授权立即池化。
- 当前BrawlTrainingSession持有资源句柄、模板、弹丸与一次性特效列表，BrawlProjectileVisualHelper集中管理。已有资源/模板缓存，但每次仍Instantiate，结束Destroy；历史“回收”是销毁清理，不是返回对象池。
- 已查ET.ObjectPool：用于普通C#对象和IPool，Fetch/Recycle/Clear，不直接管理Unity预制体与粒子。YIUI有集合/引用池及AutoRecycleObjPool，但后者固定约一秒回收，不能直接承担不同持续时间的战斗特效生命周期。当前未找到可直接接入的完整战斗GameObject特效池。
- 后续建议场景级特效池组件，按资源地址分池；取出时重置Transform、粒子、拖尾和活动状态；返回时停止并清空粒子/拖尾、禁用实例；退出场景统一销毁池和释放资源句柄。飞行物按服务器结束，一次性特效按粒子结束返回池。遵循ModelView存数据、HotfixView执行逻辑，避免静态可变全局集合。
- 用户表示已在源配置中调换此前配反的不可摧毁墙与可摧毁墙模型。本轮未改写或导出这些配置；后续以最新源表为准，不按旧文档模型名覆盖。
- 大招清草目前服务器已有：地形草按Grass|Destroyable匹配碰撞规则；有效接触且带正值Damage事件时，把格子索引加入房间ClearedGrass，后续弹丸跳过该草格。无NPC、无HP/死亡计时、不提供大招充能，也不改共享MapGrid。
- 当前客户端已接收ClearedGrass，并在RenderTick到达后调用OnGrassCleared，但该回调为空，所以画面草仍存在。这是尚未接完的客户端地形表现，不需要把草改刷为NPC。
- 待实现方案：LoadTerrain时保存草格索引到GameObject的映射；OnGrassCleared按索引移除/隐藏该草模型并删除映射；场景退出清理映射。加载与快照时序需保证已清除草不被异步加载重新显示；完整ClearedGrass列表可用于状态补齐。复用已有同步，不增加草Unit或重新刷地图。先直接移除草，不擅自添加草专属破碎特效或重生规则。

## 最新增量：弹丸飞行、击中与自然消失表现（2026-10-15）

本节覆盖历史“客户端仍用Sphere、不加载特效”的描述。发射烟雾、人物动作、瞄准范围与草模型移除仍未实现。

- 客户端根据弹丸配置 ModelPath 加载飞行预制体；空值保留碰撞直径大小的Sphere兜底。有模型时保留美术资源自身缩放，不用逻辑碰撞半径缩放特效。
- ImpactEffectPath：每个服务器Impact通知播放一次，包括只阻挡不生效的墙、穿透的每个有效目标。继续沿用EventId去重、RenderTick延迟呈现，不由Unity Collider判定命中。
- ExpireEffectPath：仅EndReason为Range/Lifetime时播放；Blocked/HitLimit只停止飞行表现，退出场景清理不播放消失特效。
- BrawlProjectileState增加DirectionX/DirectionZ，飞行模型以本地+Z对齐服务器方向；击中特效按XZ接触法线定向。配置和协议已经重新导出，客户端/服务器须使用同一版并重启。
- BrawlProjectileVisualHelper预加载配置资源，场景内使用未激活模板克隆。飞行粒子循环至服务器结束；击中/消失粒子不循环，所有活动子粒子系统结束后回收。一次性特效必须包含ParticleSystem。退出练习场销毁模板、飞行物、特效并释放已有资源句柄。
- 客户端禁用克隆上的Collider/Rigidbody物理碰撞和粒子碰撞/触发模块，避免与服务器权威逻辑重复。用户已移除ProjectileMover与AutoDestroyPS演示脚本，运行时代码不再按脚本名称处理；后续资源也不应附带自主移动、碰撞生成特效或定时销毁脚本。
- AddressByFileName使用真实文件名。本轮将四种弹丸的12个资源地址下划线改为空格，对应现有预制体：10101使用Projectile 10 / Hit 10 / Flash 10；10102使用6；40101使用1；40102使用14。仅修改资源地址，不调整技能数值。源表备份Temp/BrawlSkill.before-vfx-addresses.xlsx。
- 实现入口：BrawlProjectilePresentation、BrawlProjectileVisualHelper、BrawlProjectileViewHelper；BrawlTrainingSession管理模板、配置和一次性特效。进入场景预加载完成后才接收弹丸表现。
- 验证：配置/协议导出成功；dotnet build ET.sln通过，0错误、14个已有HybridCLR警告，30.92秒，日志Temp/BrawlProjectileVfx-build-final.log。首次ET0004已通过无状态表现适配器修复。Unity Refresh成功，Console错误查询为空；新增meta由Unity自动生成。未运行游戏测试。
- 手测：两位英雄普攻撞墙/命中敌人出现击中特效；大招穿透连续目标时逐次播放击中特效且继续飞行；朝空地发射只在射程终点出现消失特效；切回大厅不遗留粒子。未启动Play或服务器，视觉效果由用户测试。

## 最新增量：统一碰撞规则、弹丸结束通知（2026-10-15）

本节优先于下方历史BlockMask/TargetMask、阻挡组件及“大招命中即销毁”的描述。

- 已将雪莉10101/10102、柯尔特40101/40102接到CollisionRuleGroupId。BrawlSkill.xml注册BrawlCollisionRule分页，正式导出三端配置；最新源表已删除旧BlockMask/TargetMask列。其它未完成技能不补配置，缺失规则组时拒绝施放且不扣资源。
- BrawlUnitLayer位值：Hero1、Monster2、Battle4、Tile8、Grass16、Destroyable32。英雄创建时赋Hero|Battle=5；NPC从UnitLayerMask/BlocksMovement读取。墙/木桶40且阻挡移动，劫匪6且不阻挡移动，水晶/出生点0。阵营沿用已有关系，不用标签推导敌友。
- BrawlNPCBlockHelper现只校验/初始化NPC标签、几何和可选复活点组件，不再挂旧阻挡组件；类型1引用会明确报错。旧阻挡组件类型/配置文件暂保留但运行流程不再使用。移动限位直接读取BlocksMovement和Unit几何，死亡/Spawning单位不阻挡，英雄互相可重叠。原圆柱直径、矩形完整尺寸及Pivot规则不变。
- 碰撞规则由配置读取，不硬编码组号。Required要求全部位、Excluded任一位排除、Relation=1敌对/2非敌对/3不限。同组匹配行的BlockProjectile和ApplyHitEvents分别取或；多个匹配行不会重复执行HitEvent组。施放前校验规则存在、位值合法、条件不矛盾、Lifetime非负有限，再扣弹药/大招储量。规则数组保存到施放计划并传给每枚弹丸。
- 普攻组1：所有Tile仅阻挡；敌方Battle生效并阻挡。大招组2：Tile且无Destroyable只阻挡；Tile|Destroyable、Grass|Destroyable和敌方Battle生效且穿透。两种大招MaxHitCount=0，仍被不可摧毁地形墙挡住。有效命中次数与实际扣血区分；无敌目标仍匹配/产生接触，但不扣血或充能。
- 最新导出参数：雪莉大招10102速度9、基础数值288、每级32、SuperChargeOnHit0.1、单轮5枚、7.67米/45度。柯尔特40102仍12枚/0.08333秒间隔/12米/宽1、速度10、数值320/32、回充0.1。两种大招复用各自已有发射器；雪莉描述中的击退尚未实现。
- 地图编辑器和地图摆放不改。不可摧毁墙、草仍来自MapGrid；草在查询时视为Grass|Destroyable，墙视为Tile，阵营NeutralHostile。地形候选和单位候选共享规则与最近接触排序。水、空地、可穿弹障碍沿用当前不参与弹丸碰撞的行为。
- 房间BrawlProjectileRoomComponent.ClearedGrass保存已清除草格索引，不改共享关卡配置；匹配生效且带正值Damage事件的弹丸可直接清草，无单位HP/死亡流程，不提供大招充能。逐弹丸HitTerrain去重；后续弹丸忽略已清除草。退出房间一起清理，不实现草重生。
- 保留XZ连续扫掠、目标相对运动、圆/精确圆角扩张矩形、出生重叠检查、同距离墙优先。每枚弹丸每个单位/地形格最多处理一次；先近后远执行，死单位跳过。阻挡、MaxHitCount正上限、射程终点、Lifetime正值到期均可结束；Lifetime=0不计时。短寿命会裁剪本步路径，避免在寿命之后命中目标。
- 新增BrawlProjectileNotice值记录和BrawlProjectileEvent协议。Kind=Impact1/End2；结束原因None0/Blocked1/HitLimit2/Range3/Lifetime4。事件包含房间内递增EventId、Tick、弹丸ID/配置ID/所有者ID、目标ID或TerrainIndex（无地形为-1）、位置、XZ法线、Blocked/Effective。Impact位置为弹丸接触表面的位置；End位置为弹丸中心，出生内部重叠法线约定取反飞行方向。
- 同次接触只产生一次Impact，最终结束再产生一次End，Ended后不重复发送。End的Blocked/HitLimit只用于停止飞行/删除，不应额外播放自然消失特效；Range/Lifetime才可播放ExpireEffectPath。房间退出直接清理，不播放自然消失。终点仍保留一帧位置快照后删除，避免客户端提前消失。
- G2C_BrawlProjectileSnapshot新增Events和完整ClearedGrass列表。客户端先检查EntryId/RoomId/Sequence，再按EventId去重；复制协议为独立值记录（不持有池化消息），等RenderTick到达再调用IBrawlProjectilePresentation.OnProjectileEvent/OnGrassCleared。入口为BrawlTrainingSession.ProjectilePresentation，默认null；客户端保存ClearedGrass供未来表现读取。新旧协议不兼容，需要双方重启使用同一次生成结果。
- 用户明确本轮不做客户端表现：仍显示原Sphere；ModelPath/ImpactEffectPath/ExpireEffectPath已随源表导出但不加载播放，没有发射烟雾、动画、击中特效、消失特效，也不移除客户端草模型。服务器草已清除但画面仍可见属于本轮范围，不是地图未同步。后续接表现时使用通知和已清草集合。
- 复活点来源不变：SpawnPoints按队伍顺序加载为NPC并绑定玩家，不往NPCIds重复刷。NPC原地重生和英雄复活流程保持不变。
- 测试：新增Statesync_BrawlCollisionRules_Test及自建规则配置helper，覆盖阵营/标签、缺失组、草→木桶→敌人→地形墙的穿透顺序、非敌方穿过、死亡立即不阻挡、私有地形状态、事件去重、射程/寿命/命中次数结束及重复规则不重复伤害。迁移旧几何、NPC移动阻挡、发射器、大招、充能和生命周期配置测试；仅编译，未执行游戏测试。
- 验证：ExcelExporter、Proto2CS成功；dotnet build ET.sln通过，0错误、14个已有HybridCLR警告，153.64秒。日志Temp/BrawlCollisionRules-export.log、Temp/BrawlCollisionRules-proto.log、Temp/BrawlCollisionRules-build.log。Unity Refresh请求超时，但随后工程引用已自动生成、Ping非Play/非编译、Console错误查询为空。没有手改csproj/meta，没有启动服务器或Play。

手测顺序：
1. 重启新服务器与客户端，分别用雪莉/柯尔特普攻打两种墙，弹丸被挡且不扣墙血；命中敌方作战单位扣血、消失并回充。
2. 充满大招后释放：雪莉5枚扇形45度、柯尔特12枚连发。击碎木桶/可摧毁墙后同一枚弹丸继续飞行，经过敌人可继续命中，碰到地形墙停止。
3. 检查死亡墙体立即解除移动阻挡，模型仍按DeathDuration稍后移除。复活后的阵营、标签、移动阻挡和弹药/充能规则应保持正常。
4. 空地发射应在射程终点消失；无新增特效。草清除本轮需看客户端ClearedGrass/通知或调试服务器集合，画面草仍保留。

## 最新增量：柯尔特大招释放（2026-10-15）

- 用户已修改源表，本轮通过ExcelExporter正式生成，未改源表数值。40102技能：左轮发射器2、单轮12发、间隔0.08333秒、射程12米、矩形宽1米、CooldownTime=1秒、AmmoCapacity=1；弹丸半径0.06、速度10、基础伤害320/成长32、BlockMask7、TargetMask5、MaxHitCount1、SuperChargeOnHit0.1。
- 复用BrawlRevolverEmitter/SkillSequence，不新增发射器。左右边缘弹丸中心偏移±0.44米，方向锁定、发射位置跟随英雄；最后一发计划约917ms，50ms服务器Tick约950ms发出。冷却从释放开始到1秒，连发未结束时不能开始任何另一轮。
- 普攻/大招请求统一为C2G_BrawlCastSkill，Slot=1普攻/2大招，包含EntryId/Sequence/X/Z。服务器从当前英雄NormalAttackSkillId/SuperSkillId选择配置，不接受客户端任意技能ID。旧NormalAttack消息及Handler重命名，移动原meta保留GUID；需同时重启新客户端/服务器。
- SkillCastComponent分别保存NextNormalCastAt/NextSuperCastAt，冷却不互相覆盖；仍共享一条正在执行的发射计划，不中断或叠加另一技能。雪莉普攻原无CD规则不变；大招按自身CooldownTime。
- BrawlSkillCastHelper.TryStart统一检查状态、方向、槽位、连发/冷却和发射器参数后才扣资源。普攻扣Ammo，大招扣SuperAmmo；每轮12发只扣1，1.4→0.4保留小数。未充满1、正在连发、冷却、受控或非法配置均拒绝且不扣费；没有排队。
- 连发期间沿用暂停普通弹药装填的规则，结束后按原小数进度恢复；大招命中回充和自动充能不暂停。大招弹丸有效扣血仍按配置每目标回充0.1，未更改上一轮NPC贡献规则。
- 大招有效目标包含可摧毁墙/木桶。每枚命中墙或敌人仍销毁，不穿透；首枚扣血致死后，墙立即退出阻挡查询，后续弹丸能继续通过。不可摧毁地形墙只阻挡，不扣血或充能。
- 大招摇杆沿用普攻手势（点击当前移动方向、拖出死区瞄准、拖回取消），客户端按槽位发包。柯尔特按钮显示储量，去掉“释放未实现”；雪莉大招EmitterType草稿仍不支持，不扣储量。没有新增动画/特效/瞄准mesh，继续Sphere同步。
- 新增Statesync_BrawlColtSuper_Test：不足1拒绝、1.4消耗后0.4/普攻不扣、重复请求拒绝、独立冷却、12枚偏移/伤害/回充配置、墙致死后弹丸停止与回充、后续弹丸穿过死墙、非法槽位。只编译不运行。
- 手测：柯尔特普攻积攒到1后拖动大招释放12枚；移动中发射方向锁定、位置跟随；大招命中敌人/可摧毁墙会回充，墙0.2秒后移除且死亡立即不阻挡。充能不足和连发期间再次按大招不应额外发射。回归普攻弹药、摇杆与雪莉5发。
- 最终验证：dotnet build ET.sln通过，0错误/0警告，25.98秒，Temp/BrawlColtSuper-build-final.log；Unity错误日志为空。首次构建的测试墙体组件初始化错误已改用NPCBlockHelper.Attach修复。配置/协议正式生成，Handler移动保留meta；测试只编译未运行，未启动服务器或Play。

## 最新增量：超级技能充能（2026-10-15）

- 用户确认弹丸独立配置SuperChargeOnHit，不按伤害量计算；与发射技能类型无关，未来大招弹丸同样能回充。初始大招为0、死亡保留。英雄包括主角/Bot，NPC不实现普攻/大招系统；其未来多个技能与AI选择独立设计。
- 用户补充：英雄攻击劫匪/木桩等NPC可以充能，未来通过NPC额外属性区分宝箱等目标；本版先允许有效扣血的敌方NPC贡献充能，包括被TargetMask选中的可伤害障碍。无敌、死亡/出生状态、友方、未实际扣血不充能；雪莉当前墙仅阻挡不扣血，仍不充能。
- BrawlSkill.xlsx弹丸分页R列新增float SuperChargeOnHit，单位大招格数/每枚对每个有效目标命中。10101与40101暂填0.1供测试，其余行0；用户可自行调表。技能分页雪莉大招10102的AmmoCapacity原空值补为1，柯尔特40102已有1。其它大招发射参数不补齐，释放仍未实现。源表备份Temp/BrawlSkill.before-super-charge.xlsx，已通过ExcelExporter导出。
- PropertyId.MaxSuperCharge→MaxSuperAmmo=800（int），SuperCharge→SuperAmmo=810（float），原编号不变；自动恢复四项820/830/840/850保留，单位格/秒。英雄大招容量读Hero.SuperSkillId指向技能的AmmoCapacity，支持容量2及更大整数，没有硬编码上限2。SuperAmmo可为1.6等小数，超过容量截断，不保存隐藏溢出。
- BrawlSuperChargeComponent仅挂英雄，记录自动充能积分时刻；默认BaseSuperChargeRegenRate=0，不用大招ReloadTime自动装填。未来Buff可改现有恢复速率属性。自动充能不因连发暂停，死亡/Spawning暂停。当前没有大招消耗/释放入口，不能通过按钮扣除充能或发射。
- 发射器将配置充能值写入BrawlProjectileLaunch，生成弹丸时复制到BrawlProjectile.SuperChargeOnHit。与EffectValue一样使用施放计划快照，既有弹丸不受之后改表/等级变化影响。
- 伤害结算返回实际扣血；两组HitEvent都执行后，只要本次实际扣血大于0就调用一次BrawlSuperChargeHelper.OnDamagingHit。数量取配置，不乘伤害，不受目标剩余HP比例影响；致命一击也给完整值。去重沿用碰撞HitUnits，同一弹丸同一单位只结算一次，穿透不同目标可分别贡献。
- 弹丸所有者以原OwnerUnitId查找；仅存活、非Spawning英雄可获充能。死亡或销毁后的在途弹丸仍可沿原伤害逻辑飞行，但不再充能，也不转交重生实体。NPC发出的弹丸不获得英雄大招资源。
- 死亡事件将SuperAmmo存到房间BrawlParticipant，旧实体销毁后SpawnHero恢复到新实体并按新容量截断。首次记录默认0。房间退出统一清理，无静态集合。HP、普通弹药的重生规则不变。
- UnitInfo/PropertyState新增MaxSuperAmmo、SuperAmmo、SuperChargeRegenRate，入场/重生名单/每Tick同步。大招按钮显示权威数值与“释放未实现”，没有本地充能预测、大招攻击/动画/特效。
- 新增Statesync_BrawlSuperCharge_Test：初始0、NPC贡献、双伤害事件只充一次、同弹丸去重、无敌/完全减伤、致命一击、配置0、技能类型无关、死亡/销毁后不充能、容量2截断、连发中自动充能及出生暂停。Lifecycle测试增加新实体恢复1.6/2；两种发射器测试检查充能配置快照。用例仅编译、不运行，游戏由用户验证。
- 手动验证：重启本轮客户端/服务器，雪莉或柯尔特入场大招0/1；保护结束后每枚对敌方Bot或劫匪实际扣血增加0.1，最多1。打墙或无敌目标不增加；角色死亡/重生保留储量。大招按钮仍不能释放。
- 最终编译：dotnet build ET.sln成功，0错误、14个原有HybridCLR警告，59.90秒，Temp/BrawlSuperCharge-build.log；配置/协议正式导出成功，业务及文档diff格式检查通过。新增测试只编译未执行，未启动服务器或Unity Play。

## 最新增量：弹药与装填（2026-10-15）

- 后续修正：用户实测发现2.x开火变1，原TryConsume的Math.Floor错误丢弃了小数进度；现改为ammo-1，2.4→1.4，连发中暂停、结束后接续原进度。旧的丢弃小数规则作废。没有独立攻击后装填延迟：柯尔特约0.8秒连发结束后恢复，ReloadTime=1.6秒补一整格；CooldownTime=0.8秒是独立的施放门禁，雪莉仍无CD。
- 此修复完整编译通过：0错误、0警告，26.90秒，日志Temp/BrawlAmmo-fraction-fix-build.log；新增2.4→1.4及暂停/恢复断言只编译未运行。

- 用户确认：首次进场满弹；每次重生初始化为容量的1/3，保留小数。雪莉/柯尔特容量3时为1格，容量1时规则为约0.333格（卡尔技能仍未实现，不声称已支持卡尔）。英雄2秒Spawning期间不装填，出生完成后开始恢复；死亡期间不装填。
- PropertyComponent仍由创建流程显式赋初值，未给每个属性增加默认比例或重生策略。弹药初始化单独由BrawlAmmoHelper负责；BrawlLifecycleHelper通过房间参与者原UnitId区分首次与重生，再传给UnitFactory。
- 容量取普攻技能AmmoCapacity，装填一格时长取ReloadTime。初始化PropertyId.MaxAmmo/Ammo/BaseReloadRate；BaseReloadRate=1/ReloadTime，单位格/秒，内部秒配置转毫秒以匹配服务器时钟，ReloadTime=0不自动装填。ReloadRate继续经过属性修正项计算，未来加速/减速可直接使用已有平值/百分比修正。
- 一次成功普攻消耗一格；雪莉5枚、柯尔特6枚均只扣一次。弹药小于1时拒绝，不排队；非法请求、受控/死亡/出生、连发未结束或冷却未到的拒绝不扣弹。连发被取消不退弹。
- Ammo用连续数值表示完整弹药与正在装填的一格：1.4代表1格可用+下一格40%。逐格恢复，不并行补空槽。再次开火仅减1，保留小数进度（2.4→1.4）；发射期间暂停，结束后从原进度继续装填。最后一发所在Tick不增加装填进度。
- 服务器BrawlAmmoComponent仅保存积分时刻，Ammo唯一数值仍在PropertyComponent。GetDouble提供内部精度读取，避免每步float转换累计误差；常规GetFloat与同步仍保留float。Tick按当前ReloadRate积分并截断到容量，边界清理微小舍入残差。
- 发射计划执行完毕或被控制打断时重置装填时钟，随后在既有服务器Tick恢复；眩晕/击飞本身不禁止装填，死亡/出生禁止。每次重生新实体初始化，旧进度/修正项不沿用。雪莉仍沿用此前无CD，柯尔特仍按配置0.8秒CD。
- UnitInfo与BrawlPropertyState新增MaxAmmo/Ammo/ReloadRate，初始/名单重建/每Tick均同步。客户端只接收结果，在普攻按钮显示两位小数的弹药/容量，不本地扣弹或推进装填，不禁用摇杆手势；是否可施放以服务器为准。没有扩展大招弹药或复杂弹药条表现。
- 新增Statesync_BrawlAmmo_Test覆盖逐格装填、空弹/半格拒绝、保留小数进度、容量上限、柯尔特结束后1.6秒补一格、出生死亡暂停、1/3初始化和装填加成；Revolver测试增加六发只扣一格断言。测试只编译不运行。
- 手动回归：首次显示3/3；雪莉连续使用3次后第四次不发射，1.5秒补一格；柯尔特六发合计扣1格，最后一发后1.6秒补一格；重生时显示1/3，出生2秒内不恢复，完成后继续装填。协议已通过Proto2CS导出，不手改生成代码。
- 最终编译：dotnet build ET.sln成功，0错误、14个原有HybridCLR编辑器警告，耗时2分43.94秒，日志Temp/BrawlAmmo-build.log；业务/文档diff格式检查通过。新增测试已编译未执行，未启动服务器或Play。用户需用本轮客户端/服务器重新进场验证。

## 最新增量：柯尔特普攻（2026-10-15）

- BrawlRevolverEmitter实现IBrawlSkillEmitter，由EmitterType.Revolver=2选择；算法只输出初始条件与相对发射延迟，不持有Entity、不处理网络。BrawlProjectileLaunch增加DelayMilliseconds，霰弹枪默认0，仍同时发射5枚。
- 当前40101配置已是每轮6枚、单轮、间隔0.16秒、CooldownTime=0.8秒；读取现有生成配置，无需修改源表。矩形完整长度9、宽度0.7、弹丸半径0.04：出生偏移依次左/右/左/右/左/右，中心横向±(宽度/2-半径)=±0.31，全部沿正前方平行飞行。支持配置改变数量/宽度/间隔，暂不支持多轮。
- 理论发射时刻为0/160/320/480/640/800ms。服务器50ms Tick执行绝对期限，不累计逐发取整；通常落在0/200/350/500/650/800ms。位置使用执行时英雄当前位置，方向使用本次施放的锁定XZ方向；不改变人物面向控制或Animator。
- 英雄/Bot工厂默认挂BrawlSkillCastComponent，保存纯值发射计划、索引、开始时刻、冷却期限、方向和阵营快照。BrawlSkillSequenceHelper执行计划，死亡/出生/眩晕/击飞时取消剩余发射；旧Unit销毁也销毁计划。已发出的弹丸不因取消而删除。
- 普攻入口先追赶已有Tick，再验证CanStart；连发尚未结束或未到冷却期限则忽略，不排队。当前仅左轮启用配置CooldownTime，从释放开始计时；雪莉维持此前无CD，不做弹药或装填，也不实现冷却UI。
- 每Tick先推进已有弹丸，再发射到期弹丸，避免新弹丸提前多走整步；出生立即检查重叠，后续沿用整段扫掠/伤害/阻挡。弹丸数值仍是发射器接收英雄等级计算的施放快照，柯尔特1级每枚360，成长36。连发使用既有Sphere和弹丸快照，无新协议。
- 新增Statesync_BrawlRevolver_Test，覆盖左右边缘/时序/成长、过窄范围拒绝、即时首发、锁定方向跟随位置、0.8秒完成、重复释放门禁及死亡取消；旧霰弹测试未实现类型改检查Boomerang。只编译不运行测试。
- 手动验证：选择柯尔特进练习赛，等出生结束，点普攻观察6枚左右交替；向北移动同时摇杆向东释放，整轮向东且后续出生点随移动；连点不会叠加未完成连发，打墙/敌人沿用碰撞扣血；再选择雪莉回归同时5发。大招/AI/动画仍未实现。
- 验证结果：用户退出Play并停服后，dotnet build ET.sln通过，0错误、14个原有HybridCLR编辑器警告，耗时1分32.42秒；日志Temp/BrawlRevolver-build.log。Unity自动生成新增脚本引用，未手改csproj/meta；测试仅编译，未运行游戏或自动化用例。

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
