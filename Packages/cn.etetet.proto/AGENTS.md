# cn.etetet.proto

## 概述

协议定义与 Proto 编辑器工具包。

## 目录约定

- `Scripts/Model`：协议相关模型代码。
- `Scripts/Editor`：编辑器代码，通过 `.asmref` 汇入 `ET.Editor`。

## 开发约定

- 遵循 `cn.etetet.harness` 的 `/et-code` 与 `/et-build` 规则。
- 修改 Proto 导出流程时按项目 Proto 构建入口执行验证。
- 登录协议的 partial 扩展对 ToString 做脱敏，避免通用消息日志或 RPC 异常输出密码和 Gate Key；不改变 MemoryPack 传输字段。
- G2R_GetLoginKey 是服务端协议但生成命名空间为 ET，其 partial 扩展保留 ET，并仅对该文件标注 ET0112 例外。
