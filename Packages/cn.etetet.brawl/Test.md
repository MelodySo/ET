# 关卡编辑器验证清单

所有 Excel 读写测试使用 Temp 下唯一临时目录中的工作簿副本，不修改正式关卡数据。

1. 表头：十个字段（含 StyleId）；SceneAsset 分组 c；MapGrid、SpawnPoints、NPCIds、NPCPoints 四个数组导出为 int[]；没有 CellSize、ItemSpawnPoints。
2. 新建/保存：不存在的正整数 ID 可创建；同 ID 重复保存只更新一行；保存后仍选中同 ID。
3. 导入：按 ID 精确读取；缺失 ID、重复 ID、非法数字和损坏表头明确报错。
4. 保留数据：修改 SceneAsset/行列数不清空数组；保存一个关卡不改变其他关卡。
5. 草稿校验：数组可以为空；非空格子数组长度必须匹配尺寸；拒绝未知格子、越界/重复出生点、超出 Excel 单元格限制的数据。
6. 并发/锁：目标行被外部修改后拒绝覆盖；Excel 独占占用时保存失败且原文件保持完整；保存和导入后释放文件句柄。
7. 窗口状态：初始选择 0；新建/导入后选择目标 ID；保存保持 ID；退出清空；刷新/重编译保留序列化草稿。
8. 配置导出：ET.ExcelExporter 成功，客户端有 SceneAsset，服务端无 SceneAsset；双端有四个数组字段。
9. Unity 编译及窗口实际打开检查。地形运行验证见后文独立入口。

## 地形运行验证入口（保留）

- 打开 `Packages/cn.etetet.statesync/Scenes/Init.unity`，进入 Play，等待登录界面出现。
- 无需启动服务端或登录，点击 `ET/Brawl/Verification/RunTerrain`；不要在已有训练场会话中或验证过程中重复点击。
- 自动进入并退出 10101（Train）、10001（Forest）、10101，检查地形实例数量/位置、英雄模型出生格、NPC 数量/位置、UI 状态、场景卸载和句柄释放。
- 每轮成功输出 `BRAWL_TERRAIN_PASS`；前两轮相机截图保存在 `Temp/BrawlTerrain_10101.png` 和 `Temp/BrawlTerrain_10001.png`。
- 这是客户端真实资源流程的诊断工具，不覆盖登录、联网对战或打包后资源加载；验证后停止 Play。入口和验证实现按用户要求保留。
- 2026-10-12：Unity Client / Editor 模拟资源模式三轮通过：Train 12、Forest 84、Train 12 个地形实例；NPC=0，每轮退出场景已卸载且资源句柄归零。发现并修复了连续进出时大厅 CanvasGroup 延迟销毁的竞争问题。
- 本轮配置迁移：10001 的 64 个旧草丛编号 3 改为现行枚举 Grass=2；其他关卡数据未主动修改。备份为 `Temp/BrawlStyle-b77f18125fe948a7b4e737605bd5c1d5/before.xlsx`。Luban 两子表导出成功；仍有现存 TMP 中文缺字警告，未修改字体资源。

## 自动测试

### StyleId 分组回归

- 在工作簿临时副本上验证 StyleId 留空与 c 两种分组，均应允许导入、保存、回读，且保存不改分组行。
- StyleId=s、SceneAsset=s 和 MapGrid=c 仍应拒绝；错误信息指出具体字段和单元格。
- 使用 `Brawl_LevelWorkbook_Test` 执行，正式工作簿不修改。
- 同时调用真实 EditorWindow.ImportLevel，验证导入后无脏标记、退出清空选择但保留 PlayerPrefs、重建窗口恢复输入 ID 且不自动导入。测试后恢复用户原有 PlayerPrefs。
- 2026-10-13：UnityBridge 实测 Matched=1、Passed=1、Failed=0（约 41 秒），最终响应保存在 Temp/UnityBridge/responses/867193866.json；测试后恢复 Client 模式。

## NPC 笔刷手动验证（本轮未运行）

## 训练场角色模型验证（本轮未运行，交由用户测试）

- 分别选择雪莉、公牛进入训练场，应按英雄 ModelPath 显示对应 Prefab，保留原武器，位置在第一个玩家出生格，不再出现胶囊。
- NPC 数量与关卡 NPCIds 长度相同，模型来自 NPC 表 ModelPath，位置来自对应 NPCPoints；当前仅展示，不可攻击。
- 退出后换英雄再次进入，检查没有残留模型、大厅按钮可用；空 NPC 数组的关卡仍可进入。
- 保留原验证入口并更新模型/NPC 断言；本轮未执行编译或 Unity 测试，不将历史通过记录视为此次结果。

### NPC 编辑器检查

- 导入关卡，切换 NPC，输入实际 NPC Id 或下拉选择；名称、类型、基础生命值、模型地址只读。
- 左键单击放一个，同格换 ID 可替换；拖动不连续放置，右键单击删除；地形数据不变。
- 撤销/重做、保存/退出/重新导入后，NPC ID 和格子保持对应。
- 不能刷在墙、水面或玩家出生点上；刷好后改坏地形，保存应报错。
- 缩小地图裁剪范围外 NPC，范围内 NPC 坐标保持；可以撤销。
- 修改 NPC 源表后刷新下拉；无效 ID 不可刷，已摆放的 ID 被删后保存报错。
- NPCIds / NPCPoints 长度不等、位置重复/越界、ID 非正整数应拒绝；空数组允许。

## 原有自动测试

- `Brawl_LevelWorkbook_Test`：真实 Excel 往返、重复保存、跨行保留、冲突保护、锁和校验；额外覆盖逗号数组及纯数字字符串不得被自动转成数字。
- `Brawl_LevelEditorState_Test`：新建、保存后保留选择、重复保存、退出不保存、重新导入、序列化窗口草稿。
- 运行前使用项目 CodeMode 工具临时启用 ClientServer 的程序集引用并刷新 Unity（不启动 Play）。通过 UnityBridge 发送 `UnityTestRunRequest`，Name 为 `^Brawl_Level.*_Test$`。结束后恢复 Client 引用并刷新。
- 2026-10-10：两项测试通过（Matched=2 / Passed=2 / Failed=0），正式关卡表保持空表。
- 2026-10-10：完整 ET.ExcelExporter 导出成功，客户端有 SceneAsset、服务端无 SceneAsset，三个数组均生成为 int[]。
- 2026-10-10：dotnet build ET.sln 通过，0 错误；14 条警告来自未修改的 HybridCLR 字段。通过 Unity 已安装的 VS 插件重新生成旧工程引用，未手工修改 csproj。
