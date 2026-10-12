# 关卡编辑器验证清单

所有 Excel 读写测试使用 Temp 下唯一临时目录中的工作簿副本，不修改正式关卡数据。

1. 表头：八个字段；SceneAsset 分组 c；三个数组导出为 int[]；没有 CellSize。
2. 新建/保存：不存在的正整数 ID 可创建；同 ID 重复保存只更新一行；保存后仍选中同 ID。
3. 导入：按 ID 精确读取；缺失 ID、重复 ID、非法数字和损坏表头明确报错。
4. 保留数据：修改 SceneAsset/行列数不清空数组；保存一个关卡不改变其他关卡。
5. 草稿校验：数组可以为空；非空格子数组长度必须匹配尺寸；拒绝未知格子、越界/重复出生点、超出 Excel 单元格限制的数据。
6. 并发/锁：目标行被外部修改后拒绝覆盖；Excel 独占占用时保存失败且原文件保持完整；保存和导入后释放文件句柄。
7. 窗口状态：初始选择 0；新建/导入后选择目标 ID；保存保持 ID；退出清空；刷新/重编译保留序列化草稿。
8. 配置导出：ET.ExcelExporter 成功，客户端有 SceneAsset，服务端无 SceneAsset；双端有三个数组字段。
9. Unity 编译及窗口实际打开检查。数组绘制/地图运行验证不属于本次范围。

## 自动测试

- `Brawl_LevelWorkbook_Test`：真实 Excel 往返、重复保存、跨行保留、冲突保护、锁和校验；额外覆盖逗号数组及纯数字字符串不得被自动转成数字。
- `Brawl_LevelEditorState_Test`：新建、保存后保留选择、重复保存、退出不保存、重新导入、序列化窗口草稿。
- 运行前使用项目 CodeMode 工具临时启用 ClientServer 的程序集引用并刷新 Unity（不启动 Play）。通过 UnityBridge 发送 `UnityTestRunRequest`，Name 为 `^Brawl_Level.*_Test$`。结束后恢复 Client 引用并刷新。
- 2026-10-10：两项测试通过（Matched=2 / Passed=2 / Failed=0），正式关卡表保持空表。
- 2026-10-10：完整 ET.ExcelExporter 导出成功，客户端有 SceneAsset、服务端无 SceneAsset，三个数组均生成为 int[]。
- 2026-10-10：dotnet build ET.sln 通过，0 错误；14 条警告来自未修改的 HybridCLR 字段。通过 Unity 已安装的 VS 插件重新生成旧工程引用，未手工修改 csproj。
