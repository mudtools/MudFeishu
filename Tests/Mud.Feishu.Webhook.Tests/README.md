# Mud.Feishu.Webhook 测试项目

本项目包含 Mud.Feishu.Webhook 组件的单元测试，用于验证 Webhook 事件处理功能的正确性和可靠性。

## 测试覆盖范围

### 1. 验证器测试 (Validators)

#### SignatureValidatorTests
测试飞书事件签名验证器：
- 请求头签名验证（有效/无效签名）
- SHA-256 签名计算（与飞书官方 SDK 兼容）
- 多应用配置继承（应用级强制验证开关）
- 固定时间比较（防止计时攻击）
- 空签名处理（强制验证模式/非强制验证模式）
- 生产环境/开发环境安全策略差异

#### CompositeFeishuEventValidatorTests
测试组合验证器的验证编排逻辑：
- 完整验证流程（时间戳→Nonce 检查→签名→Nonce 标记）
- **P1 修复核心测试：Nonce 消费时机**
  - 签名验证失败时 Nonce 不被标记为已使用
  - 签名验证通过后 Nonce 被正确标记
  - 并发场景下 Nonce 被其他请求标记的处理
- 时间戳验证失败时跳过后续验证
- Nonce 已被使用时跳过签名验证
- 订阅请求验证委托
- 异常处理（安全失败）

#### NonceValidatorTests
测试飞书事件 Nonce 验证器：
- Nonce 去重功能（标记已使用/检测重放攻击）
- Nonce 检查功能（仅检查不标记）
- 多应用 Nonce 隔离
- 降级策略（Reject 模式 / Allow 模式）
- 生产环境空 Nonce 拒绝
- 开发环境空 Nonce 允许

#### SubscriptionValidatorTests
测试飞书事件订阅验证器：
- 订阅请求验证（有效/无效 Token）
- 订阅请求类型验证
- Token 不匹配场景（固定时间比较，日志掩码）
- 缺失字段处理（空 Token / 空 Challenge / null 请求）
- 多应用场景验证

#### TimestampValidatorTests
测试飞书事件时间戳验证器：
- 有效时间戳验证
- 过期时间戳检测
- 时间戳容差范围
- 多应用配置继承

#### ConfigurationSupportTests
测试配置支持功能：
- 多应用配置解析
- 配置继承逻辑

### 2. 服务测试 (Services)

#### FeishuWebhookServiceTests
测试飞书 Webhook 核心服务：
- 事件订阅验证
- 事件数据处理
- 重复事件去重（幂等性）
- 事件解密
- 签名验证
- **去重回滚路径测试**
  - 事件处理异常时回滚去重状态
  - 事件处理成功时标记为已完成
  - 事件处理取消时回滚去重状态
- 多应用隔离（相同 EventId 不同 AppKey）
- 应用特定处理器和拦截器

#### FeishuEventDecryptorTests
测试飞书事件解密服务：
- V1.0 版本事件解密
- V2.0 版本事件解密
- 无效密钥处理
- 无效 Base64 数据处理
- 空数据处理
- 取消令牌支持

#### SecurityAuditServiceTests
测试安全审计服务：
- 安全事件记录
- 成功/失败事件审计

#### FeishuWebhookConcurrencyServiceTests
测试并发控制服务：
- 并发限制
- 超时处理

#### FailedEventRetryServiceTests
测试失败事件重试服务：
- 重试逻辑
- 指数退避

#### InMemoryFailedEventStoreTests
测试内存失败事件存储：
- 事件存储和检索
- 过期清理

### 3. 配置测试 (Configuration)

#### FeishuWebhookOptionsTests
测试 Webhook 配置选项：
- 默认值验证
- 自定义值设置
- 配置验证（超时、并发数、请求体大小、时间戳容差）
- 多应用配置验证

#### FeishuAppWebhookOptionsTests
测试应用级配置选项：
- 默认值验证
- 配置验证（AppKey、Token、EncryptKey）
- 配置继承逻辑（时间戳容差、超时、签名验证、异常处理、性能监控）

#### ConfigurationValidatorsTests
测试配置验证器：
- FeishuWebhookOptionsValidator 验证逻辑

#### ConfigurationValidatorIntegrationTests
测试配置验证器集成场景

#### RateLimitOptionsTests
测试限流配置选项

#### FailedEventRetryOptionsTests
测试失败事件重试配置选项

### 4. 中间件测试 (Middleware)

#### FeishuMultiAppMiddlewareTests
测试多应用中间件：
- 路由解析
- 请求处理流程
- 错误响应

#### FeishuRateLimitMiddlewareTests
测试限流中间件：
- 请求频率限制
- IP 白名单

#### IpAddressValidationTests
测试 IP 地址验证：
- IP 白名单匹配
- CIDR 格式支持

### 5. 其他测试

#### FeishuWebhookHealthCheckTests
测试健康检查端点

#### FeishuWebhookHandlerRegistryTests / FeishuWebhookInterceptorRegistryTests
测试处理器和拦截器注册表

#### FeishuWebhookModelTests
测试数据模型

#### FeishuWebhookExceptionTests
测试异常类型

## 测试技术栈

- **测试框架**: xUnit
- **Mock 框架**: Moq
- **断言库**: FluentAssertions
- **代码覆盖**: coverlet.collector

## 运行测试

### 使用 .NET CLI
```bash
# 运行所有测试
dotnet test Tests/Mud.Feishu.Webhook.Tests/Mud.Feishu.Webhook.Tests.csproj

# 运行测试并生成代码覆盖率报告
dotnet test Tests/Mud.Feishu.Webhook.Tests/Mud.Feishu.Webhook.Tests.csproj --collect:"XPlat Code Coverage"

# 运行特定测试类
dotnet test Tests/Mud.Feishu.Webhook.Tests/Mud.Feishu.Webhook.Tests.csproj --filter "FullyQualifiedName~CompositeFeishuEventValidatorTests"

# 运行单个测试
dotnet test Tests/Mud.Feishu.Webhook.Tests/Mud.Feishu.Webhook.Tests.csproj --filter "FullyQualifiedName~CompositeFeishuEventValidatorTests.ValidateHeaderSignatureAsync_WhenSignatureInvalid_ShouldReturnFalse_AndNotMarkNonceAsUsed"
```

### 使用 Visual Studio
1. 打开测试资源管理器（Test Explorer）
2. 点击"运行所有测试"按钮
3. 查看测试结果和覆盖率

## 测试结构

```
Tests/Mud.Feishu.Webhook.Tests/
├── Configuration/
│   ├── FeishuWebhookOptionsTests.cs           # Webhook 配置测试
│   ├── FeishuAppWebhookOptionsTests.cs         # 应用级配置测试
│   ├── ConfigurationValidatorsTests.cs         # 配置验证器测试
│   ├── ConfigurationValidatorIntegrationTests.cs # 配置验证器集成测试
│   ├── RateLimitOptionsTests.cs                # 限流配置测试
│   └── FailedEventRetryOptionsTests.cs         # 重试配置测试
├── Middleware/
│   ├── FeishuMultiAppMiddlewareTests.cs        # 多应用中间件测试
│   ├── FeishuRateLimitMiddlewareTests.cs       # 限流中间件测试
│   └── IpAddressValidationTests.cs             # IP 验证测试
├── Services/
│   ├── FeishuWebhookServiceTests.cs            # Webhook 服务测试
│   ├── FeishuEventDecryptorTests.cs            # 事件解密测试
│   ├── SecurityAuditServiceTests.cs            # 安全审计测试
│   ├── FeishuWebhookConcurrencyServiceTests.cs # 并发控制测试
│   ├── FailedEventRetryServiceTests.cs         # 失败重试测试
│   ├── InMemoryFailedEventStoreTests.cs        # 内存事件存储测试
│   └── TimestampValidatorTests.cs              # 时间戳验证器测试
├── Validators/
│   ├── SignatureValidatorTests.cs              # 签名验证器测试
│   ├── CompositeFeishuEventValidatorTests.cs   # 组合验证器测试
│   ├── NonceValidatorTests.cs                  # Nonce 验证器测试
│   ├── SubscriptionValidatorTests.cs           # 订阅验证器测试
│   ├── TimestampValidatorTests.cs              # 时间戳验证器测试
│   └── ConfigurationSupportTests.cs            # 配置支持测试
├── Registry/
│   ├── FeishuWebhookHandlerRegistryTests.cs    # 处理器注册表测试
│   └── FeishuWebhookInterceptorRegistryTests.cs # 拦截器注册表测试
├── Models/
│   └── FeishuWebhookModelTests.cs              # 数据模型测试
├── Health/
│   └── FeishuWebhookHealthCheckTests.cs        # 健康检查测试
├── Exceptions/
│   └── FeishuWebhookExceptionTests.cs          # 异常测试
├── Utils/
│   ├── RequestIdHelperTests.cs                 # 请求 ID 辅助工具测试
│   └── IpAddressHelperTests.cs                 # IP 地址辅助工具测试
├── Utilities/
│   ├── EnvironmentServiceTests.cs              # 环境服务测试
│   └── TimestampHelperTests.cs                 # 时间戳辅助工具测试
├── GlobalUsings.cs                             # 全局引用
├── TestWebhookAppKeyAccessor.cs                # 测试用 AppKey 访问器
├── Mud.Feishu.Webhook.Tests.csproj             # 项目文件
└── README.md                                   # 本文档
```

## 测试原则

1. **单元测试隔离**: 使用 Mock 对象隔离外部依赖
2. **AAA 模式**: 所有测试遵循 Arrange-Act-Assert 模式
3. **命名规范**: 测试方法命名格式为 `MethodName_Scenario_ExpectedBehavior`
4. **完整覆盖**: 覆盖正常流程、异常流程和边界条件
5. **快速执行**: 单元测试执行速度快，适合 CI/CD 集成

## 核心功能测试

### 事件验证
- ✅ 订阅请求验证（Token 和类型）
- ✅ 签名验证（SHA-256 头部签名，与飞书官方 SDK 一致）
- ✅ 时间戳验证（防重放攻击）
- ✅ Nonce 去重验证（两步验证：检查→标记）
- ✅ 组合验证器编排逻辑（时间戳→Nonce 检查→签名→Nonce 标记）

### 事件解密
- ✅ AES-256-CBC 解密
- ✅ V1.0 和 V2.0 版本支持
- ✅ 错误处理和日志记录
- ✅ 取消令牌支持

### Webhook 服务
- ✅ 事件订阅验证流程
- ✅ 事件处理流程
- ✅ 幂等性保证（去重）
- ✅ 去重回滚路径（异常/取消时回滚去重状态）
- ✅ 并发控制
- ✅ 超时处理
- ✅ 异常处理

### 安全加固
- ✅ Token 固定时间比较（防止计时攻击）
- ✅ 签名固定时间比较
- ✅ Nonce 消费时机修复（签名失败不消费 Nonce）
- ✅ 多应用安全隔离

## 注意事项

- 所有测试使用 Mock 对象，不需要真实的飞书服务器
- 测试覆盖了同步和异步方法
- 测试验证了异常处理和边界条件
- 加密测试使用模拟的 AES 加密算法

## 贡献指南

添加新测试时，请遵循以下规范：
1. 使用清晰的测试方法命名
2. 添加必要的注释说明测试目的
3. 确保测试独立且可重复执行
4. 验证正常和异常情况
5. 更新本 README 文档

## 许可证

本项目遵循 MIT 许可证。详见根目录的 LICENSE 文件。
