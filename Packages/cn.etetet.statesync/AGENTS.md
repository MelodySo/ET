# cn.etetet.statesync

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
- 详情显示 BaseHp 与 MoveSpeedTier；已解锁、未解锁均可试用。训练场本地展示，练习赛请求服务器创建房间。

## 服务器练习赛第一版

- 入口固定玩法 102、关卡 10201、对手英雄 201（公牛），这是当前 Demo 约定；玩法表要求启用、TeamSize=1、TeamCount=2、TimeLimitSeconds=0。
- Gate 上通过 C2G_BrawlPracticeEnter / Exit 两组 RPC 进入和退出；进入返回完整单位快照。本轮没有动态出生/销毁通知、准备确认或战斗同步。
- BrawlPracticeSessionComponent 归属于登录 Session；其子 BrawlPractice Scene 与 Gate 共用 Fiber，包含 UnitComponent 及玩家、Bot、NPC 的真实服务端 Unit。退出或连接销毁时随实体树释放。
- 单位 ConfigId 按 Kind 指向 BrawlHero 或 BrawlNPC，禁止调用原 MMO Unit.Config() 解释这些 ID。首版不添加移动、AI、数值、技能或 AOI 组件。
- 服务端校验关卡、出生点及 NPC，读取 BrawlGlobal.CellSize 计算世界坐标；客户端按快照的单位 ID、配置 ID 与坐标加载模型，不自行补出 Bot/NPC。
- EntryId 用于进入请求幂等与退出匹配；新进入替换该连接旧房间，迟到的旧退出不会删除新房间。当前不支持断线续局。
- 客户端共用 BrawlTrainingHelper 的场景、地形和退出 UI；练习赛显示服务器房间 ID，训练场仍为本地流程。资源加载失败尝试退出服务器房间并恢复大厅。

## 本地训练场第一版

- `BrawlTrainingHelper` 由英雄详情的训练场按钮触发；当前按用户约定选取关卡 10101，场景从 BrawlLevelConfig.SceneAsset 读取，不调用原 MMO EnterMap，也不进入 Map2。
- 使用 DefaultPackage 叠加加载场景，兼容 SceneAsset 的相对路径/短名称；当前收集器按文件名寻址。
- 状态与资源由 LobbyPanelComponent.TrainingSession 持有；加载防重入、异步后用 EntityRef 获取大厅；大厅销毁时取消/清理。
- 隐藏大厅 CanvasGroup，创建独立 Overlay 退出按钮；退出卸载场景并恢复原活动场景、大厅显示和相机状态。
- 主角按所选 BrawlHeroConfig.ModelPath 加载，放在关卡第一个出生格，不再创建胶囊；格子尺寸读取 BrawlGlobalConfigCategory.CellSize（米，当前 2），进入时校验正数并存入会话。列对应 X、行对应 Z。地形、英雄、NPC 坐标及相机取景共用此尺寸，配置仍保存格子索引，不缩放 Prefab。
- 使用 BrawlLevelConfig.StyleId 获取 BrawlLevelStyleConfig，MapGrid 1/2/3/4 分别使用 ModelPath1/2/3/4；Ground=0 不生成模型，空地址跳过，ModelPath5 暂未使用。按收集器短地址加载 Prefab，保留其原始旋转与缩放。
- 同种模型每次会话只加载一个句柄；退出先销毁实例/卸载场景，再释放句柄并恢复大厅。大厅临时 CanvasGroup 保留至大厅销毁，避免同帧重新进入遇到延迟销毁问题。
- NPCIds/NPCPoints 按下标配对，读取 BrawlNPCConfig.ModelPath 加载模型；校验数组、位置和配置。只展示，不创建网络 Unit、血量或战斗组件。保留 Prefab 的武器、旋转、缩放与 Animator，只在实例上关闭 Root Motion，不驱动动画参数，不修改场景已有对象。
- 训练场是本地展示流程，不建立服务端房间；现有账号登录仍需服务端与 MongoDB。CodeMode 不变，不等同于完整离线启动。
- 保留验证入口 `Brawl/Verification/RunTerrain`：从本包 Init 场景启动，等待登录 UI 就绪后手动执行；无需登录/服务器，使用真实加载与退出逻辑验证 10101、10001、10101 三轮。仅诊断入口绕过登录，正常业务登录不变。结果输出 BRAWL_TERRAIN_PASS，截图保存 Temp/BrawlTerrain_{id}.png；验证后停止 Play。

## 账号登录界面

- 登录仍使用原有按钮，首次合法登录自动注册；账号规则和错误提示由 login 包提供。
- LoginPanelComponent.LoginInProgress 防止重复点击；失败显示提示且允许重试，不触发 LoginFinish。
- 不修改 YIUIGen 生成代码，业务界面逻辑修改 YIUISystem / YIUIComponent。
