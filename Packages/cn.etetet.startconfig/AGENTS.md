# cn.etetet.startconfig

服务端启动配置包。Luban 下按 Localhost、Moba、Mmo、Release 分组，沿用项目统一导出入口。

- 修改表后使用 `dotnet ./Bin/ET.ExcelExporter.dll`，不要手工编辑生成代码。
- PowerShell 平台判断须检查 `$IsMacOS` 的布尔值，不能以变量是否存在判断平台；PowerShell 7 在 Windows 上也定义它。
- 所有命令使用 pwsh；唯一编译命令 `dotnet build ET.sln`。
