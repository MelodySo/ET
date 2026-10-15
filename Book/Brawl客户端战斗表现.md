# Brawl 客户端战斗表现

更新日期：2026-10-15。本文记录已确认方案、待讨论项与实施顺序；第四节另记录本轮已完成的 Animator 资源，运行时表现代码尚未接入。

## 当前工作限制与协作边界

- 最新授权：可以修改资源、文档/Markdown，但仍不修改代码和配置。此次只创建自有 Animator/Override/Mask/槽位 Clip，并替换 `PBRDefault` 角色 Prefab 的 Animator Controller 引用；用户明确排除 `PBRMaskTint`。
- 不进行配置/协议导出、编译或 Play 游戏测试。此次允许通过 UnityBridge 定向导入和只读检查这些资源，`.meta` 由 Unity 自动生成。
- 用户正在让 dot 实现公牛 AI 与寻路，不介入或覆盖这部分工作，不主动给另一会话发送指令。
- 本文的运行时代办不构成实施授权；解除代码限制后仍需检查最新代码及其他会话改动。
- 摄像机、既有弹丸飞行/命中/消失特效等继续记录在 [Brawl开发交接.md](Brawl开发交接.md)，不搬迁、不重写。本文只说明新的角色/技能表现如何衔接。

## 一、资源核查记录与固定武器原则

以下来自本轮之前的只读文件核查，未在 Unity 中实际预览所有动画；不保证每段动作的握持、穿插、脚滑效果已验证。

- 雪莉模型：`Packages/cn.etetet.brawl/Bundles/Prefabs/Character/PBRDefault/BasicFemaleDefault.prefab`。
- 核查时绑定 `PistolAnimator`，启用 `PistolDefault`；预挂的 `ShotgunDefault` 等其他武器关闭。
- `PistolAnimator`、`ShotGunAnimator` 均为单 Base Layer，无 AvatarMask、无 BlendTree；状态转换主要依赖退出时间，属于展示式组织，不作为最终战斗控制器直接使用。
- 原 Prefab 开启 Root Motion，但当前运行时加载代码会关闭。战斗表现继续关闭 Root Motion，动画不得自行修改权威位移。
- `HandGunStance` 有普通/战斗待机、前后左右跑动、单发/自动射击、换弹、受击、眩晕、死亡、击倒/起身、跳跃、冲刺、翻滚、胜利等动画；移动有带 RM 与非 RM 版本，优先使用非 RM。
- 抽查单发射击 Clip 的 Loop Time 开启，死亡关闭；实施时必须逐一核对循环和退出规则，不因为名称为 SingleShot 就假设只播一遍。
- `ShotGunAnimator` 引用 `Rifle02Stance`，包含 `ShootShotgun_RF02_Anim`；目前不需要另购动画才能做首版。

已确认的通用原则：角色当前拿什么武器，就先固定使用该武器及对应动作；不实现装备、运行时换武器或动态武器选择系统。

武器选择有一处需在实施前确认：用户同时认可“雪莉使用现成霰弹枪＋Rifle02Stance”的候选方案，而核查时实际启用的是手枪。保留这两条记录；后续确认是否对雪莉进行一次固定资源调整，不擅自切换，也不扩展成换武器系统。

## 二、已确认的客户端职责

- 客户端只解释服务器结果，不能建立另一套独立战斗判定。
- 位置、血量、能否移动/攻击、死亡与重生由服务器决定；不能由动画结束、AnimationEvent 或客户端计时器触发权威结算。
- 暂不加入本地移动预测。
- 状态与瞬时动作分开处理，避免把“移动中射击”建成与移动互斥的唯一状态。

| 维度 | 表现内容 | 关系 |
| --- | --- | --- |
| 生命周期 | 出生中、存活、死亡 | 死亡终止普通动作 |
| 移动 | 待机、移动 | 可与射击同时存在 |
| 动作 | 开火、换弹、受击 | 依据对应事件及规则播放 |
| 全身接管 | 眩晕、击飞、击倒等 | 接管时停止不允许并行的普通动作 |
| 附加效果 | 无敌等 | 优先颜色/闪烁/特效，不必独占动画状态 |

客户端表现状态由每个单位/视图持有，不建立全局可变静态状态。具体组件和类划分等解除代码限制后再结合最新 ET 结构确定。

## 三、移动与朝向规则

### 移动表现

- 移动方向、人物面向、技能瞄准/施放方向是三个独立变量，不能混用。
- 按人物面向，把世界移动方向转换为相对角色的前后左右，驱动四向 BlendTree。
- 使用实际位移速度判断移动与动画速度，不只看输入意图；顶墙时有输入但可能没有位移。
- 在与位置插值一致的时间线上取得表现速度；首次加载、重生、位置重置等需重置历史采样，避免错误的大速度。
- 停止后保留面向，方向变化平滑呈现；平滑只影响视觉，不得改变服务器施放方向或额外延长技能锁向。

### 用户明确确认的面向行为

| 场景 | 规则 |
| --- | --- |
| 正常移动、没有技能锁向 | 面向移动方向 |
| 正常停止 | 保留最后面向 |
| 按住/拖动技能摇杆准备瞄准 | 只更新指示器，不因瞄准自动转身 |
| 服务器确认技能实际成功使用 | 此时才按施放方向改变面向；被拒绝的请求不作为成功使用 |
| 雪莉瞬发普攻，角色正在移动 | 成功释放时改变一次面向，随后立即恢复移动方向；不为整段射击动画额外锁向 |
| 雪莉瞬发普攻，角色静止 | 成功释放后保持施放方向，直到后续移动或技能改变它 |
| 连发技能 | 发射期间保持施放方向，移动用侧跑/后退适配；结束或中断解除锁向，移动则恢复移动方向，否则保留最后面向 |

上述“立即恢复”是用户确认的规则，不可擅自改为播放完攻击动画才恢复。单次转向可能短于一帧，实际枪口/上半身如何视觉表达仍需讨论；必要时独立处理上半身姿态，不能因此修改权威弹道或人物面向规则。

连发锁向的开始、结束、中断事件及具体时间点需在攻击表现通知设计时细化，不能只用动画长度猜测。

## 四、Animator 组织

- 创建自己的战斗 Animator，复用购买的 Clip，不破坏资源包展示控制器。
- 基础层：战斗待机与四向移动。
- 上半身覆盖层：射击、必要的换弹，使用 AvatarMask＋Override。
- 死亡、击倒等全身动作接管时关闭普通上半身攻击层；具体受击是否打断射击需后续细化。
- 不直接把现成的全身射击 Clip 当作 Additive 动画使用。
- Mask 只限制骨骼影响范围，不负责自动瞄准任意世界方向；瞄准与面向控制独立处理。
- 核对循环、过渡、模型朝前方向、武器握持位置、枪口挂点。只使用现有玩法所需动作，不因资源里有冲刺/跳跃就新增玩法。
- 模型加载完成后绑定 Animator；没有 Animator 的墙、木桶等正常跳过。销毁、重生、更换视图后清理旧动作和事件引用。
- 起步、停步、滑墙与侧跑需检验脚滑；本阶段不引入完整 IK/动作匹配系统。

参考：[Unity 2022.3 Animation Layers](https://docs.unity3d.com/2022.3/Documentation/Manual/AnimationLayers.html)。

### 4.1 本轮已创建的资源

目录：`Packages/cn.etetet.brawl/Assets/Animation/BrawlCharacter/`。

- `BrawlCharacter.controller`：共享状态机，只引用独立槽位 Clip，不直接绑定某个英雄的购买动画。
- `BrawlCharacterDefault.overrideController`：Prefab 使用的默认 Override，24 个槽位均填入有效的 `HandGunStance` 动画。
- `BrawlUpperBody.mask`：Humanoid 上半身 Mask，启用躯干、头、双臂与手指；关闭 Root、双腿与手脚 IK。
- `Slots/BrawlSlot_*.anim`：24 个独立占位 Clip。它们本身无动画曲线，实际姿态由 Override 提供。不要直接把基础 controller 挂到模型上，也不要删掉这些作为替换键的资源。
- `Bundles/Prefabs/Character/PBRDefault/` 下全部 21 个角色改为引用默认 Override；`PBRMaskTint`、购买包展示控制器、源 FBX、武器启用状态、Avatar、材质等不改。

默认动画沿用核查时雪莉当前启用的手枪套装，便于统一编辑；不是确认所有角色最终都使用手枪。雪莉是否固定改成霰弹枪仍按第一节记录处理，本轮没有切换武器。其他角色在运行时代码接入前也会使用这套默认动作，近战/双手武器的握持尚未校对。

独立槽位的原因：AnimatorOverrideController 按原始 Clip 替换，而非按状态名字替换。即使普通攻击、专属技能临时播放同一段动画，也需要不同的原始槽位，才能在以后独立替换。

### 4.2 分层、参数与状态约定

| 层索引 / 名称 | 默认权重 | 内容与调用约定 |
| --- | --- | --- |
| 0 / `Locomotion` | 1 | 默认 `Move` 状态，2D Simple Directional BlendTree：中心待机，前后左右跑动；全身基础姿态。 |
| 1 / `UpperBody` | 0 | Override＋上半身 Mask；默认 `Empty`。代码开启权重后，可触发同名 Trigger 或显式切换动作。 |
| 2 / `FullBody` | 0 | Override，无 Mask，覆盖全身；默认 `Empty`。没有 AnyState 自动进入条件，由表现代码显式切换。 |

参数：

| 参数 | 类型 / 默认值 | 含义 |
| --- | --- | --- |
| `MoveX`、`MoveZ` | Float / 0 | 相对角色朝向的实际表现移动方向，左右对应 X，前后对应 Z。静止为 `(0,0)`，正常移动通常使用归一化方向；不是直接传世界速度米/秒。 |
| `LocomotionSpeed` | Float / 1 | 移动状态播放倍率。停步用 MoveX/Z 归零，不要把倍率设为 0 冻结待机。 |
| `UpperActionSpeed` | Float / 1 | 当前上半身动作播放倍率。 |
| `FullBodyActionSpeed` | Float / 1 | 当前全身动作播放倍率。 |
| 六个 `Upper...` 同名参数 | Trigger | 对应下表上半身状态；允许再次触发同一状态，用于后续逐发事件。 |

动作状态与默认映射如下；原始替换键为 `BrawlSlot_` 加状态名，移动键见表首行。实际源文件均位于 `Animation/HandGunStance/`，文件名后缀为 `_HG01_Anim.fbx`。

| 层 / 槽位 | 默认 Clip 名称前缀 | 结束方式 |
| --- | --- | --- |
| Locomotion：`Idle`、`MoveForward`、`MoveBackward`、`MoveLeft`、`MoveRight` | IdleBattle01、RunFWD、RunBWD、RunLeft、RunRight | BlendTree，由 MoveX/Z 驱动；移动使用非 RM 版本。 |
| UpperBody：`UpperNormalAttack` | ShootSingleshot | 一周期后返回 Empty。 |
| UpperBody：`UpperSuperAttack` | ShootAutoshot | 一周期后返回 Empty；只是默认占位，并非所有大招都是连射。 |
| UpperBody：`UpperReload`、`UpperHit` | Reloading、GetHit01 | 一周期后返回 Empty。 |
| UpperBody：`UpperSpecial01`、`UpperSpecial02` | ShootSingleshot、GetHit02 | 预留槽位，一周期后返回 Empty。 |
| FullBody：`FullDeath`、`FullStunned`、`FullAirborne`、`FullKnockDown` | Die、Dizzy、JumpAir、KnockDown | 不自动离开；只按服务器状态由表现代码切换。 |
| FullBody：`FullGetUp`、`FullSpawn`、`FullVictory` | GetUp、PowerUp、Victory | 一周期后返回 Empty，仅表示视觉动作播完，不产生复活等业务结果。 |
| FullBody：`FullCommon01`、`FullCommon02`、`FullCommon03` | DashFWD、RollFWD、Jump | 三个通用全身动作预留槽，图中独立摆放；一周期后返回 Empty。 |
| FullBody：`FullHero01`、`FullHero02`、`FullHero03` | PowerUp、ShootSingleshot、Victory | 三个角色专属全身动作预留槽，图中独立摆放；一周期后返回 Empty。 |

上半身进入和自动退出的过渡为 0.05 秒，自动退出使用归一化 Exit Time = 1。原始射击 Clip 即使开启循环，也只在这个状态中播放一周期再退出；没有改 FBX 导入设置。未来持续射击、持续施法需依据实际阶段设计，不能从这个默认占位推断服务器技能时长。

全身动作的完整路径示例为 `FullBody.FullDeath`、`FullBody.FullHero01`；后续可使用 `CrossFadeInFixedTime` / `Play` 并明确层索引。Death/Stunned 等保留状态没有退出箭头是刻意设计，不是漏接线。所有状态使用 Write Defaults = Off，没有 StateMachineBehaviour 或 AnimationEvent 业务脚本。

### 4.3 后续代码接入必须遵守的边界

1. 每个角色视图持有独立的 AnimatorOverrideController 实例，基于共享基础 controller 复制默认映射，再按槽位批量替换该角色的 Clip。不要直接修改项目中的共享默认 Override，也不要把多个角色的可变覆盖表共用成一个实例；销毁视图时释放自己创建的运行时实例。
2. 替换键始终是 `Slots/` 的原始占位 Clip，不是已经覆盖后的购买动画名字。全套映射确定后优先批量 ApplyOverrides，再赋给该角色 Animator；运行时加载方式、按英雄绑定的映射资源留待下一轮设计，本轮不新增配置字段或角色映射代码。
3. `UpperBody` / `FullBody` 默认权重为 0；仅触发 Trigger 或切换状态不会自动开启权重。动作开始、取消、完成、死亡、重生时由表现组件管理权重。进入全身接管前清理/关闭上半身层；离开后将覆盖层切回 Empty、权重归零，并清理过期 Trigger，防止旧射击恢复。
4. Empty 无 Motion，且 Write Defaults 关闭；不能依赖切到 Empty 自动恢复所有骨骼，必须按上一条清理覆盖层权重。层间淡入淡出仍由后续代码实现，本轮资源不提供自动业务优先级。
5. MoveX/Z 应从当前显示位置的实际位移计算，保留服务器权威与既有插值；不因创建 Animator 就改移动速度、碰撞或朝向规则。Animator 是否启用、参数更新、动作时间线和复活重置均未在本轮接入。
6. 继续确保运行时 `applyRootMotion = false`。本轮只改 Prefab 的 Controller 字段，未更改原 Prefab 的 Root Motion 标志；现有加载代码的关闭行为需保留。
7. 上半身 Mask 目前面向这些 Humanoid 角色，不等同于支持任意 Generic 骨架。将来换用非 Humanoid 角色需另行检查 Avatar/骨骼遮罩。无 Animator 的墙桶依旧无需使用这个控制器。
8. 预留跳跃/冲刺/全身技能槽位仅为资源扩展点，不意味着新增对应玩法。真正的技能成功事件、连发节奏、受击打断、死亡等仍按第五节待讨论事项接入。

### 4.4 本轮验证范围

- UnityBridge 定向导入成功：基础 controller、默认 override、AvatarMask、24 个槽位 Clip，以及 21 个 Default Prefab；新资源和目录的 `.meta` 完整，由 Unity 生成。
- 从 Unity 序列化对象读取确认：3 层、11 个参数，层权重为 1/0/0，上半身 Mask 引用正确；24 组 Override 的 48 个 Clip 引用全部有效。
- 逐个从 Unity 读取 21 个 Default Prefab 的 Animator Controller，全部指向 `BrawlCharacterDefault`。Git 差异中每个 Prefab 只有 Controller 一行替换。
- 基础 controller 静态检查：48 个序列化对象无重复 ID、无失效内部引用；24 个原始槽位均独立。
- `PBRMaskTint` 的 21 个 Prefab 与 21 个 `.meta` 共 42 个文件，与修改前 SHA256 完全一致。
- 未编译、未进入 Play、未测试联网战斗和动画实际观感。以上是资源导入/引用/结构验证，不代表已经完成运行时动画驱动或所有英雄握持校对。

## 五、攻击表现通知——方向认可，细节待讨论

核查时已有技能释放请求和弹丸快照，缺少独立的成功施法表现通知。实施前必须重新检查最新协议，不重复造已有接口。

- 建议轻量事件包含：施法标识、单位 ID、技能、服务器 Tick/时间、施放方向、动作阶段等必要信息；这些是候选信息，不是冻结的协议字段。
- 雪莉一轮多颗弹丸只播放一次开火动作，不能按弹丸数量触发攻击动画。
- 柯尔特区分一次技能、逐发节奏及整个连发窗口。
- 被服务器拒绝的释放不播放成功动作；中断、死亡和重生后旧事件不得作用到新实体。
- 事件需要去重、处理延迟到达，并与既有位置/弹丸插值时间线对齐；不能直接使用最新状态让人物动作领先于显示中的位置。
- AnimationEvent 可触发声音、枪口火光等本地效果，不负责服务器子弹创建或伤害结算。

尚需讨论：事件类型/字段、传递通道、逐发还是整段动画、动作中断优先级、晚到事件取舍、以及瞬发转向后立即恢复时的上半身姿态。不得把这一节当作完整协议定稿。

## 六、技能瞄准指示器——首版方向认可

- 复用现有 `IBrawlSkillAimPreview.ShowOrUpdate/Hide` 和 `BrawlSkillAimPreviewData`，不重写摇杆。
- 雪莉使用扇形，柯尔特使用直线/条带；其他技能按后续实际类型扩展。
- 射程、角度、宽度必须对应技能与发射器规则；实施前核实已有 SkillRange/SkillCastRange 参数含义，不另写一套矛盾数值。
- 指示器立即响应本地输入，原点跟随当前显示位置，而非跳到尚未插值到的服务器位置。
- 松手、取消、死亡、失焦、退出时隐藏并释放相关资源。
- 当前是按住瞄准，不是真正的蓄力技能；不新增蓄力伤害、蓄力同步或蓄力业务。
- 首版展示完整配置范围，不做遇墙截断。后续若做遮挡预览，需衔接共享几何与碰撞规则，不能直接依赖 Unity Physics 作为真实战斗判定。

借鉴资源：`Assets/WalldoffStudios/AimIndicators/Scripts/Indicators`。

- 可借鉴动态网格、材质参数、颜色与填充效果，使用我们自己的类，不引用 WalldoffStudios 运行时代码或控制器。
- 原包支持扇形、直线、抛物线、目标圈；首版只实现当前所需形状。
- 原包的障碍截断使用 Physics/Physics2D，不直接复制进现有权威碰撞流程。
- 核查的扇形/直线 Shader 使用 UnityCG.cginc；后续实现需检查并适配 URP、深度、透明排序和贴地高度。
- 贴地效果先针对当前平面关卡，不承诺支持复杂高低地形投影。

## 七、明确后置的反馈

以下方向认可，但不是第一阶段任务：

- 角色头顶血条、弹药、大招充能。
- 实际受伤反馈与飘字；命中/接触不等于成功扣血，尤其是无敌目标。
- 出生、死亡、复活、无敌、受控的进一步特效和音效。
- 枪口火光、开火声音、换弹声音。
- NPC 受击、销毁表现。

换弹表现需单独设计：当前自动逐格装填，不能每一点弹药增长都触发完整换弹动画。表现也不能反过来控制资源恢复。

基础死亡动作属于角色首版状态接入；上述后置项指更完整的反馈，并不要求把最基本的死亡状态一起推迟。

## 八、后续建议顺序与验收重点

以下运行时代码仍仅为排期建议，代码限制解除前不得执行；共享 Animator 资源骨架已按本轮单独授权创建：

1. 确认雪莉最终固定武器；在已创建的 Animator/Mask/槽位基础上补全每个英雄的运行时动画映射。
2. 雪莉待机、四向移动、普通朝向、基础死亡动作，保留已有服务器权威位移。
3. 独立接入本地技能指示器，不以攻击协议尚未完成为由改写输入逻辑。
4. 讨论并定稿成功释放/逐发/结束/中断通知，再接上半身开火与技能朝向规则。
5. 扩展其他英雄及有 Animator 的 NPC；后续再做第七节反馈。

未来验收关注：顶墙不空跑、斜向/侧向移动过渡、移动中单发及连发、静止射击后保留朝向、瞄准不转身、死亡打断、重生不继承旧事件、无 Animator 的 NPC 正常加载、取消指示器及退出无残留。

## 文档变更记录

- 2026-10-15：首次整理用户确认的角色状态、动画分层、技能指示器和朝向规则；记录代码/配置暂停限制。
- 首次整理只创建本文档，未改代码、配置、资源或原交接文档；未编译、未运行测试。
- 2026-10-15 后续资源授权：新增三层 Animator、默认 Override、上半身 Mask、24 个独立槽位；只替换 21 个 PBRDefault Prefab 的 Controller 引用。代码/配置仍未改动，MaskTint 不在本轮范围内；未运行游戏测试。
