# cn.etetet.brawl

## 范围

荒野对战 Demo 的独立业务包，包编号 55。当前仅包含关卡 Excel 与关卡编辑器，不改变登录、地图进入或战斗流程。

- 根目录 `Ignore.ET.Brawl.asmdef` 是必需的默认隔离边界；仅由 CodeMode 生成的 asmref 启用对应目录。缺少它会让 Client 模式误编译 Editor/Test。

## 关卡配置

- 关卡 Excel：`Luban/Config/Datas/BrawlLevel.xlsx`，一行一关卡；玩法与英雄表由用户维护。
- `Luban/Config/Defines/BrawlLevel.xml` 登记表定义，字段从 Excel 表头读取；配置类由 Luban 生成，禁止重复手写。
- `Luban/Config/Defines/BrawlHero.xml` 注册英雄表，供 statesync 大厅的轻量英雄 UI 读取；本轮不实现购买、升级或战斗入口。
- `SceneAsset` 仅导出客户端，其余字段双端可用。每格固定 1 米，不保存 CellSize。
- 三个数组使用英文逗号分隔整数；空单元格表示空数组。当前允许空数组作为未完成草稿，不代表可开局地图。
- 公共格子枚举在 `Scripts/Model/Share/Level/`。每局运行时必须复制配置数组，不能修改共享配置。

## 编辑器

- 入口：`ET/Brawl/关卡编辑器`，代码放 `Scripts/Editor/Share/Level/`。
- 使用现有 `Bin/ET.ExcelMcp.dll`（EPPlus）通过标准输入 JSON-RPC 读写，不往 Unity 引入 Excel DLL。
- 调用 excel_range 写字符串时必须加前导单引号（工具会移除它），否则逗号分隔数组和纯数字资源名会被工具自动转换为数字。
- 文件仅在读写操作期间打开，不能在 OnEnable/OnDisable 之间持有工作簿。
- 保存先写原表副本，再检查文件及目标行是否发生外部变化，最后替换；保留原文件备份于 Temp。
- 保存不改变当前 ID；退出才清空。关闭窗口/退出时提示未保存修改。
- EditorWindow 提供枚举笔刷、连续拖画、缩放平移和出生点编辑；平地为 0、颜色 #00FF00，新建默认 20×20。
- 行列各限制 1–100；修改尺寸保留左下角区域并重算点位索引，缩小须确认。道具点暂不编辑，尺寸变化时同步重映射。
- SceneAsset 支持拖入 SceneAsset，保存项目相对路径（含 .unity）；兼容显示唯一的旧短名称。
- 原生 Unity Undo/Redo，一次地形拖画为一次撤销；保存、导入、退出时清除本窗口撤销记录，避免恢复过期工作簿冲突戳。
- 窗口保存必须满足 SpawnPoints 数量等于 BrawlMode.xlsx 的 TeamSize×TeamCount，且出生点位于平地或草丛。读取玩法源表不依赖运行时配置单例，也不要求玩法 Enabled。
- 工作簿基础校验保留读取旧空网格草稿的兼容性，窗口导入后将其初始化为平地。
- 本轮按用户要求不运行测试或编译；旧 EditorState 测试的空出生点保存预期需随新规则更新。

## 验证

参见 `Test.md`。所有命令使用 pwsh，唯一编译命令 `dotnet build ET.sln`。不要手工生成 .meta 或编辑 .csproj。
