# cn.etetet.yiuieffect

## 概述

YIUI 特效与 UI 粒子相关包，包含运行时特效逻辑、资源引用和编辑器辅助代码。

## 规则

- 修改代码前先确认所属程序集和包依赖；不得跨包访问未显式声明的符号。
- UI 粒子对象池接口必须避免使用 Unity 生命周期消息名称，防止 Unity 分析器误判。
- 不手工生成或修改 `.meta`、`.csproj`；程序集引用和工程文件由 Unity 刷新生成。
- 涉及异步逻辑时，遵循 `cn.etetet.harness` 的 `et-async` 规范。
