# Brawl Demo 开发交接

更新日期：2026-10-14。此文件用于新会话恢复上下文，不代表授权立即实施所有待办。

## 当前目标与协作方式

- ET 上的荒野乱斗风格面试 Demo；Unity 2022.3.62f3，VS 2026。
- 先读根 AGENTS.md、harness 规范及目标包 AGENTS.md；命令使用 pwsh。
- 用户自己操作 Unity 测试；节省 token，不主动进行耗时游戏测试或读取整个资源目录/Excel。唯一编译入口 `dotnet build ET.sln`。
- 不手改 .meta / .csproj / Luban 生成代码。已有大量未提交改动（资源、配置、先前移动代码），不可清理或覆盖。此次没有提交 Git。
- 保留已有临时验证入口，不删除 `Brawl/Verification/RunTerrain`。

## 已有流程

- MongoDB 账号登录，首次登录自动注册（本地 Demo）；大厅英雄列表、详情和试用入口已存在。
- 训练场：本地展示，关卡 10101，场景读取配置；加载地形、英雄和 NPC 模型，无本地移动。正常登录仍需要服务器。
- 练习赛：玩法 102、关卡 10201，1 人一队、2 队、时限 0；Gate 建房，服务端创建玩家、固定公牛 Bot（英雄 201）及配置 NPC。
- 进入 RPC 返回单位快照，客户端依快照加载模型；退出释放房间、场景和资源。当前只有一个真实玩家，不是多人匹配实现。
- 网格索引 = row * ColumnCount + column；世界 X/Z = column/row * BrawlGlobal.CellSize，当前格长约定 1 米。美术缩放由用户处理，不再额外缩放 Prefab。

## 本轮完成

- 玩家和 Bot 的 BrawlMoveComponent.Radius 读取各自 BrawlHero.CollisionRadius，不再使用固定 0.5。
- 出生点分别使用玩家、Bot 配置半径检查，拒绝非有限或非正半径；不再以统一半径检查所有出生点。
- 摇杆常量：`Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/BrawlJoystickHelper.cs` 的 `DeadZone=0.2f`；HandleRange=60，对应中心 12 UI 局部单位。
- 死区内及边界停止，死区外归一化方向，全速移动；旋钮本身仍随手指位置显示。服务器也归一化非零输入，不使用模拟力度。
- 不重新归一化碰墙后的剩余位移，保留自然滑墙减速。
- 未修改 Excel 或重新导出；用户已完成配置导出。未新增协议、类、包依赖、异步流程或 Unity 物理碰撞。

## 移动现状与代码入口

statesync 包：

- `Scripts/Hotfix/Server/Brawl/BrawlPracticeHelper.cs`：建房、出生验证、创建移动组件。
- `Scripts/Hotfix/Server/Brawl/BrawlPracticeMovementHelper.cs`：输入、服务器固定步长、位置快照。
- `Scripts/Hotfix/Share/BrawlTerrainMovementHelper.cs`：XZ 圆形与地形格碰撞、边界、滑动。
- `Scripts/Model/Share/BrawlMoveComponent.cs`：方向、速度、半径、实际速度数据。
- `Scripts/Model/Share/BrawlMovementSettings.cs`：50ms Tick、500ms 输入超时、最多追赶 5 步、2 Tick 插值缓冲等。
- `Scripts/HotfixView/Client/YIUISystem/Lobby/`：BrawlJoystickHelper、BrawlPracticeClientComponentSystem、BrawlPracticeMovementViewHelper 等输入与表现逻辑。

服务器权威；WASD/摇杆发送意图，约 20Hz，起停及时发送。速度暂按 MoveSpeedTier 米/秒。服务端固定 50ms 步进，正常约 20Hz 快照，静止仍同步玩家/Bot；客户端插值，不预测、不外推、不控制 Animator。输入超时自动停止。Bot 有移动组件但无 AI。只阻挡不可摧毁墙、水、地图边缘；平地和草可通过。可摧毁墙是 NPC，当前故意可穿过。

协议：C2G_BrawlMoveInput 的 EntryId 区分进入批次、Sequence 为输入序号、X/Z 是方向而非位置。G2C_BrawlMoveSnapshot 的 RoomId/EntryId 防止旧房间消息污染、Tick 是模拟步编号、LastInputSequence 为服务器已接收输入序号（不等同逐条输入执行确认），Units 中 UnitId/X/Z 是权威位置。连续日志是状态同步产生，不一定是异常。

## 碰撞配置约定

- 英雄 CollisionRadius=0.3 米，CollisionHeight=1.2 米（脚底向上完整高度）；半径与高度独立。配置半径不再额外乘 CellSize 或模型缩放。
- 高度已导出，但当前平面移动不使用，也没有为此添加碰撞组件。
- NPC 表：CollisionLength / CollisionHeight / CollisionWidth / PivotX / PivotZ，float、双端字段。
- 已讨论约定：Width=0 为圆柱，Length 表示直径；Width>0 为长方体，Length/Width 为完整 X/Z 尺寸、Height 为完整 Y 高度。PivotX/Z 表示相对单位原点的碰撞中心偏移（米），不是世界坐标。该约定尚未实现碰撞消费逻辑，下一轮先确认用户未改变语义。
- 用户贴出的 NPC：101 墙 0.6×0.3、高 1；201 木桶直径 0.6、高 1；301 劫匪直径 0.6、高 1。英雄 101/201/301/401 均半径 0.3、高 1.2。以最新导出配置为准。

## 明确留待后续，不要擅自实现

1. 英雄与英雄、英雄与 NPC 的碰撞；阻挡与可摧毁应拆成独立组件，尺寸数据本身不等于阻挡能力。
2. NPC 几何形状/偏移消费、圆与盒碰撞、旋转约定；可摧毁墙销毁后解除阻挡。
3. Bot 移动 AI、寻路；先调查 ET 现有能力，沿用服务器权威移动入口。
4. 客户端预测、校正和真正多人房间广播。
5. 动画、上下身分层、技能、子弹、伤害、生命和木桩复活。
6. 动态单位出生/销毁通知、对局结算、时限、防消极对局机制及匹配。
7. 将本地训练场从展示升级为可玩的流程，需用户确定方案。

## 验证状态

本轮 `dotnet build ET.sln` 已通过：0 错误、14 个 HybridCLR CS0649 字段警告，耗时约 58 秒。不启动服务器、不执行 Unity Play 或自动化游戏测试，因此不宣称运行时回归已通过。
用户回归关注：1 米通道通过；墙/水/边缘停止和斜向滑动；摇杆死区内静止、死区外不同距离等速；松手/失焦停止；切英雄使用对应半径；仍能穿过 Bot/NPC。

## 本地启动提示

MongoDB 服务需先启动；在项目根目录、管理员 PowerShell 中运行：

```powershell
dotnet Bin/ET.App.dll --SceneName=StateSync --Process=1 --StartConfig=Localhost --Console=1
```

Unity CodeMode 使用 Client。编译若提示 DLL 占用，请用户在服务器窗口 Ctrl+C 停止，不擅自杀进程；MongoDB 不用停止。
