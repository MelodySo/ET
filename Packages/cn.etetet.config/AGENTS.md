# cn.etetet.config

## 概述

配置导出包，承载 Luban 配置、`ET.Config` 相关代码输出，以及 `ET.ExcelExporter` / `ET.ExcelMcp` 等内部工具链。

## 详细文档

- **代码与包规范**：请查看 `/et-code` skill
- **编译与导出**：请查看 `/et-build` skill
- **测试执行**：请查看 `/et-test-run` skill
- **Excel 工具操作**：请查看 `/et-excel` skill

## 修改约束

- 对外包身份使用 `cn.etetet.config`，但内部工具名暂时保留 `Excel` 语义
- 修改 Luban、导出器或 MCP 路径时，优先检查包名和目录引用，不要顺手改内部工具程序集名
- `PackageType` 与 `packagegit.json` 的编号必须保持一致，当前固定为 `13`
- LubanGen.ps1 平台检测使用 `$IsMacOS` 的布尔值；Windows PowerShell 7 也存在该变量，不能检查它是否为 null。
- 每组 Luban 导出允许 60 秒，超时终止整个子进程树，避免遗留生成器继续写文件。
- brawl 关卡表通过自身 `Luban/Config/Defines/BrawlLevel.xml` 注册，只有一个业务 xlsx，无需另建 Base 表。
