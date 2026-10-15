# Braw AI 开发交接

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