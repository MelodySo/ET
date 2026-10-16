# Brawl 客户端战斗表现

更新日期：2026-10-16。本文记录已确认方案、实施结果与待办。出生／攻击／死亡及施法通知见第十二节，移动速度校准见第十一节；前文保留历史记录。

## 当前工作限制与协作边界

- 最新授权：允许修改所需客户端代码、动画资源和本文档；此前“只讨论、不修改代码”的限制已解除。
- 已实现固定武器映射、独立运行时 Override、待机／四向移动与轻量表现状态；后续授权新增扇形、矩形和圆形施法前预览（第十节）；不修改服务端、协议、Excel、AI／寻路、预测与碰撞。
- 不启动服务器、不进入 Unity Play；实际观感由用户测试。PBRMaskTint、购买包控制器、原始 FBX 导入设置与武器启用状态保持不变。
- 另一个会话的公牛 AI 与寻路工作保持原样，不主动给另一会话发送指令。
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

历史候选记录（本轮已确定保留现有手枪，无需再次确认）：用户同时认可“雪莉使用现成霰弹枪＋Rifle02Stance”的候选方案，而核查时实际启用的是手枪。保留这两条记录；后续确认是否对雪莉进行一次固定资源调整，不擅自切换，也不扩展成换武器系统。

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

客户端表现状态由每个单位/视图持有，不建立全局可变静态状态。本轮的数据归属与代码位置见第九节。

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

以下为历史排期建议；本轮明确只执行待机／移动基础，实际范围以第九节为准：

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

## 九、客户端角色移动表现实现（2026-10-15）

### 本轮完成范围与接入位置

- 沿用现有 BrawlModelHelper / BrawlTrainingSession 资源加载、句柄缓存和清理流程；没有新增资源加载框架、Excel 字段或协议。
- ModelView 的 `BrawlCharacterAnimationView` 保存 Animator、模板、独立运行时 Override、速度采样、显示速度、面向和未来技能锁向标志。会话 `CharacterAnimations` 持有实例，练习赛 `BrawlPracticeUnitView.Animation` 引用该数据。
- HotfixView 的 `BrawlCharacterAnimationHelper` 执行绑定、重置、更新、释放。`BrawlModelHelper.InstantiateModel` 在加载后绑定，覆盖本地训练场静态待机与练习赛玩家／Bot。武器上的 Animator 不当作角色 Animator；无三层角色契约的墙、木桶等正常跳过，所有模型 Animator 仍关闭 Root Motion。
- 每个角色创建 `new AnimatorOverrideController(template.runtimeAnimatorController)`，从模板取得原始 BrawlSlot 键，再一次 `ApplyOverrides`。共享默认 Override 和角色模板均不在运行时修改，不每帧替换 Clip。
- `BrawlPracticeMovementViewHelper.Update` 在写入插值位置后采样世界显示位移／本帧 unscaled delta。顶墙、断流停在最后快照时，无位移就回待机；不使用摇杆或协议 MoveDirection 判断是否播放跑动。
- 正常移动时面向实际显示位移，停止保留最后朝向；以实际显示朝向的逆旋转把世界移动方向转为 MoveX/Z，保留模型原始旋转偏移。转身为纯视觉平滑，不能修改权威方向、位置、速度、碰撞。技能准备瞄准仍不调用转身。
- MoveX/Z 平滑时间 0.08 秒、转身速度 720 度／秒、静止阈值 0.02 米／秒，均为 Helper 内纯视觉调校常量。播放倍率按实际显示速度／角色配置 MoveSpeedTier 归一化，限制在 0.1–2；静止恢复倍率 1，避免冻结待机。此倍率不是动画测量出的真实步幅，脚滑仍需观感调校。
- 首次加载、出生隐藏／显示切换、重建视图、插值时间线追赶重置均清空采样。帧间隔超过 0.25 秒或单帧位移超过参考速度×0.25 秒时，按不连续位置重置处理，避免把跳点播放成奔跑。未来明确的位置重置入口应主动调用 Reset；当前无新增瞬移协议。
- Reset 明确归零两个动作层权重、清除全部旧 Trigger、恢复移动参数与动作倍率，并在 Animator 激活时切回 Move／Empty。Empty 无 Motion 和 Write Defaults Off 不承担姿态清理职责。
- 当前只播放 Locomotion；出生仍沿用服务器 Spawning 隐藏人物，死亡仍沿用服务器名单保留／移除模型。没有实现死亡、受控或攻击动画，也没有客户端战斗结果判定。
- `SetFacing` 只是未来成功施法表现事件的接入口；尚无调用方，不以请求、瞄准或弹丸数量推断成功动作。FacingLocked 与显示速度独立，后续连发结束／中断需由确认事件解除。
- 服务器移除角色、视图重建、加载取消、会话退出时释放运行时 Override；模型与资源句柄继续由原流程销毁／释放。

### 固定武器映射资源

- 编辑器入口：`Brawl/Animation/Build Character Locomotion Mappings`，代码在 `Packages/cn.etetet.brawl/Scripts/Editor/Share/BrawlCharacterAnimationEditor.cs`。
- 工具检查 PBRDefault 的实际启用对象，左右双持归同类；对应购买包原控制器读取 Clip，不按英雄名字选武器。多种不同武器同时启用会报错，要求人工确认。
- 模板位置：`Packages/cn.etetet.brawl/Assets/Animation/BrawlCharacter/Characters/<Prefab名>.overrideController`，各 Prefab 的角色 Animator 直接引用模板，YooAsset 随 Prefab 依赖加载，无新增收集规则。
- 本轮替换 `Idle / MoveForward / MoveBackward / MoveLeft / MoveRight` 五个槽位为相应套装的 `IdleBattle01 / RunFWD / RunBWD / RunLeft / RunRight`，均需为循环、非 RM Clip。
- 其余 19 个动作槽位仍保留默认编辑占位映射，层权重关闭，不能据此宣称所有英雄的攻击／死亡握持已经完成。后续动作接入前必须补充对应映射和退出规则。
- 雪莉 BasicFemaleDefault 保留 Pistol／HandGunStance；公牛 SalarymanDefault 使用现有 Shotgun／Rifle02Stance。未切换任何武器。
- 具体逐模型报告生成到 `Temp/BrawlCharacterAnimation-mappings.txt`，包含实际武器、源控制器和五个 Clip 文件名。

### 验证范围与剩余事项

- 编辑器映射菜单执行成功：21 个角色各有五个有效循环移动 Clip；每个模板保留 24 个独立 BrawlSlot 键；临时创建两个运行时 Override 并修改其中一个，确认另一实例与模板不受影响。报告为 `Temp/BrawlCharacterAnimation-mappings.txt`。
- 21 个 PBRDefault Prefab 最终均只有一行 Controller 引用变更；去除了 Unity SavePrefabAsset 产生的无关序列化升级。工具已改为仅替换控制器引用文本后由 Unity 导入。MaskTint、默认共享 Override、基础 controller、原始动画与武器状态没有修改。
- 新脚本及映射资源的 `.meta` 由 Unity 生成，工程引用已自动刷新，没有手改 `.csproj`；ModelView 只保存数据，HotfixView 执行表现，未新增包依赖或全局可变静态状态。异步加载后的组件从现有 EntityRef 重新获取，未新增跨 await 的 Entity 持有。
- `dotnet build ET.sln` 通过：0 错误、14 个已有 HybridCLR CS0649 警告，耗时 82.79 秒。日志 `Temp/BrawlCharacterAnimation-build.log`。`git diff --check` 通过；没有提交 Git。
- 编辑器中途退出后已重新打开；按用户要求未继续排查机器资源／崩溃原因。资源验证与编译均在重启后完成，没有启动服务器或进入 Play。
- 未运行服务器、Unity Play、联网战斗或自动化游戏用例。所有英雄握持、转身速度、脚滑、侧跑过渡仍需用户观察。
- 后续：成功施法通知、技能锁向时间线、攻击／装填／死亡／受控动作、瞄准指示器、音效及枪口特效按前文规则另行实现；不增加冲刺／跳跃玩法。

### Unity 手动验证

1. 完成编译后由用户启动现有服务器，从 Init 登录，选择雪莉进入服务器练习赛。出生显示后应播放手枪战斗待机；公牛 Bot 应采用霰弹枪待机／跑动套装。
2. WASD／移动摇杆分别直行、斜行、转弯、停止；人物应朝实际位移方向平滑转身，停下保留最后面向。转身期间可出现侧跑／后退混合，正常转向完成后回前跑。当前没有技能锁向，不能用技能拖动来测试持续侧跑。
3. 持续推向不可通过的墙：无实际位移时短暂过渡后待机；沿墙斜推发生滑动时按实际滑动方向移动。松手保持待机，不应原地持续跑。
4. 观察 Bot：其现有 AI 产生位移就跑、停止或顶墙不产生位移就待机。AI 决策和寻路问题仍由另一个会话处理；本轮动画不改变路径。
5. 拖动普攻／大招摇杆但不释放：人物不因瞄准自动转身。释放也尚无新增射击动画；保持本轮动作层权重 0。
6. 死亡并等待服务器复活：旧模型移除、新模型出生隐藏后出现，不能继承旧速度和 Trigger。退出再进、切换雪莉／其他英雄、本地训练场待机，检查武器套装及无残留模型／Override。
7. Play 时选中角色 Animator：Locomotion/UpperBody/FullBody 权重为 1/0/0，MoveX/Z 静止趋零，LocomotionSpeed 静止为 1，Apply Root Motion 为关闭；玩家和 Bot 的 controller 名含各自实例名与 Runtime。
## 十、施法前范围指示区域（2026-10-15）

本轮继续授权实现技能预览：扇形、矩形接现有技能，圆形实现绘制能力但不新增技能或修改配置。

- 参考资源位于 `Assets/WalldoffStudios/AimIndicators`。本轮借鉴平面形状、透明填充与轮廓思路，自行实现 Brawl Shader／表现代码，不引用 WalldoffStudios 的运行时类、Joystick、Controller 或 Physics 遮挡截断。
- `BrawlSkillRangeType.Rectangle=1`：Param1 为前向长度，Param2 为完整宽度，起点在人物脚下，左右各一半宽度。`Sector=2`：Param1 为半径，Param2 为完整夹角（度），方向位于角平分线。`Circle=3`：Param1 为半径，中心使用传入 Origin；本轮没有圆形技能、选点施法或投掷落点规则。
- 当 `SkillCastRangeType=0` 时读取 SkillRangeType／SkillRangeParam1/2，和当前雪莉／柯尔特发射器一致。独立施放范围类型1支持矩形，读取实际生成字段 SkillCastParam1/2；类型2在该枚举中是抛物线，不是扇形，本轮隐藏，不误画。其他未知、非法、非有限或非正尺寸也隐藏。
- 普攻／大招各持有一个 `BrawlSkillAimVisual`，会话管理四顶点 Mesh、MeshRenderer 和 MaterialPropertyBlock。只有形状／尺寸变化时重建四顶点承载面，Shader 裁出轮廓；拖动只更新世界位置与旋转，没有每帧新建 Mesh 或 Material。运行时 Mesh 随会话释放，材质随原模型资源句柄管理。
- `IBrawlSkillAimPreview` 沿用 ShowOrUpdate／Hide 入口，签名新增显式 BrawlTrainingSession 参数，与既有弹丸表现的无状态适配器模式一致。实现为 `BrawlSkillAimPreview` → `BrawlSkillAimVisualHelper`，ModelView 仅保存资源数据，HotfixView 处理逻辑。
- 加载入口仍为 BrawlTrainingHelper，通过 BrawlModelHelper／DefaultPackage 预加载 `BrawlSkillAimPreview` Prefab；路径为 `Packages/cn.etetet.brawl/Bundles/Prefabs/Presentation/BrawlSkillAimPreview.prefab`。材质在同目录，Shader 在 `Packages/cn.etetet.brawl/Assets/Shaders/BrawlSkillAimPreview.shader`。既有 BrawlPrefabs/CollectPrefab 收集器自动带上依赖，无新增配置、无 Shader.Find 打包依赖风险。
- 自有材质使用 URP Unlit、透明混合、ZWrite Off、ZTest LEqual、双面渲染，平面抬高0.04米减少地面闪烁。普攻浅蓝白，大招金色；材质默认边线0.06米、填充透明度0.14、边线透明度0.85，可在材质中调校。普通场景遮挡仍生效，不承诺穿透所有场景物体显示。
- 预览立即响应相机转换后的本地瞄准方向，原点每帧使用插值后角色显示位置；不转人物、不发准备协议、不产生伤害或碰撞，不把预览可见当作服务器允许施法。
- 沿用既有手势：按住并拖出死区显示，回死区／松手／取消／失焦隐藏；没有拖出死区的点按仍按既有移动方向释放，不额外显示常驻范围。支持普攻／大招独立实例。
- 接收到当前英雄 Dead／Spawning、模型隐藏或 MyUnitId 更换时取消旧手势和预览，旧实体的拖动不会在新实体上继续释放；退出时释放资源。本轮只依据已收到的状态清理界面，服务器仍最终验证技能。
- 完整显示配置范围，不做遇墙截断，不缩短半径抵消弹丸尺寸。不创建 Collider、不做射线碰撞，也未改变权威移动、弹道、AI、寻路和其他会话内容。

验证：Unity 资源菜单成功并输出 BRAWL_SKILL_AIM_ASSET_PASS，URP Shader 无导入错误，材质／Prefab 引用和无 Collider 契约检查通过；新增 meta 与工程引用由 Unity 生成。dotnet build ET.sln 成功，0 错误、14 个已有 HybridCLR CS0649 警告，耗时65.07秒，日志 Temp/BrawlSkillAim-build.log。git diff --check 通过。没有启动服务器、进入 Unity Play 或执行联网游戏测试，三种形状的实际画面尚未实测。

用户手测：
1. 雪莉普攻／大招拖出死区：看到不同配置角度的扇形；旋转拖动方向时指示区域即时跟随，人物不因准备瞄准转身。
2. 柯尔特普攻／大招拖出死区：显示对应长度、宽度的矩形；前端随配置范围变化，不能把宽度误当半宽。
3. 一边移动一边瞄准，原点应跟着画面人物，不跳到最新服务器位置；回死区、松手和切到其他窗口都隐藏。
4. 拖动时被击杀、等待重生、退出重进，检查没有残留范围和旧拖动。墙前瞄准不做范围截断；场景遮挡遵循材质深度测试。
5. 圆形暂时没有业务入口，不为了验证改 Excel 或新增技能。未来 SkillRangeType=3 且 SkillCastRangeType=0 时，按 SkillRangeParam1 半径绘制原点圆；目标中心选择规则需随对应技能另行接入。
## 十一、按动画原生步幅匹配移动速度（2026-10-16）

本节覆盖第九节的“非RM移动Clip、实际速度除以MoveSpeedTier、倍率限制0.1–2”方案。用户反馈滑步后，改用动画根位移速度作为倍率分母。

- 移动层已有 `LocomotionSpeed`，绑定到 `Locomotion.Move` 的状态速度参数；不修改全局 Animator.speed，也不改变攻击层速度。
- 非RM四向动画的 AnimationClip.averageSpeed 为0；对应RM版本有有效根位移速度，周期与原非RM动作相同、均循环。21个自有角色Override的四个移动槽位改为相应RM版本，Idle与其余预留动作不变；购买包FBX／导入设置不改。
- 游戏运行时仍始终 `applyRootMotion=false`。采用RM版本是为了读取原始步幅速度并播放对应动作，角色位置仍由现有服务器快照插值驱动，没有移动预测或动画位移反馈。
- `n` 仍为插值后实际显示位移／帧时间，单位米／秒；`m` 来自四向移动Clip的原始平均根位移速度，经当前移动Clip权重混合、Avatar.humanScale和实例Transform缩放换算为水平米／秒；`LocomotionSpeed=n/m`。不再用英雄配置移速当分母，也不再限制为0.1–2。
- 从Animator读取的是Clip混合权重，不使用已经受播放倍率影响的Animator平均速度作分母，避免反馈振荡。Idle不参与移动权重归一化，避免起步时移动权重小导致除数接近0；尚未有有效Clip权重时按当前局部移动方向混合四向速度。静止倍率恢复1，MoveX/Z仍平滑归零。
- BrawlCharacterAnimationView保存四向原始速度、复用的AnimatorClipInfo列表及NativeMoveSpeed／PlaybackSpeed。旧ReferenceMoveSpeed改名PositionResetSpeed，仅保留位置跳变判断用途，不参与播放倍率。
- 编辑器菜单 `Brawl/Animation/Inspect Motion Speeds` 输出 `Temp/BrawlAnimation-motion-speeds.txt`；`Verify Root Motion Scale` 在临时预览场景中手动求值，不进入Play、不修改场景实例，输出 `Temp/BrawlAnimation-motion-scale.txt`。
- 实测雪莉 BasicFemaleDefault 与公牛 SalarymanDefault 前跑：Clip速度约7.9009，Avatar.humanScale约0.5142、实例缩放1，实际根位移采样约4.0650米／秒。不能直接把7.9009当该模型的米／秒。该值不是代码常量，其他角色与方向从各自资源读取。
- 起停混合、急转身、重定向脚型仍可能有残余脚滑；此方案匹配周期平均步幅，不等于脚部IK或逐帧足底锁定。

验证：21个角色的84个移动槽位均已切换为同套、同周期的循环RM动画。雪莉／公牛临时预览采样4.0650米／秒，按Clip×Avatar×Transform计算4.0626米／秒，误差约0.06%；关闭Root Motion后连续求值一周期，模型位置保持不变。没有进入Play或启动服务器。dotnet build ET.sln 本次未完整通过：dotnet.exe进程38792占用Bin/ET.Core.dll，复制重试10次后报MSB3027／MSB3021；共2个复制错误，24个警告（含重试与原HybridCLR警告），耗时40.30秒。日志Temp/BrawlAnimation-speed-build.log。未擅自停止服务器，需用户停止后补跑完整构建。游戏观感仍由用户验证：直线匀速时脚步应明显更贴地；滑墙减速时步频随实际速度降低；顶墙无位移时回待机；Bot复用同样规则。检查Animator的Apply Root Motion仍关闭，LocomotionSpeed随n/m变化，松手后为1。
## 十二、出生、攻击与死亡动作（2026-10-16）

### 实现与边界

- 未发现专用 Spawn/Appear/Birth 动画。21 个 PBRDefault 角色的 `FullSpawn` 使用其实际固定武器套装的 `IdleBattle01`，不把 PowerUp 强化动作当作出生。出生期间模型可见，业务上是否可攻击/移动仍以服务器状态为准。
- `FullDeath` 使用各武器套装的非循环 `Die`；死亡时关闭上半身层、清理所有旧 Trigger，全身层权重为 1，播完保持末帧。模型移除、复活时机仍由服务器 roster 决定，不用动画完成触发战斗结果。
- `UpperNormalAttack` 与 `UpperSuperAttack` 映射到实际武器源控制器的攻击 Clip。本阶段两槽位可以共用动作，但保持独立原始槽位；不是确认所有大招最终都用普通攻击动作。雪莉保持 Pistol/HandGun，公牛使用 Shotgun/Rifle02 的 ShootShotgun。
- 攻击播放入口为 `BrawlCharacterAnimationHelper.PlayAttack`：上半身 Mask 覆盖，下半身继续移动；每次重播一周期，回到 Empty 后显式关闭层权重。出生/死亡会清理攻击。已接入 G2C_BrawlSkillUse 权威施法通知；动画入口不从输入或弹丸数量推测施法成功。
- 用户本轮确认允许 UseSkill 类通知，新增 G2C_BrawlSkillUse / BrawlSkillUseEvent：EventId、CastId、UnitId、SkillId、Slot、Tick、方向、Sustained 与 Started/Fired/Completed/Interrupted 阶段。只扩展通知，不修改技能结算、AI、寻路或 Excel。
- 不接枪口特效、声音；角色 Animator 的 AnimationEvent 派发关闭，避免购买包事件意外触发反馈。现有弹丸特效未改。
- `AnimationLifeSamples` 将出生/死亡布尔状态按快照 Tick 缓冲，随 RenderTick 消费；新视图从初始 roster 恢复状态。现有 roster 移除仍立即执行，不新增尸体延迟保留机制。
- `ResetSampling` 只清理移动采样，不再因追帧/位置跳变重播死亡或清除动作。`Reset` 用于完整视图初始化；运行时 Override 随视图释放。

### 编辑器入口与资源位置

入口均在 Unity 顶部 `Brawl` 菜单，不是独立运行时界面：

| 菜单 | 用途 |
| --- | --- |
| Animation / Build Character Action Mappings | 按 Prefab 实际启用武器，生成/更新 21 个角色的移动、出生、攻击、死亡映射；旧 Build Character Locomotion Mappings 菜单保留为同一操作的兼容入口。 |
| Animation / Inspect Motion Speeds | 输出移动 Clip 原始根位移速度，报告 `Temp/BrawlAnimation-motion-speeds.txt`。 |
| Animation / Verify Root Motion Scale | 临时预览场景验证雪莉、公牛根位移缩放换算，不进入 Play；报告 `Temp/BrawlAnimation-motion-scale.txt`。 |
| Presentation / Build Skill Aim Assets | 生成自有技能指示器 Prefab/材质，通常不需要每次运行。 |

映射资源：`Packages/cn.etetet.brawl/Assets/Animation/BrawlCharacter/Characters/*.overrideController`。生成代码：`Packages/cn.etetet.brawl/Scripts/Editor/Share/BrawlCharacterAnimationEditor.cs`；报告 `Temp/BrawlCharacterAnimation-mappings.txt`。不修改 PBRMaskTint、购买包源控制器、FBX、武器开关或 Excel。

### 客户端代码关系

以下代码均位于 statesync 包的 Lobby 目录：

- `BrawlModelHelper`：通过现有资源加载链实例化模型，绑定角色 Animator。
- `BrawlUnitRosterViewHelper`：按服务器单位名单创建/移除视图，恢复生命周期状态；每个单位持有 `BrawlPracticeUnitView`。
- `BrawlPracticeMovementViewHelper`：接收服务器快照、缓冲生命周期状态、插值显示位置，在实际显示位移后更新移动动画。
- `BrawlCharacterAnimationView`（ModelView）：每角色独立 Override、速度采样、面向与动作状态的数据；没有战斗判定。
- `BrawlCharacterAnimationHelper`（HotfixView）：批量绑定映射、三层权重与 Trigger 清理、出生/死亡/攻击动作入口、移动速度 n/m 换算、销毁释放。
- 共享 `BrawlCharacter.controller` 管状态与层，各角色 Override 管 Clip，运行时为每个角色复制独立实例。移动方向、面向与瞄准方向保持分离。

### 手动验证

1. 正常启动客户端进入练习赛，确认出生阶段人物可见并保持当前武器待机，出生结束后能正常移动；动画不提前解除出生限制。
2. 让玩家和公牛分别死亡，确认全身倒下、没有继续跑动或上半身攻击，死亡末帧保持到服务器移除；重生后无旧死亡姿态残留。
3. 移动、停止、正面顶墙，确认移动倍率与待机切换没有回退；连续重进练习赛检查旧视图没有残留。
4. 服务器加载本次代码与协议后验证攻击：包括无弹药拒绝不播放、一次霰弹仅播放一次、移动中攻击下半身继续移动、死亡打断攻击、无枪口特效。

验证仅做编辑器资源生成、引用检查与编译；不由代理启动服务器或进入 Unity Play，实际观感由用户测试。

### 施法通知链路与最新授权

本小节覆盖前文“不修改服务器/协议”以及第五节“通知待设计”的本轮限制：用户确认需要 UseSkill 类服务器通知，现已实现最小事件链路。

- 服务端 `BrawlSkillCastHelper.TryStart` 校验/扣资源成功后，把 SkillId 传给现有 `BrawlSkillSequenceHelper`。`BrawlSkillUseNoticeHelper` 只旁路记录通知：成功开始、每轮实际发射、完成或中断。相同发射延迟的一轮弹丸仅发一个 Fired，既有弹丸数量、时序、碰撞、伤害不变。
- 事件缓存在现有房间组件，独立通过 `G2C_BrawlSkillUse` 发送；使用房间内单调 EventId 和 CastId。通知覆盖所有走公共施法入口的单位。当前练习房间只有一个真实连接，尚未实现多人房间成员广播。
- 客户端 `G2C_BrawlSkillUseHandler -> BrawlSkillUseViewHelper -> BrawlCharacterAnimationHelper`。复制协议数据后排队，校验 EntryId/RoomId、EventId 去重，按 RenderTick 播放；结束/中断必须匹配 ActiveCastId。退出清队列，重建模型不重播旧动作。
- 超过现有 MaxCatchUpTicks 的迟到开始/发射不补播；结束/中断仍消费以释放已有锁向。模型短暂未加载时暂存，超过窗口丢弃。此策略优先避免补播过时攻击，弱网下可能省略动作，弹丸与战斗结果仍以原有快照为准。
- 连发开始锁定施放方向，实际 Fired 重播上半身动作，Completed 解锁但允许最后一段动作播完，Interrupted 立即关闭上半身层。死亡/出生优先清除动作和锁向。瞬发不维持动画时长锁向，正在移动时立即恢复实际移动朝向。
- 当前公牛 AI 只有追逐，没有施法调用。本轮不添加 Bot 攻击行为；其出生、移动、死亡可验证，攻击表现复用入口需后续 AI 实际施法后验证。
- 因为这次经授权新增了服务器通知，测试攻击需要服务器加载本次协议和代码；此前单纯移动动画改动不需要服务器重启。本轮不自动停止/重启服务器。
- 扩展 `Statesync_BrawlRevolver_Test`：连发开始/逐发/完成、死亡中断与新 CastId、同轮多颗只发一次 Fired；扩展 `Statesync_BrawlColtSuper_Test`：拒绝不通知、成功身份/槽位正确、重复请求不追加成功事件。仅编译检查，未执行运行用例。
### 本轮最终验证结果

- 21 个角色的出生/攻击/死亡映射已由 Unity 生成并保存；24 个独立替换键、引用有效性与运行时 Override 实例隔离检查通过。默认共享 Override、MaskTint、原始动画导入设置未修改。
- `dotnet build ET.sln` 最终成功：0 错误、14 个既有 HybridCLR CS0649 警告，耗时 54.54 秒。双端模型/逻辑、客户端 ModelView/HotfixView、Editor 与扩展测试代码均参与编译。日志：`Temp/BrawlAnimation-actions-final-build.log`。
- 早期尝试遇到旧服务器进程锁 DLL；用户关闭服务器后最终构建成功。代理没有停止或启动服务器，没有进入 Unity Play，没有执行运行测试。出生、死亡姿态以及移动中上半身攻击的实际观感仍需人工验证。