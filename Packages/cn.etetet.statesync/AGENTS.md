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

## 账号登录界面

- 登录仍使用原有按钮，首次合法登录自动注册；账号规则和错误提示由 login 包提供。
- LoginPanelComponent.LoginInProgress 防止重复点击；失败显示提示且允许重试，不触发 LoginFinish。
- 不修改 YIUIGen 生成代码，业务界面逻辑修改 YIUISystem / YIUIComponent。
