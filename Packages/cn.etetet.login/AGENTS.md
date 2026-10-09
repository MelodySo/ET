# cn.etetet.login

## 职责与边界

- 本包负责 Realm 账号认证、Gate 登录凭据、在线玩家身份及客户端登录连接。
- 账号持久化使用分区配置中的 MongoDB；不在此包加入英雄、背包或战斗规则。
- Model 保存数据与策略常量，Hotfix 的 Helper / System 实现业务。
- 数据库操作和 RPC 后访问 Entity 必须使用 EntityRef 重新获取。
- 密码不能明文落库，也不能写入日志或错误消息。
- 当前仅为本地 Demo：首次合法登录自动创建账号，已存在账号必须验证密码。
- 账号规范见 LoginAccountPolicy：标准化为小写，密码不裁剪；MongoDB 唯一索引保证并发创建不覆盖密码。
- Realm 初始化 DBManagerComponent 和 LoginAccount 唯一索引，使用 StartZoneConfig 的连接和分区库名。
- 密码保存 PBKDF2-SHA256 哈希、随机盐和迭代次数；KDF 在工作线程计算，不访问 Entity。
- Realm 将数据库账号 ID 通过 R2G_GetLoginKey 传给 Gate；Gate 使用该 ID 创建 Player，Key 只能消费一次。
- 不改变大厅和进入地图协议；不提供找回密码、正式注册或公网认证服务。
- 业务协议修改后使用项目 Proto 导出器生成，不手工修改生成代码或 csproj。

## 测试

- 验证清单见 Test.md，功能测试放在本包 Scripts/Hotfix/Test。
- 使用独立测试数据库；禁止清理正常运行数据库。
- 编译入口：dotnet build ET.sln。
- 自测入口：`"Test --Name=^Login_Account.*_Test$" | dotnet ./Bin/ET.App.dll --SceneName=Test --StartConfig=Localhost`。
- Login_AccountNetwork_Test 独占运行，临时替换测试进程内分区配置，销毁场景后恢复并清理随机前缀测试库。
