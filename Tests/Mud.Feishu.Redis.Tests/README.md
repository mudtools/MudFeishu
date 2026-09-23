# Mud.Feishu.Redis 测试项目

本项目包含 `Mud.Feishu.Redis` 组件的**单元测试（Moq 契约层）**。
命令语义类结论（Lua 脚本、TTL、`SET NX`、Sorted Set 裁剪、SCAN）**不在本工程验证**，见下文「真实 Redis 与显式跳过」。

## 真实 Redis 与显式跳过

- **命令语义类断言只认真实 Redis**（ADR-8）：Moq 桩只能验证"传给 StackExchange.Redis 的参数正确"，
  无法验证脚本语法、`ARGV` 顺序、`EXPIRE`/`SET NX`/`ZREMRANGEBYRANK` 语义与返回值转换。
- 集成测试工程：`Tests/Mud.Feishu.Redis.IntegrationTests`（已加入 `Mud.Feishu.slnx`）。
- 启用方式：设置环境变量 `MUDFEISHU_REDIS_TESTS=1`，且本机 Docker 可用（Testcontainers 启动 `redis:7`）：

  ```bash
  # Windows PowerShell
  $env:MUDFEISHU_REDIS_TESTS = "1"; dotnet test Tests/Mud.Feishu.Redis.IntegrationTests/Mud.Feishu.Redis.IntegrationTests.csproj
  ```

- **未启用时用例计为 skipped（不计为 passed）**：用例由 `RedisFact`/`RedisTheory` 在**测试发现期**置
  `Skip`，因此 TRX 中体现为 skipped。CI 在 ubuntu（自带 Docker）上启用；`scripts/verify-build.ps1`
  步骤 5 在 Docker 可用时断言 `total>0 / failed=0 / skipped=0`，不可用时输出覆盖缺口告警。

## 测试覆盖范围

### 1. 服务测试（Services）

#### RedisFeishuEventDistributedDeduplicatorTests

- 首次事件标记 / 重复事件（Completed、Processing）/ 处理中超时可恢复（Lua 返回值 0/1/2/3 的映射）
- Redis 异常（连接/超时）统一包装为 `FeishuRedisException`
- 事件处理状态查询、空事件 ID、自定义 TTL 支持
- 服务端时钟：脚本含 `redis.call('TIME')` 且不再下发 C# 端时间戳（`ARGV` 数量断言）

#### RedisFeishuNonceDistributedDeduplicatorTests

- 首次 Nonce 标记 / 重复 Nonce 检测（防重放）/ 空 Nonce
- 自定义 TTL：`StringSetAsync` 以 `When.NotExists` + 指定 TTL 调用（TTL 实参断言）
- 使用状态查询、Redis 连接失败、Redis 超时、手动移除

#### RedisFeishuSeqIDDeduplicatorTests

- 首次/重复 SeqID 标记（Lua 返回值 0/1）
- 处理状态查询、缓存数量、最大已处理 SeqID（含无数据时返回 0）
- Redis 连接失败处理

#### RedisTokenStoreTests / RedisUserTokenStoreTests

- 构造参数校验（null 连接、null 日志回落 `NullLogger`、自定义前缀）
- 令牌读写往返、缺失时返回 null
- `RedisUserTokenStore.ClearAllUsersAsync`：以 `AllUsersScanPattern` 异步 SCAN + 批量删除（键集合断言）

### 2. 配置测试（Configuration）

- `RedisOptionsTests`：默认值与自定义值、地址形态、超时边界
- `RedisOptionsBindingTests`：JSON 绑定、`EventCacheExpiration` 小于 1 分钟钳制、旧扁平键回填、`TimeSpan` 陷阱
- `RedisOptionsValidatorTests`：`IValidateOptions` 失败/成功路径
- `DeduplicationLegacyKeyTests` / `RedisDeduplicationIneffectiveKeysWarnTests`：双读回落与"配了但无效"告警

### 3. 健康检查测试（HealthChecks）

- `RedisHealthCheckTests`：健康（PING 成功）、`RedisException`、一般异常 → Unhealthy

### 4. 扩展与装配测试（Extensions）

- `RedisFeishuServiceBuilderExtensionsTests`：调用顺序守卫（在 `AddFeishuApp` 之后调用抛异常）、null 参数
- `SeqIdScopeKeyInferenceTests`：`scopeKey` 推断优先级（统一节 > `RedisOptions` > 默认应用 > 首应用 > 回落）

### 5. 契约守卫（ContractGuards）

- `RedisKeyLayoutContractGuards`：金标准键样例（双冒号 + `\:` 转义）、
  **模式与键同源属性测试（FsCheck）**、令牌前缀 Memory/Redis 逐字节一致、
  `rediss://` → TLS、连接选项装配、集成测试工程必须在 `Mud.Feishu.slnx` 内、无日志宿主解析不抛

## 测试技术栈

- **测试框架**: xUnit 2.9.3（`xunit.runner.visualstudio` 3.1.5；net6.0 用 2.8.2）
- **Mock 框架**: Moq 4.20.72
- **断言**: FluentAssertions 8.8.0
- **属性测试**: FsCheck / FsCheck.Xunit 2.16.6
- **测试运行器**: Microsoft.NET.Test.Sdk 18.0.1（net6.0 用 17.12.0）
- **代码覆盖**: coverlet.collector 6.0.4
- **集成测试**（独立工程）: Testcontainers.Redis 3.10.0

## 运行测试

```bash
# 运行所有单元测试（多 TFM；本机未装 .NET 6 时用 -f net8.0 指定）
dotnet test Tests/Mud.Feishu.Redis.Tests/Mud.Feishu.Redis.Tests.csproj

# 生成覆盖率报告
dotnet test Tests/Mud.Feishu.Redis.Tests/Mud.Feishu.Redis.Tests.csproj --collect:"XPlat Code Coverage"

# 运行特定测试类
dotnet test Tests/Mud.Feishu.Redis.Tests/Mud.Feishu.Redis.Tests.csproj --filter "FullyQualifiedName~RedisFeishuEventDistributedDeduplicatorTests"

# 契约守卫
dotnet test Tests/Mud.Feishu.Redis.Tests/Mud.Feishu.Redis.Tests.csproj --filter "Category=ContractGuard"
```

### 使用 Visual Studio

1. 打开测试资源管理器（Test Explorer）
2. 点击"运行所有测试"按钮
3. 查看测试结果与覆盖率

## 测试结构

```
Tests/Mud.Feishu.Redis.Tests/
├── Configuration/
│   ├── DeduplicationLegacyKeyTests.cs                # 旧键/统一节双读回落
│   ├── RedisDeduplicationIneffectiveKeysWarnTests.cs  # "配了但无效"告警
│   ├── RedisOptionsBindingTests.cs                    # 配置绑定与钳制
│   ├── RedisOptionsTests.cs                           # 默认值/边界
│   └── RedisOptionsValidatorTests.cs                  # 启动期校验
├── ContractGuards/
│   └── RedisKeyLayoutContractGuards.cs                # 键布局/模式同源/装配不变量
├── Extensions/
│   ├── RedisFeishuServiceBuilderExtensionsTests.cs    # DI 注册与调用顺序
│   └── SeqIdScopeKeyInferenceTests.cs                 # scopeKey 推断优先级
├── HealthChecks/
│   └── RedisHealthCheckTests.cs                       # 健康检查
├── Services/
│   ├── RedisConnectionWarmupServiceTests.cs           # 启动期连接预热
│   ├── RedisFeishuEventDistributedDeduplicatorTests.cs
│   ├── RedisFeishuNonceDistributedDeduplicatorTests.cs
│   ├── RedisFeishuSeqIDDeduplicatorTests.cs
│   ├── RedisTokenStoreTests.cs
│   └── RedisUserTokenStoreTests.cs
├── PerAppRedisTokenStoreKeyIsolationTests.cs          # per-app 令牌键隔离（TOK-1）
├── GlobalUsings.cs
├── Mud.Feishu.Redis.Tests.csproj
└── README.md
```

## 测试原则

1. **单元测试隔离**：使用 Mock 隔离外部依赖（Redis 连接），不依赖真实 Redis，执行快
2. **AAA 模式**：所有测试遵循 Arrange-Act-Assert
3. **命名规范**：`MethodName_Should{Behavior}_When{Condition}`（历史用例保留了 `..._Should...` 与 `..._ShouldReturnX` 两种风格）
4. **断言有效**：优先断言**关键值**（键、TTL、`ARGV`、HashEntry 内容），避免只 `Verify` 调用次数
5. **命令语义只认真实 Redis**：涉及 Lua/TTL/SCAN 语义的断言一律放集成测试工程

## 注意事项

- 所有单元测试使用 Mock 对象，**不需要**真实 Redis 服务器
- **本工程不存在**"Redis 故障自动降级到内存"的测试：组件**不提供**内置降级能力（降级策略由上层
  `NonceFailureMode` / WebSocket 异常处理决定）
- 集成测试的跳过不会被计为通过（见「真实 Redis 与显式跳过」）

## 贡献指南

添加新测试时，请遵循以下规范：

1. 使用清晰的测试方法命名
2. 添加必要的注释说明测试目的（尤其是"为什么"层面的回归背景）
3. 确保测试独立且可重复执行
4. 验证正常和异常情况；凡涉及 Redis 命令语义，请加到集成测试工程
5. 更新本 README 文档

## 许可证

本项目遵循 MIT 许可证。详见根目录的 LICENSE-MIT 文件。
