# cn.etetet.test

## 概述

通用测试用例包，基于 `ConsoleMode.Test` 运行，按 `[Test]` 特性自动发现并分发到对应处理器执行。支持按包名与用例名的正则过滤批量执行。

## 测试 skill 入口

本文件只保留测试相关 skill 的轻量路由。harness 中的 `et-tdd`、`et-test-write`、`et-test-run` 只负责分流；命中测试任务后按需读取下面对应 `SKILL.md`，不要一次性加载全部测试规则。

### et-tdd - 测试驱动规范

**使用场景**：

- 使用测试驱动方式开发新功能或修复 Bug。
- 需要完整“需求 -> 测试方案 -> 测试用例 -> 实现 -> 编译 -> 运行 -> 回归”闭环。

**补读**：`Packages/cn.etetet.test/skills/et-tdd/SKILL.md`

### et-test-write - 测试编写入口

**使用场景**：

- 编写新的 `ATestHandler` 测试用例。
- 修改现有测试用例。
- 补 `Test.md` 或最小验证清单。

**补读**：`Packages/cn.etetet.test/skills/et-test-write/SKILL.md`

### et-test-run - 测试执行入口

**使用场景**：

- 执行测试（全部 / 指定用例）。
- 查看测试日志、分析失败原因。
- 验证修改后的代码是否通过目标测试或回归测试。

**补读**：`Packages/cn.etetet.test/skills/et-test-run/SKILL.md`

## 核心类

| 文件 | 说明 |
|------|------|
| `TestConsoleHandler.cs` | 控制台处理器，解析命令并进入测试模式 |
| `TestDispatcher.cs` | 按包名与处理器名正则筛选匹配的测试用例 |
| `ATestHandler.cs` | 测试基类，约定 `Handle(...)` 接口 |
| `FiberInit_TestCase.cs` | `TestCase` 场景初始化，每个用例均为全新服务器环境 |
| `TestArgs.cs` | 命令行参数定义 |
| `ITestHandler.cs` | 测试处理器接口 |

## 登录回归兼容

- TestHelper.CreateRobot 使用确定的合法账号名与测试专用密码，不再用空密码绕过正式认证。
- 账号功能测试归属 login 包；其中网络用例使用临时数据库配置，不能让测试账号写入正常游戏数据库。
