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
- 详情显示 BaseHp 与 MoveSpeedTier；已解锁、未解锁均可试用。练习赛事件仍留空。

## 本地训练场第一版

- `BrawlTrainingHelper` 由英雄详情的训练场按钮触发；当前按用户约定选取关卡 10101，场景从 BrawlLevelConfig.SceneAsset 读取，不调用原 MMO EnterMap，也不进入 Map2。
- 使用 DefaultPackage 叠加加载场景，兼容 SceneAsset 的相对路径/短名称；当前收集器按文件名寻址。
- 状态与资源由 LobbyPanelComponent.TrainingSession 持有；加载防重入、异步后用 EntityRef 获取大厅；大厅销毁时取消/清理。
- 隐藏大厅 CanvasGroup，创建独立 Overlay 退出按钮；退出卸载场景并恢复原活动场景、大厅显示和相机状态。
- 只创建原点处主角胶囊、展示相机与灯光。没有网络 Unit、移动、战斗、Grid 实例化或其他单位，不修改场景资源中已有对象。
- 训练场是本地展示流程，不建立服务端房间；现有账号登录仍需服务端与 MongoDB。CodeMode 不变，不等同于完整离线启动。
- 本轮遵照用户要求不运行测试、不启动服务端或 PlayMode；由用户手动验证进入与退出。

## 账号登录界面

- 登录仍使用原有按钮，首次合法登录自动注册；账号规则和错误提示由 login 包提供。
- LoginPanelComponent.LoginInProgress 防止重复点击；失败显示提示且允许重试，不触发 LoginFinish。
- 不修改 YIUIGen 生成代码，业务界面逻辑修改 YIUISystem / YIUIComponent。
