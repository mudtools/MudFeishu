# Mud.Feishu.AotVerification

MudFeishu Native AOT 兼容性验证工程

## 用途

此项目用于验证 MudFeishu 库在 .NET Native AOT 环境下的兼容性，确保:
- JSON 序列化/反序列化在 AOT 下正常工作
- protobuf-net 二进制序列化在 AOT 下正常工作
- HTTP 客户端在 AOT 下正常工作
- 事件处理机制在 AOT 下正常工作

## 构建和验证

```bash
# 构建 AOT 版本
dotnet publish -r win-x64 -c Release /p:PublishAot=true
dotnet publish -r linux-x64 -c Release /p:PublishAot=true

# 运行验证
./bin/Release/net8.0/win-x64/publish/Mud.Feishu.AotVerification
./bin/Release/net8.0/linux-x64/publish/Mud.Feishu.AotVerification
```

## 验证阶段

### 阶段 0 — 基础架构 ✅
- ✅ AOT 基础架构验证
- ✅ 项目结构验证
- ✅ 编译通过验证

### 阶段 1 — P0 致命缺陷修复验证 ✅
- ✅ JSON 源生成上下文验证（FeishuJsonContext 含 FeishuEventHeader）
- ✅ 用户自定义 Context 合并验证（FeishuJsonDefaults.ConfigureUserResolver）
- ✅ Widget 多态序列化验证
- ✅ EventData 序列化/反序列化验证
- ✅ FeishuApiResultJsonContext 注册认证接口 DTO 类型
- ✅ P0-2: FeishuEventHeader 反序列化验证（通过 FeishuJsonContext.Default 强类型路径）
- ✅ P0-1: FeishuApiResult<GetUserDataResult> 闭合泛型反序列化验证
- ✅ P0-1: FeishuApiResult 非泛型基类反序列化验证

### 阶段 2 — P1 高风险缺陷修复验证 ✅
- ✅ protobuf-net AOT 验证
- ✅ WebSocketJsonContext 注册 9 个协议消息类型
- ✅ EventCallbackJsonContext 生成 10 个域 Context（覆盖 80+ 事件类型）
- ✅ AuthenticationManager 反序列化使用 DeserializerOptions
- ✅ AddFeishuApp<TAppManager> 添加 [DynamicallyAccessedMembers] 注解
- ✅ FeishuAppManager.CreateAppContext 改用 DI 解析消除反射
- ✅ Demo 项目移除 Swashbuckle 依赖
- ✅ P1-1: WebSocket AuthResponseMessage / PingMessage 反序列化验证（通过 resolver 链）
- ✅ P1-2: EventCallback MessageReceiveResult 反序列化验证（通过 resolver 链）
- ✅ P1-2: DriveFileEventHeader 子类反序列化验证（N-03 联动）
- ✅ ConfigureWebSocketResolver() 调用验证
- ✅ ConfigureEventCallbackResolver() 调用验证

### 阶段 3 — P2 工程化加固验证 ✅
- ✅ Directory.Build.props 全局 AOT 配置（EnableAotAnalyzer/EnableTrimAnalyzer/TrimMode）
- ✅ Webhook 旧版 AspNetCore.Http 改为 FrameworkReference（net6.0+ 统一使用 FrameworkReference）
- ✅ FeishuJsonDefaults 非 AOT 路径添加 [RequiresUnreferencedCode] + [RequiresDynamicCode] 注解
- ✅ rd.xml 兜底文件（Abstractions + DataModels）
- ✅ 各 csproj 移除 IsAotCompatible 重复声明，统一由 Directory.Build.props 管控
- ✅ protobuf-net.BuildTools 版本对齐 3.2.56
- ✅ WebSocket JsonOptions 增加 Deserializer 字段（R-06 修正）

### 阶段 4 — CI 标准化（待实现）
- ⬜ CI 增加 win-x64 + linux-x64 双 RID AOT 发布验证
- ⬜ 引入 AotStrictMode 将 IL2xxx 警告升级为错误
- ⬜ 发布配置 .pubxml 标准化
- ⬜ 根 README 增加 AOT 部署前置条件章节

## 验证用例清单

| 编号 | 验证项 | 对应缺陷 | 状态 |
|---|---|---|---|
| [1] | 基础设施验证 | — | ✅ |
| [2] | DataModels Context 接线（含 WebSocket + EventCallback） | P0-1/P1-1/P1-2 | ✅ |
| [3] | JSON 序列化选项验证 | — | ✅ |
| [4] | Webhook 响应 DTO 序列化 | G3 | ✅ |
| [5] | EventData 序列化 | B9 | ✅ |
| [6] | protobuf-net 类型验证 | P1 | ✅ |
| [7] | Widget 多态序列化 | G4 | ✅ |
| [8] | FeishuEventHeader 反序列化（强类型） | P0-2 | ✅ |
| [9] | FeishuApiResult<T> 反序列化（强类型） | P0-1 | ✅ |
| [10] | WebSocket 协议消息反序列化（resolver 链） | P1-1 | ✅ |
| [11] | EventCallback 事件反序列化（resolver 链） | P1-2 | ✅ |
