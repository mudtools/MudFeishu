# Mud.Feishu.WebSocket.Tests

本测试项目包含 Mud.Feishu.WebSocket 项目的单元测试，覆盖了核心功能和组件。

## 测试状态

✅ **所有测试通过** - 208/208 测试成功 (100%)

最后优化日期: 2026年1月16日

详细的优化过程和问题解决方案请参见 [TEST_SUMMARY.md](TEST_SUMMARY.md)

## 测试框架

- **xUnit** - 测试框架
- **Moq** - Mock 框架
- **FluentAssertions** - 断言库

## 测试覆盖范围

### 1. 配置测试 (`Configuration/`)

#### FeishuWebSocketOptionsTests
测试 `FeishuWebSocketOptions` 配置类的各种属性和行为：
- 默认值验证
- 非法值由 `Validate()` 拒绝（`Reconnect.BaseDelayMs` / `Reconnect.MaxDelayMs` 交叉校验、`HeartbeatIntervalMs` 最小值等）
- 布尔值属性设置
- 整数属性设置

### 2. 核心组件测试 (`Core/`)

#### MessageRouterTests
测试 `MessageRouter` 消息路由器的消息分发功能：
- 处理器注册/注销
- 消息路由到正确的处理器
- v1.0 和 v2.0 消息格式处理
- 空消息和无效消息处理
- 多处理器场景
- 消息类型提取

#### SessionManagerTests
测试 `SessionManager` 会话管理器的功能：
- 会话 ID 设置和获取
- 会话持续时间计算
- 会话有效性验证
- 会话更新事件触发
- 会话重置
- 重连会话恢复

#### EventSubscriptionManagerTests
测试 `EventSubscriptionManager` 事件订阅管理器的功能：
- 事件类型订阅（单个和批量）
- 重复事件类型处理
- 订阅请求发送
- 订阅事件触发（成功/失败）
- 订阅清空

### 3. 处理器测试 (`Handlers/`)

#### JsonMessageHandlerTests
测试 `JsonMessageHandler` 基类的 JSON 序列化/反序列化功能：
- 无效 JSON 处理
- 空值和缺失属性处理
- 大型 JSON 处理
- 特殊字符处理
- Unicode 字符处理
- 数值类型处理
- 布尔值处理

#### HeartbeatMessageHandlerTests
测试 `HeartbeatMessageHandler` 心跳消息处理器：
- 消息类型识别（大小写不敏感）
- 有效心跳消息处理
- null 数据处理
- 无效 JSON 处理
- 不同状态值处理
- 零/负/超大时间戳处理
- 取消令牌处理

#### AuthMessageHandlerTests
测试 `AuthMessageHandler` 认证消息处理器：
- 认证成功/失败处理
- 错误码处理
- 空值和缺失属性处理
- 各种错误场景处理
- 回调函数调用验证

### 4. 数据模型测试 (`DataModels/`)

#### FeishuWebSocketMessageTests
测试 `FeishuWebSocketMessage` 基类：
- 默认值验证
- 属性设置和获取
- JSON 序列化/反序列化
- null 和空值处理

#### AuthMessageTests
测试 `AuthMessage` 认证消息：
- 构造函数行为
- 类型属性设置
- 数据属性设置
- 序列化/反序列化

#### AuthDataTests
测试 `AuthData` 认证数据：
- 属性设置和获取
- null、空值处理
- 数值范围处理
- 序列化/反序列化
- 真实 token 格式处理

### 5. 状态测试

#### WebSocketConnectionStateTests
测试 `WebSocketConnectionState` 枚举：
- 枚举值验证
- 枚举顺序
- 唯一性验证
- 字符串解析（区分/不区分大小写）
- 无效名称处理
- 类型转换

## 运行测试

### 使用 Visual Studio
1. 打开测试资源管理器（Test Explorer）
2. 选择要运行的测试
3. 点击"运行"按钮

### 使用 .NET CLI
```bash
# 运行所有测试
dotnet test

# 运行特定测试项目
dotnet test Tests/Mud.Feishu.WebSocket.Tests/Mud.Feishu.WebSocket.Tests.csproj

# 运行特定测试类
dotnet test --filter "FullyQualifiedName~MessageRouterTests"

# 运行特定测试方法
dotnet test --filter "FullyQualifiedName~MessageRouterTests.RegisterHandler_ShouldAddHandler_WhenHandlerIsValid"
```

### 使用命令行参数
```bash
# 显示详细信息
dotnet test --verbosity normal

# 生成代码覆盖率报告
dotnet test --collect:"XPlat Code Coverage"
```

## 测试原则

1. **单元测试** - 测试单个组件的功能
2. **独立性** - 每个测试应该独立运行
3. **可重复性** - 测试结果应该是可重复的
4. **快速执行** - 测试应该快速执行
5. **清晰性** - 测试名称和结构应该清晰易懂

## 添加新测试

1. 在相应的文件夹中创建新的测试文件
2. 继承测试命名约定：`{ClassName}Tests.cs`
3. 使用 Arrange-Act-Assert 模式组织测试
4. 为测试方法使用描述性名称：`{MethodName}_Should{ExpectedBehavior}_When{Condition}`
5. 使用 Moq 模拟依赖项
6. 使用 FluentAssertions 编写清晰的断言

## 最佳实践

- 使用 `Fact` 用于单次测试
- 使用 `Theory` 和 `InlineData`/`MemberData` 进行参数化测试
- 为异步方法使用 `async Task` 返回类型
- 使用 `CancellationToken.None` 作为默认参数（除非特别测试取消）
- 在构造函数中初始化共享对象
- 每个测试应该只测试一个行为
- 保持测试简洁和专注

## 持续集成

这些测试将在 CI/CD 流程中自动运行，确保代码质量和稳定性。
