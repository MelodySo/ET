# Braw AI 开发交接

## 2026-10-16 雪莉／柯尔特战斗 AI（当前版本）

本节覆盖下方历史公牛规则。最新授权为实际实现并编译；不启动服务器、不进入 Unity Play，运行效果由用户验收。未修改或导出 Excel，保留工作区已有的 BrawlNPC.xlsx、BrawlSkill.xlsx 及 WalldoffStudios 资源改动。

- 练习赛运行时互选对手：玩家雪莉 101 → 柯尔特 Bot 401；玩家柯尔特 401 → 雪莉 Bot 101。不是关卡编辑器预置 NPC；重生沿用本局保存的英雄，重新挂载 AI。其他英雄尚未配置练习赛配对，会明确报配置未支持。
- 删除运行树中的 5 米感知限制。目标有效且未死亡就持续追击；按最新要求，每个技能分别使用 `max(0, SkillRangeParam1 - RangeMargin)` 作为 XZ 中心施放距离，余量默认 0.5 米。当前已导出配置下：雪莉普攻／大招 7.17 米、柯尔特普攻 8.5 米、大招 11.5 米。大招不再受普攻停距约束；大招耗尽后，若目标还在普攻范围外则继续接近。目标出生期间可以追踪，但等出生结束才攻击；自身死亡、出生、受控时不行动。
- 两棵独立可编辑树，复用节点实现。选择顺序：已有连发则停住等结束 → 大招位置与大招施放 → 普攻位置与普攻施放 → 普攻位置畅通则原地等弹药／冷却 → 继续追击。受阻时即使距离已足够也继续绕路；无可达路径时沿用原寻路的停止行为。
- 普攻、大招分别沿 `英雄 → 对应槽位技能 → 弹丸 → CollisionRuleGroupId` 解析规则，复用 `BrawlCollisionRuleHelper.Resolve/Match`，只取 `BlockProjectile` 决定墙体遮挡；`ApplyHitEvents` 不等于阻挡，也不在视线查询中执行伤害／破墙。当前规则下，地形 Wall 挡两者，可摧毁墙／桶挡普攻但不挡大招；以后跟随规则组、标签及阵营匹配变化，不再由大招槽位或布尔开关假定穿墙。
- 视线仍是 Bot 与目标之间的有限矩形，默认宽 0.2 米，与子弹尺寸、散射和发射器无关。本轮候选仍限定地形 Wall 和单位 Tile，草丛、水、英雄不作为视线障碍；死亡或出生中的墙体跳过。地形墙使用 Tile 标签及 NeutralHostile 阵营，与实际弹丸一致。实际子弹命中、穿透与破墙仍由原碰撞流程结算。
- `BrawlAICombatHelper` 直接调用已有 `BrawlSkillCastHelper.TryStart`，复用资源消耗、冷却、充能、连发与 `G2C_BrawlSkillUse`，没有新增协议或枪口特效。瞄准当前目标位置，不预判；单次连发沿用施放时锁定的方向，期间停止移动。Bot 普攻决策间隔使用技能配置 CooldownTime（至少一 Tick），避免雪莉瞬间耗完三发；不改变玩家普攻规则。

### 资源、编辑和代码入口

1. Unity 菜单 `Brawl/AI/Create or Select Shelly and Colt Combat` 创建缺失资产、选中柯尔特并导出；已有树不覆盖。资源为 `Packages/cn.etetet.statesync/Settings/BrawlAI/ShellyCombat.asset` 和 `ColtCombat.asset`。
2. Inspector 的 Tree → `Open BehaviorTreeEditor` 编辑节点；Save 保存，`Brawl/AI/Export Selected AI` 或树窗口 Export 导出全部已配对树。HeroId 是本树英雄，OpponentHeroId 是练习赛对手，互为配对；OpponentHeroId=0 的历史树不参与导出。
3. `BTBrawlAttackPosition` 配置 Slot、RangeMargin（默认 0.5 米）、SightWidth（默认 0.2 米）；不再保存固定 Distance 或 IgnoreDestroyableWalls。`BTBrawlUseSkill` 选择 NormalAttack 或 Super；`BTBrawlHasActiveCast` 防止连发中重复施放。射程跟随各技能已加载配置，调整普攻余量时也应同步等待装填分支。导出器迁移旧节点时按同一 Sequence 的施法动作推导槽位，等待分支取普攻；保留树结构及已有有效槽位。
4. `BrawlAIConfigCategory` 以 Trees 按英雄索引，PracticeOpponents 保存配对。导出只更新 statesync 的 Client／ClientServer／Server 三份 `BrawlAIConfigCategoryFactory_Config.cs`，未重导 MMO Spell／Buff、Luban 或其他资产。
5. 服务端：`BrawlPracticeHelper` 选对手，`BrawlUnitFactory` 挂 AI，`BrawlAIHelper` 驱动树和现有 A*，`BrawlAICombatHelper` 判攻击位置／施放，`BrawlAISightHelper` 查询墙体；Share 的 `BrawlAISightGeometryHelper` 完成矩形和圆形障碍相交。
6. 旧 `BullChase.asset` 保留为历史资产。`Brawl/Verification/RunAI` 旧脚本断言“无攻击、5 米内追击”，菜单现只提示已停用，不自动登录或进场。

### 验证范围与手测清单

- `Statesync_BrawlCombatAI_Test`：双向配对、7.17／8.5／11.5 米边界、大招优先与耗尽后追到普攻距离、技能射程改变后阈值变化、缺弹等待、挡墙后继续追击、各技能独立规则组、更换弹丸规则组与 BlockProjectile 后判断变化、阵营匹配、只触发效果不阻挡、视线查询无施法副作用、地形墙与草、死亡墙清除、连发不重入及旋转矩形。旧 `Statesync_BrawlAI_Test` 验证超过 5 米仍追击，保留绕障、不可达、边界与重生覆盖。用例本轮只编译，未执行。
- 本次技能射程／碰撞规则接入已完成 Unity 刷新及行为树导出，三份 CodeMode 工厂内容一致。`dotnet build ET.sln` 成功：0 错误、14 个既有 HybridCLR CS0649 警告，74.48 秒，日志 `Temp/BrawlAI-skill-rules-build.log`。未启动服务器、进入 Unity Play 或运行测试；资源导出和编译通过不代表实机效果通过。前一版固定距离构建记录保留在 `Temp/BrawlCombatAI-build.log`。
- 用户启动新编译的服务器后，分别选择雪莉、柯尔特进入练习赛，确认对手互换；远离攻击范围仍应追来，普通攻击约在柯尔特 8.5／雪莉 7.17 米处停住。主动贴近不会触发后退，首版没有保持最小距离或侧移。
- 消耗弹药后应原地等待装填，连发期间不继续贴脸；让 Bot 命中获得大招充能，检查柯尔特可在 11.5 米优先开大，放完后继续接近到 8.5 米普攻；雪莉两种技能均在 7.17 米内。首次出生的大招并非满充能。
- 隔可摧毁墙时，无大招应绕路寻找能普攻的位置，有可用大招应停在规定距离内直接开大；不可摧毁地形墙应挡住两种技能；草丛不影响这个简单视线判断。
- 玩家死亡期间 Bot 停止追踪；玩家重生后恢复。击杀 Bot 后，应以原对手英雄重新出生并恢复行为；退出重进不沿用旧路径、连发或普攻计时。
- 待优化：射程边界迟滞、走位／后退、发射器专属命中预测、复杂目标选择、大量 Bot 性能和视线缓存。本版不以矩形视线保证每颗子弹都能命中。

## 2026-10-15 历史公牛版本（以下规则已被上节替代）

状态：用户接管手测，已停止联调；实现及服务端回归通过，完整实机验收未完成（2026-10-15）。

## 需求与运行规则

- 练习赛公牛 Bot（英雄201），XZ中心距离玩家小于等于5米执行Chase，大于5米执行Idle。恰好5米属于Chase；没有迟滞或额外攻击距离。
- 不攻击、不释放技能、不巡逻、不返航。英雄间沿用现有可重合规则。
- 服务端固定50ms移动步内执行真实行为树。Idle立即清路径与移动意图；Chase只输出方向，实际位移经过现有地形和NPC碰撞。
- 死亡、出生、受控及目标无效时停止；重生创建新AI组件，退出随房间释放。

## 行为树与插件使用

1. Unity菜单 `Brawl/AI/Create or Select Bull Chase` 选中现有资源；仅资源不存在时创建默认树，不覆盖已有编辑。
2. 资源：`Packages/cn.etetet.statesync/Settings/BrawlAI/BullChase.asset`。HeroId=201。
3. Inspector树字段的 `Open BehaviorTreeEditor` 打开编辑器；F8菜单依赖已设置的树上下文。
4. 默认结构：BrawlAIRoot → Selector → 第一分支Sequence（BTBrawlPlayerWithinDistance，Distance=5；BTBrawlChase），第二分支BTBrawlIdle。节点按优先级互斥选择；Unit绑定使用根输出的Unit。
5. 右键节点创建/删除/复制/粘贴/替换；Save保存资产。运行参数存于AI组件，不能把每只Bot的路径写入共享树。
6. 使用 `Brawl/AI/Export Selected AI`；2026-10-15修复后，树窗口Export也通过IBTAssetExporter调用当前Brawl资产的同一专用导出器。其它树仍走原有导出流程。
7. 专用导出只写三份 `Packages/cn.etetet.statesync/CodeMode/Config/{Client|ClientServer|Server}/Config/BrawlAIConfigCategoryFactory_Config.cs`，不批量重导Spell/Buff。成功日志前缀 `BRAWL_AI_EXPORT_OK`。
8. 首次新增配置目录或切换CodeMode后，在项目根目录运行 `dotnet ./Bin/ET.CodeMode.dll --CodeMode=Client`（当前模式为Client），通过Unity刷新生成程序集引用和工程文件，再运行唯一构建入口 `dotnet build ET.sln`。不要手改asmref、meta或csproj，不要把其他模式硬改为Client。
9. 等Unity编译结束；停止旧服务器并从项目根目录启动本地服务器，重新进入 `Packages/cn.etetet.statesync/Scenes/Init.unity`。导出或Unity自动编译不代表正在运行的服务器已更新配置。

## 实现落点

- Model/Share/BrawlAI：根、条件、Chase、Idle节点和BrawlAIConfigCategory。
- Model/Server/Brawl/BrawlAIComponent：路径、目标ID、重算时刻等每单位状态。
- Hotfix/Server/Brawl/BrawlAIHelper及节点Handler：BTHelper.RunTree实际执行；单位工厂只给绑定英雄的Bot挂AI。
- BrawlPathfindingHelper：先检查可直达路径，否则A*搜索格中心；每条边检查角色半径、墙、水、地图边界和当前存活阻挡NPC，使用权威移动函数复核。默认250ms重新规划，目标改变立即重算；不可达清路径。
- BrawlPracticeMovementHelper：在原50ms移动步中调用AI，保留权威状态及碰撞流程。
- Editor/Share：资产创建、校验、专用导出与手动实机验证菜单。
- 行为树编辑器增加通用IBTAssetExporter接口，避免底层包反向依赖statesync；Brawl资产实现接口。

## 已核实的客户端配置故障

异常：`config factory not found: ET.BrawlAIConfigCategory`。
堆栈：ConfigLoaderHelper.cs:195 → :131 → 客户端ConfigLoader.cs:24 → statesync Entry.cs:47。
原因：初次创建statesync/CodeMode/Config目录后没有运行CodeMode生成引用；Client目录缺少AssemblyReference.asmref，Unity ET.Config.csproj未包含AI工厂。服务端通配编译包含工厂，所以服务端构建/测试通过不能证明客户端可启动。
修复：运行项目ET.CodeMode工具、Unity刷新。已确认生成 `{ "reference": "ET.Config" }`，ET.Config.csproj包含AI工厂，Library/ScriptAssemblies/ET.Config.dll重新生成。客户端优先使用已加载ET.Config程序集，不能仅核对Bin/ET.Config.dll。
Console桥接曾把Debug.LogError记录标为ScriptingWarning，诊断必须读取全部级别，不能只过滤Error。

## 验证证据

- 首次完整构建：0错误、14个已有HybridCLR警告；Temp/BrawlAI-build.log。
- 新增实机入口后的第二次构建：0错误、14警告，42.62秒；Temp/BrawlAI-build-final.log。
- Export按钮修复后完整构建：0错误、14警告，62.82秒；Temp/BrawlAI-export-fix-build.log。
- 定向回归：`Test --Name=^Statesync_Brawl(AI|Lifecycle)_Test$`，Total2/Passed2/Failed0，140ms；Temp/BrawlAI-regression-final.log，Logs/All.log同样确认通过。
- AI回归覆盖距离内/恰好5/距离外、跨界停步、墙和NPC绕行、水隔断不可达、地图边界、半径窄道、目标死亡、Bot死亡清路径、重生新组件和房间释放。
- 实机入口：Play中执行 `Brawl/Verification/RunAI`，使用已保存的本机登录或已登录会话，不输出凭据。检查两次新练习房间、静止与移动目标、无伤害弹丸、退出释放；成功前缀BRAWL_AI_PLAY_PASS及BRAWL_AI_PLAY_COMPLETE，截图Temp/BrawlAI-play-1.png和2.png。
- 客户端配置引用修复后已重新进入指定Init：缺失配置工厂异常消失，客户端正常进入UI初始化；存在YIUI DontDestroyOnLoad警告。证据Temp/BrawlAI-console-after-config.json。
- 最新完整构建Temp/BrawlAI-client-config-build-retry.log最终0错误，6条MSB3026文件复制重试警告，32.29秒。前一次失败为本任务服务器占用DLL，已停止后重构建成功，不是源代码编译错误。
- RunAI已实际调用，但登录失败，未进入练习赛，未产生PASS标记或截图。错误为http://127.0.0.1:10101/get_router连接被拒绝。服务器日志2026101521.RouterManager@1@0.Error.log在21:29:21记录HttpListenerException(5)“拒绝访问”，地址http://+:10101/，HttpComponentSystem.cs:26/32。需用项目规定的管理员服务器环境或由用户处理HTTP监听权限；本次没有修改系统URL ACL。
- 用户要求接管手测，已停止后续修复及联调。核验本任务服务器PID38688（2026-10-15本机21:29:10启动、dotnet ET.App StateSync/Localhost）后关闭，旧PID8080也已不存在；Bridge ExitPlay返回成功，IsPlaying=false。自动验证和输入驱动随Play退出终止，未清Console或日志。
- 实机静止/移动追踪、实际障碍与边界、两次退出重入仍待用户验收；服务端自动测试通过不可替代实机通过。

## 限制与后续

- A*采用格中心，可能找不到几何上存在但格中心无法表达的狭窄路线；搜索预算最多10000节点。当前仅单Bot演示，未做大量Bot性能验收。
- 精确边界、障碍与重生已在可控服务端测试覆盖；实机入口目前验证静止/移动目标及退出重入，不能将两者混称为所有真实场景已通过。
- 不提交或推送；保留用户BrawlGlobal.xlsx及WalldoffStudios资源、用户刚执行的其它导出产物。
