# AI-Native 实施进度与交接备忘（Phase 0 完成 / Phase 1 进行中）

> **日期**：2026-09-27
> **关联**：[路线图](../../.docs/AI/AI-Native-Agent-Roadmap-v1.md) · [总体功能设计](../../.docs/AI/AI-Native-Agent-Functional-Design.md) · [Phase 0 设计](../../.docs/AI/AI-Native-Agent-Phase0-Design.md) · [Phase 1 设计](../../.docs/AI/AI-Native-Agent-Phase1-Design.md)

## Phase 0（Agent 底座）—— ✅ 全部 DoD 达成

| # | 任务 | 交付 | 状态 |
| --- | --- | --- | --- |
| T0-1 | 新建 `Mud.Feishu.AI` 项目 | csproj（四 TFM：ns2.0/net6.0/net8.0/net10.0） | ✅ |
| T0-2 | MAF 1.20.0 定版 + 单版本守卫 | `AgentContractGuards`（单版本 + 仅 AI 项目可引用 MAF 两守卫） | ✅ |
| T0-3 | `FeishuAgentOptions` + `Validate` | Options（ModelServiceKey/Instructions/Name/MaxHistoryMessages/SessionTtl）+ `FeishuAgentOptionsValidator`（IValidateOptions + ValidateOnStart net6+） | ✅ |
| T0-4 | `ConversationKeyBuilder` + `IConversationStore` + Memory 实现 | 键布局 `feishu:{appKey}:conversation:{chat|user}:{id}`（转义对齐 TokenKeyBuilder）+ `Build/TryParse/ComposeNamespace`；契约四件（含 `SessionStoreEncoding`/`ConversationScope`）**位于 Mud.Feishu.Abstractions/Conversations**（纵向引用治理，2026-09-27 迁移） | ✅ |
| T0-5 | `RedisConversationStore` | 落于 **Mud.Feishu.Redis**（仅引用 Abstractions——纵向引用治理）；`{expireTimestampMs}|{payload}` 格式经 `SessionStoreEncoding` 单点实现，与 `TokenStoreHelper` 格式等价性有守卫锁定 | ✅ |
| T0-6 | `FeishuAgent` 骨架 | `sealed FeishuAgent : AIAgent` 组合 `ChatClientAgent`（sealed 约束）；`MessageCountingChatReducer` 消费 `MaxHistoryMessages`；`GetOrCreateSessionAsync`/`SaveSessionAsync` 会话持久化门面（损坏载荷按 miss 重建） | ✅ |
| T0-7 | `AddFeishuAgent` / `AddFeishuOpenAIChatClient` | 键控 IChatClient（解析即校验，不透传 null）+ 配置节绑定（源生成）+ `AddFeishuRedisConversationStore` 替换默认存储 | ✅ |
| T0-8 | OTel 飞书维度 Span | 复用 `FeishuActivitySource`（`Mud.Feishu`，宿主零配置）；`feishu.agent.name/operation`、`feishu.conversation.key`、`feishu.app_key`、`feishu.llm.input/output/total_tokens` | ✅ |
| T0-9 | 单测 + Demo + AOT 验证 | AI.Tests 72 用例 + Redis.Tests 8 会话存储用例全绿；`Demos/Mud.Feishu.Agent.Demo` 一行启动裸模型问答；AOT 结论见 [MAF-AOT-适配结论](./MAF-AOT-适配结论.md) | ✅ |

**AOT 结论**：MAF 1.20.0 在 `-p:AotStrictMode=true`（`--no-incremental`）下 0 `IL2026`/0 `IL3050`/0 `AOT00x`；`Mud.Feishu.AI` 保留 `IsAotCompatible`。

**版本对齐（重要沉淀）**：MAF 1.20.0 传递依赖要求 M.E.* ≥ 10.0.11，且 Binder 10.0.11 与 Configuration 必须同版本 lockstep（低版本 Configuration 会出现 `MissingMethodException: ConfigurationSection.TryGetValue`）。根 `Directory.Build.props` 两组（net8+/net6·ns2.0）M.E.* 全部统一到 **10.0.11**；`Mud.Feishu.OpenTelemetry` 的 Update 同步对齐；测试工程 `Microsoft.Extensions.Configuration` 8.0.0/10.0.9 → 10.0.11。

## Phase 1（最小闭环）—— 已落地基础层，余项见下

### 已完成

- **T1-1 事件基座**：`ConversationalFeishuEventHandler<T>`（继承 `IdempotentFeishuEventHandler<T>`，业务键 `feishu.agent.conversation:{EventId}` 命名空间化）+ `IContextAssembler` 插件（多注册、按 Order 排序、单装配器失败异常隔离）+ `ConversationRequest`。回复通道 `ReplyAsync` 为抽象（派生类选择 `IFeishuTenantV1Message.ReplyMessageAsync` 等通道；AI 项目暂不引用 core 项目）。
- **T1-2 特性**：`[FeishuTool(Name, Description, RequiredScopes, IsWrite)]`、`[ToolParameter]`（参数级/方法级两种标注形态均支持）、`[ToolHide]`。
- **执行授权链抽象**：`IToolExecutionAuthorizer`（钩子，SDK 不内建策略）+ `AuthorizationResult`（**三态** Allowed/Denied/NeedsUserConfirmation——与 Phase 3 HITL 契约统一，不另立类型）+ `FeishuToolContext`（缺 AppKey 即失败，TMA2-20）。
- **T1-8 注册表白名单**：`FeishuToolRegistry`（Register 不启用 / MapTool 显式启用 / `FeishuToolDefinition` 含 scopes·IsWrite·Handler）。
- **T1-3 `[FeishuTool]` 源生成器**（`Mud.Feishu.AI.Tools`，netstandard2.0 Roslyn 增量生成器，以 `OutputItemType="Analyzer"` 挂入 `Mud.Feishu.AI`）：编译期产出 OpenAI-compatible JSON Schema（`name/description/parameters{type,description}/required` + `x-feishu.required_scopes`/`is_write`）为**字符串常量**（静态类 `Mud.Feishu.AI.Tools.Generated.FeishuToolSchemas`，运行期零反射，AOT 净零）；可空参数不进 `required`；样例工具 `IFeishuBitableQueryTool`（bitable.query_records）闭环验证（生成产物契约测试 3 项全绿）。

### 待做（下一工作批次）

1. **T1-4/5/6/7 `FeishuToolBinding` + 10 只读工具**：**不能**放入 Mud.Feishu.AI——纵向引用治理下 AI 不得引用 core `Mud.Feishu`（强类型接口载体）。需新建上层包（如 `Mud.Feishu.AI.FeishuTools`，引用 AI + core，DAG 层级合法）承载分域工具实现与执行链；分域映射要点（§3.3.3）：Bitable `filter` 简化文法解析器、Search `search_in`→`DocFilter/WikiFilter` 构造、IM RFC3339→秒级时间戳、`FeishuApiResult<T>` 解包 + 白名单投影 + `MaxToolResultLength` 截断 + `page_token` 透传；执行顺序 `BeginScope(ctx.AppKey)` → 授权 → 调用 → 裁剪。工具接口按 §3.3.2 清单补齐其余 9 个（生成器已就绪，逐接口标注即可）。
2. **T1-9/10 护城河硬验收用例**：双 appKey `BeginScope` 租户上下文断言、authorizer Deny 下游零调用断言、scopes/decision 进 OTel 属性（§7 三项）。
3. **T1-11 lark-cli 同场景对比备忘**（延迟/权限/类型安全三维度 → `documents/`）。
4. 工具名契约表守卫（10 个工具名与 §3.3.2 逐一相等）。
5. FeishuAgentOptions 接线 `EnforceToolAuthorization`（写工具进 Phase 2 执行链时）与 `FeishuAgent:Tools` 白名单配置（MapTool 的配置面等价物，T1-8 收尾）。

### 实施偏差记录（与设计文档对照）

| 偏差 | 依据 |
| --- | --- |
| `FeishuAgentOptions` 未含 `MaxTurnCount`/`EnforceToolAuthorization`/`SlowModelWarning` | R5 规则 2（每个公开配置属性必须有真实消费点）：MAF 1.20.0 无 max-turns 旋钮；授权强制属 Phase 2 执行链消费；慢调用告警无消费点。各属性接线时同批加回 |
| `IConversationStore` 存「序列化会话 JSON 字符串」而非 `AgentSession` 对象 | Phase 0 §3.3「Store 只做存原始字符串 + TTL」；序列化归属持有 Agent 的 `FeishuAgent`（Memory/Redis 两端行为逐字节一致） |
| 会话契约四件（`IConversationStore`/`ConversationKeyBuilder`/`ConversationScope`/`SessionStoreEncoding`）**迁至 Mud.Feishu.Abstractions/Conversations**；`SessionTtl` 自 `FeishuAgentOptions` 移出为 **`FeishuConversationOptions`**（Abstractions/Configuration，配置节 `FeishuConversation`） | **包间只允许纵向引用**（各包 → Abstractions），禁止横向引用——Redis 曾引用 AI 即违反；与 `FeishuDeduplicationOptions`（Memory/Redis 双后端共享配置）同模式；AI.Tests 增 `ImplementationPackages_ShouldOnlyReferenceAbstractions_VerticalDependencyOnly` 守卫锁定 |
| `FeishuToolBinding` 分域实现规划改为上层包承载 | 同上：AI 不得引用 core，10 工具的强类型接口在 core `Mud.Feishu` 内 |
| `AddFeishuAgent` 增加 `IConfiguration` 显式参数重载 | 对齐 `FeishuMultiAppExtensions` 既有模式；避免 `Configure<IConfiguration>` 依赖注入形态在无 IConfiguration 的纯 DI 宿主中解析失败 |
