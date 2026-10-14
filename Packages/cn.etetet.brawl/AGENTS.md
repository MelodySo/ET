# cn.etetet.brawl

## 范围

荒野对战 Demo 的独立业务包，包编号 55。当前仅包含关卡 Excel 与关卡编辑器，不改变登录、地图进入或战斗流程。

- 根目录 `Ignore.ET.Brawl.asmdef` 是必需的默认隔离边界；仅由 CodeMode 生成的 asmref 启用对应目录。缺少它会让 Client 模式误编译 Editor/Test。

## 关卡配置

- 关卡 Excel：`Luban/Config/Datas/BrawlLevel.xlsx`，一行一关卡；玩法与英雄表由用户维护。
- `Luban/Config/Defines/BrawlLevel.xml` 登记表定义，字段从 Excel 表头读取；配置类由 Luban 生成，禁止重复手写。
- BrawlLevel.xlsx 包含 BrawlLevel 和 BrawlLevelStyle 两个子表，通过 `子表名@文件路径` 分别登记。关卡 StyleId 引用风格；编辑器读写十个字段，保存时保留风格子表。
- 风格字段沿用用户命名 ModelPath1..5；编号对应地形枚举，Ground=0 暂无资源字段，ModelPath5 保留。模型值为 YooAsset 文件名短地址；运行时实例化由 statesync 的 BrawlTrainingHelper 负责。
- `Luban/Config/Defines/BrawlHero.xml` 注册英雄表，供 statesync 大厅和试用入口读取；不实现购买、升级。
- `Luban/Config/Defines/BrawlMode.xml` 注册玩法表，供双端运行时读取玩法人数、开关和时限；Name、Description 仍仅导出客户端。玩法 102、关卡 10201 的服务器练习赛由 statesync 包实现。
- `SceneAsset` 分组为 c；`StyleId` 支持 c（仅客户端显示风格）或留空（兼容旧表），其余字段分组留空。编辑器保存保留原有分组，不强行覆盖。关卡表只保存格子索引，不保存 CellSize。
- `BrawlGlobal.xlsx` 为竖向单例表，A1 为 `##var#column`，每个参数占一行，E 列为值；BrawlGlobal.xml 使用 mode="one" 注册，无 ID。CellSize 为 int，当前约定 1 米，分组留空供双端使用；运行时通过 BrawlGlobalConfigCategory.CellSize 读取。新增参数后使用原 ExcelExporter 导出。
- 英雄 CollisionRadius / CollisionHeight 为 float，双端共享，单位米；当前约定半径 0.3、完整高度 1.2。练习赛移动和出生校验读取半径，高度暂未用于平面碰撞。NPC Width=0 为圆柱（Length 为直径）；Width>0 为轴对齐矩形（Length/Width 为完整 X/Z 尺寸）；PivotX/Z 为相对 Unit 原点偏移，单位米。statesync 仅对挂阻挡组件的 NPC 消费这些几何数据，不乘格长/模型缩放。
- BrawlUnitComponentBlock.xml 注册阻挡组件表，Id/CanBeDestroyed 双端共享，Name 仅客户端。NPC.ComponentTypes/ComponentValues 按下标配对，类型 1 为阻挡，值为该表行 ID；墙当前引用 1、木桶引用 2、劫匪为空。用户明确 CanBeDestroyed 保留在阻挡组件，不拆独立可摧毁组件。是否挂载决定是否阻挡，bool 用于后续摧毁玩法。
- MapGrid、SpawnPoints、NPCIds、NPCPoints 使用英文逗号分隔整数；空单元格表示空数组。当前允许空数组作为未完成草稿，不代表可开局地图。ItemSpawnPoints 已删除。
- 可摧毁墙通过 NPC 摆放，不再属于地形。地形编号以 BrawlTileType 为准：Ground=0、Wall=1、Grass=2、BulletPassableObstacle=3、Water=4；这是用户调整后的编号，不再沿用历史编号空缺。
- NPCIds 与 NPCPoints 等长、按下标配对；NPCPoints 是格子索引，每格最多一个 NPC，与 MapGrid 独立。已有地图不自动添加 NPC。
- BrawlNPC.xml 注册 NPC 源表；模型地址采用不带扩展名的 Prefab 文件名，匹配主包 DefaultPackage 的 BrawlPrefabs 收集规则。
- 公共格子枚举在 `Scripts/Model/Share/Level/`。每局运行时必须复制配置数组，不能修改共享配置。

- BrawlSkill.xml 注册 BrawlSkill.xlsx 内的 BrawlSkill/BrawlProjectile 两个分页，生成双端配置。技能算法和服务器弹丸放 statesync；不手写生成类。当前仅消费雪莉单轮发射需要的字段，其它行允许保持草稿。导出若出现空 Id 行，先检查表尾纯空格残留，不修改实际技能数值绕过校验。

## 编辑器

- 入口：`Brawl/关卡编辑器`，代码放 `Scripts/Editor/Share/Level/`。
- 使用现有 `Bin/ET.ExcelMcp.dll`（EPPlus）通过标准输入 JSON-RPC 读写，不往 Unity 引入 Excel DLL。
- 调用 excel_range 写字符串时必须加前导单引号（工具会移除它），否则逗号分隔数组和纯数字资源名会被工具自动转换为数字。
- 文件仅在读写操作期间打开，不能在 OnEnable/OnDisable 之间持有工作簿。
- 保存先写原表副本，再检查文件及目标行是否发生外部变化，最后替换；保留原文件备份于 Temp。
- 保存不改变当前 ID；退出才清空。关闭窗口/退出时提示未保存修改。
- PlayerPrefs 的 ET.Brawl.LevelEditor.LastLevelId 记住最后使用/关闭时填写的正数 ID；新开窗口自动填回 ID，不自动导入，域重载不覆盖现有草稿。退出关卡不清除记忆。
- EditorWindow 提供枚举笔刷、连续拖画、缩放平移和出生点编辑；平地为 0、颜色 #00FF00，新建默认 20×20。
- NPC 笔刷单击放置/替换、右键单击删除，不拖刷；复用 Unity Undo。NPC 只能放在平地或草丛，且不与玩家出生点重叠；改变地形造成冲突时保存会拒绝。
- NPC 列表由窗口持有 BrawlNPCWorkbook 源表快照，第一次选择 NPC 工具时读取，也可手动刷新；保存含 NPC 的关卡时重新读取并核验 ID。名称、类型、生命值、模型地址只读。
- NPCIds / NPCPoints 随新建、导入、保存、退出和尺寸修改一起维护；尺寸缩小时保持配对裁剪。训练场由 statesync 的 BrawlTrainingHelper 按这些数据加载 NPC 模型，尚无战斗逻辑。
- 行列各限制 1–100；修改尺寸保留左下角区域并重算出生点索引，缩小须确认。
- SceneAsset 支持拖入 SceneAsset，保存项目相对路径（含 .unity）；兼容显示唯一的旧短名称。
- 原生 Unity Undo/Redo，一次地形拖画为一次撤销；保存、导入、退出时清除本窗口撤销记录，避免恢复过期工作簿冲突戳。
- 窗口保存必须满足 SpawnPoints 数量等于 BrawlMode.xlsx 的 TeamSize×TeamCount，且出生点位于平地或草丛。读取玩法源表不依赖运行时配置单例，也不要求玩法 Enabled。
- 工作簿基础校验保留读取旧空网格草稿的兼容性，窗口导入后将其初始化为平地。
- 旧 EditorState 测试的空出生点保存预期需随新规则更新；本轮地形运行集成验证与该旧编辑器测试分开记录。

## 验证

参见 `Test.md`。所有命令使用 pwsh，唯一编译命令 `dotnet build ET.sln`。不要手工生成 .meta 或编辑 .csproj。
