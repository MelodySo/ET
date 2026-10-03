# cn.etetet.unitybridge

## 概述

Unity 本地文件桥接包，提供：

- `DotNet~` 下的纯命令行 `ET.UnityBridge`
- `Scripts/Editor` 下的 Unity Editor 文件宿主，汇入 `ET.Editor`
- `Scripts/Model/Share` 下的桥接命令、错误码与共享文件协议

## UnityBridge skill 入口

- **架构规范**：请查看 `/et-code` skill
- **编译构建**：请查看 `/et-build` skill

本文件只保留 UnityBridge 相关 skill 的轻量路由。harness 中的 `et-unitybridge` 只负责分流；命中 UnityBridge 任务后按需读取下面对应 `SKILL.md`，不要一次性加载全部 UnityBridge 规则。

### et-unitybridge - UnityBridge 命令调用入口

**使用场景**：

- 查询 UnityBridge 宿主是否在线、Unity 是否正在编译、PlayMode 状态、CodeMode、Unity 版本。
- AI 操作 Unity Editor：资源、场景、选择集、GameObject、Transform、Inspector、Prefab、菜单、截图、GameView、Editor 测试。
- 执行 `Compile` / `Refresh` / `RegenProject` / `EnterPlay` / `ExitPlay` / `Reload` 等跨 Unity 状态变化的命令。
- 排查 UnityBridge 返回的 `Error` / `Message`。

**补读**：`Packages/cn.etetet.unitybridge/skills/et-unitybridge/SKILL.md`

## 核心目录

- 直接依赖 `ET.Test` 的编辑器处理器放在 `Scripts/Editor/Test`，与测试框架一起仅在 `ClientServer` 模式启用；不要放在所有模式都编译的 `Scripts/Editor/Share`。
- 切换模式验证：`Client` 下不应编译 `UnityBridgeUnityTestRunHandler`；`ClientServer` 下该处理器应与 `TestDispatcher` 一同编译。

| 路径 | 说明 |
|------|------|
| `DotNet~` | `ET.UnityBridge.csproj` 与命令行入口 |
| `Scripts/Editor` | Unity Editor 宿主、处理器与分发逻辑 |
| `Scripts/Model/Share` | 桥接命令、错误码、路径与文件存储协议 |
