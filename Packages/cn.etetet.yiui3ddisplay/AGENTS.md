# cn.etetet.yiui3ddisplay

## 范围

- 通用 YIUI 三维展示组件；Runtime 的 UI3DDisplay 接收 UGUI 事件，ModelView 保存展示数据，HotfixView 的 YIUI3DDisplayChildSystem 管理 RT、相机、模型和交互。
- 保持独立于 Brawl、statesync 等业务包；业务模型加载、Idle 初始化与构图留在调用方。
- 优先复用 UI3DDisplay + YIUI3DDisplayChild。可以在 AddChild 前配置 m_ShowObject 和 m_LookCamera，由 Awake 接入已有展示对象；调用方持有其资源加载句柄并负责销毁模型。
- 展示层读取 YIUIConstHelper.Const.YIUI3DLayer，项目必须配置对应 Layer；禁止回退到未隔离的默认层。
- Unity 对象启停与 YIUI 启停都可能发生，RT 获取／释放必须幂等；销毁时须兼容 Unity 对象先于 Entity 销毁。
- m_DragDegreesPerWidth > 0 时按控件本地横向位移计算 Y 轴旋转，不再乘 deltaTime；默认 0 保留旧速度语义。m_FitScaleRoot 在 LateUpdate 持续抵消父节点缩放，兼容窗口尺寸变化。
- 不手写 .meta 或修改 .csproj；按用户要求，本轮不启动 Unity、不编译、不运行测试，待正常导入与人工验收。
