# cn.etetet.loader

## 概述

加载器包，负责 `ET.Loader` 的 DotNet 入口、加载流程和相关运行时代码。

## 详细文档

- **编译构建**：请查看 `/et-build` skill
- **架构规范**：请查看 `/et-code` skill

## 核心目录

| 路径 | 说明 |
|------|------|
| `DotNet~` | `ET.Loader.csproj` 与相关生成脚本 |
| `Scripts` | Loader 层共享与服务端代码 |
| `CodeMode` | Loader 的 CodeMode 扩展代码 |

## IDE 工程生成

- OnGenerateCSProjectProcessor.OnSelectingCSProjectStyle 返回 Legacy，防止 VS2026 插件生成 slnx 并删除 ET.sln。
- OnGeneratedSlnSolution 返回 MainPackage.txt 首个非空行指定主包的 ET.sln 内容，保留 Unity、服务端和工具工程；根方案可能是硬链接，不接受普通 Unity 方案覆盖。
- ET/Loader/ReGenerateVisualStudioProjectFiles 从 VS 插件发现真正的 devenv.exe（排除 VS Code），设置外部编辑器并调用 SyncAll；用于修复 internal 编辑器不更新 csproj 的问题。
- 不修改 PackageCache 或手工维护 csproj 文件清单。
