# AI-Native 实施进度与交接备忘（Phase 0 完成 / Phase 1 完成）

> **日期**：2026-09-27（本批次：Phase 1 收尾——工具执行链 + 10 只读工具 + 护城河硬验收）
> **关联**：[路线图](../../.docs/AI/AI-Native-Agent-Roadmap-v1.md) · [总体功能设计](../../.docs/AI/AI-Native-Agent-Functional-Design.md) · [Phase 0 设计](../../.docs/AI/AI-Native-Agent-Phase0-Design.md) · [Phase 1 设计](../../.docs/AI/AI-Native-Agent-Phase1-Design.md) · [lark-cli 对比备忘](./lark-cli-同场景对比备忘.md)

## Phase 0（Agent 底座）—— ✅ 全部 DoD 达成

| # | 任务 | 交付 | 状态 |
| --- | --- | --- | --- |
| T0-1 | 新建 `Mud.Feishu.AI` 项目 | csproj（四 TFM：ns2.0/net6.0/net8.0/net10.0） | ✅ |
| T0-2 | MAF 1.20.0 定版 + 单版本守卫 | `AgentContractGuards`（单版本 + 仅 AI 项目可引用 MAF 两守卫） | ✅ |
| T0-3 | `FeishuAgentOptions` + `Validate` | Options（ModelServiceKey/Instructions/Name/MaxHistoryMessages）+ `FeishuAgentOptionsValidator`（IValidateOptions + ValidateOnStart net6+） | ✅ |
| T0-4 | `ConversationKeyBuilder` + `IConversationStore` + Memory 实现 | 键布局 `feishu:{appKey}:conversation:{chat|user}:{id}`（转义对齐 TokenKeyBuilder）+ `Build/TryParse/ComposeNamespace`；契约四件（含 `SessionStoreEncoding`/`ConversationScope`）**位于 Mud.Feishu.Abstractions/Conversations**（纵向引用治理，2026-09-27 迁移） | ✅ |
| T0-5 | `RedisConversationStore` | 落于 **Mud.Feishu.Redis**（仅引用 Abstractions——纵向引用治理）；`{expireTimestampMs}|{payload}` 格式经 `SessionStoreEncoding` 单点实现，与 `TokenStoreHelper` 格式等价性有守卫锁定 | ✅ |
| T0-6 | `FeishuAgent` 骨架 | `sealed FeishuAgent : AIAgent` 组合 `ChatClientAgent`（sealed 约束）；`MessageCountingChatReducer` 消费 `MaxHistoryMessages`；`GetOrCreateSessionAsync`/`SaveSessionAsync` 会话持久化门面（损坏载荷按 miss 重建） | ✅ |
| T0-7 | `AddFeishuAgent` / `AddFeishuOpenAIChatClient` | 键控 IChatClient（解析即校验，不透传 null）+ 配置节绑定（源生成）+ `AddFeishuRedisConversationStore` 替换默认存储 | ✅ |
| T0-8 | OTel 飞书维度 Span | 复用 `FeishuActivitySource`（`Mud.Feishu`，宿主零配置）；`feishu.agent.name/operation`、`feishu.conversation.key`、`feishu.app_key`、`feishu.llm.input/output/total_tokens` | ✅ |
| T0-9 | 单测 + Demo + AOT 验证 | AI.Tests 78 用例 + Redis.Tests 会话存储用例全绿；`Demos/Mud.Feishu.Agent.Demo` 一行启动裸模型问答；AOT 结论见 [MAF-AOT-适配结论](./MAF-AOT-适配结论.md) | ✅ |

**AOT 结论**：MAF 1.20.0 在 `-p:AotStrictMode=true`（`--no-incremental`）下 0 `IL2026`/0 `IL3050`/0 `AOT00x`；`Mud.Feishu.AI` 保留 `IsAotCompatible`。

**版本对齐（重要沉淀）**：MAF 1.20.0 传递依赖要求 M.E.* ≥ 10.0.11，且 Binder 10.0.11 与 Configuration 必须同版本 lockstep（低版本 Configuration 会出现 `MissingMethodException: ConfigurationSection.TryGetValue`）。根 `Directory.Build.props` 两组（net8+/net6·ns2.0）M.E.* 全部统一到 **10.0.11**；`Mud.Feishu.OpenTelemetry` 的 Update 同步对齐；测试工程 `Microsoft.Extensions.Configuration` 8.0.0/10.0.9 → 10.0.11。

## Phase 1（最小闭环）—— ✅ 全部任务落地（2026-09-27 批次）

### 前批已完成（T1-1/2/3/8 基础层）

- **T1-1 事件基座**：`ConversationalFeishuEventHandler<T>`（继承 `IdempotentFeishuEventHandler<T>`，业务键 `feishu.agent.conversation:{EventId}` 命名空间化）+ `IContextAssembler` 插件 + `ConversationRequest`。
- **T1-2 特性**：`[FeishuTool(Name, Description, RequiredScopes, IsWrite)]`、`[ToolParameter]`、`[ToolHide]`。
- **执行授权链抽象**：`IToolExecutionAuthorizer` + `AuthorizationResult`（三态，与 Phase 3 HITL 契约统一）+ `FeishuToolContext`（缺 AppKey 即失败，TMA2-20）。
- **T1-8 注册表白名单**：`FeishuToolRegistry`（Register 不启用 / MapTool 显式启用）。
- **T1-3 `[FeishuTool]` 源生成器**（`Mud.Feishu.AI.Tools`，ns2.0 Roslyn 增量生成器）。

### 本批次交付（2026-09-27）

| # | 任务 | 交付 | 状态 |
| --- | --- | --- | --- |
| T1-3+ | 源生成器多工具支持修复 | 生成器重写为「投影为值相等模型 → `Collect` → 单次发射」：常量与 `SchemaByToolName` 全部只发射一份，10 个工具接口并存零重复成员；按工具名序数排序保证产物确定性 | ✅ |
| T1-4 | `FeishuToolBinding` 执行链 | **新包 `Mud.Feishu.AI.FeishuTools`**（引用 AI + core + DataModels，DAG 层级合法）：①上下文校验（缺 appKey 即失败）→ ②授权门禁（拒绝=不切租户上下文+零调用下游，fail-closed；写工具在 `EnforceToolAuthorization=true` 未注册授权器即拒绝）→ ③`IFeishuAppContextScopeFactory.BeginScope`（共享单例 `AsyncLocalAppContextSwitcher`，任意客户端切换全局生效；finally 释放）→ ④分域下游委托 → ⑤异常归一（结构化 `[tool_error] <tool>: <reason>`，取消即时传播）；OTel Span `feishu.agent.tool` 携带 `feishu.tool_name/app_key/tool.scopes/tool.decision/tool.write/tool.truncated` | ✅ |
| T1-5 | Bitable 三工具 | `bitable.list_tables`（`GetAppTablePageListAsync`）/ `bitable.list_fields`（`GetFieldsPageListAsync`）/ `bitable.query_records`（`QueryRecordsPageListAsync`）；**filter 简化文法解析器**（`= / contains`，`and` 连接 ≤5 子句，or/括号显式拒绝并回填可读原因）→ `RecordQueryFilterInfo`；`field_names` 列过滤绑定层落地；`page_size` 隐藏钳制、`page_token` 透传 | ✅ |
| T1-6 | Docx/Wiki/Search 四工具 | `docx.get_raw_content`（纯文本+截断）/ `wiki.get_node`（白名单 node_token/title/obj_type/obj_token/parent_node_token）/ `wiki.list_nodes` / `search.doc_wiki`（`search_in` → `DocFilter`/`WikiFilter` 扁平化构造，doc/wiki/both 三态 + 非法值结构化拒绝；query ≤30 字符钳制；白名单 title/url/owner/doc_type/token） | ✅ |
| T1-7 | IM/Sheets 三工具 | `im.get_history_messages`（RFC3339→秒级时间戳 `TryParseExact` 严格解析；`container_id_type=chat`、`sort_type=ByCreateTimeDesc` 绑定层注入；单条 content 预览截断）/ `sheets.list_sheets` / `sheets.get_range_values`（值网格逐格 JsonNode 投影） | ✅ |
| T1-8 收尾 | 白名单配置面 + 工具源桥 | `FeishuAgentOptions` 新增 `Tools`（**MapTool 配置面等价物**，未知名字 fail-fast）/ `MaxToolResultLength` / `EnforceToolAuthorization`（三属性消费点均在 FeishuTools 包，守卫锁定）；`FeishuAgentToolSource` 抽象（AI 定义、工具包派生、`AddFeishuAgent` 聚合进 `ChatOptions.Tools`）+ `FeishuToolAIFunction`（Schema 取编译期常量，`JsonElement.Clone()` 零反射）；注册扩展 `AddFeishuReadonlyTools`（10 工具全注册、默认不启用） | ✅ |
| T1-9 | OTel LLM/Tool Span | LLM Span 前批已落；工具 Span 本批随执行链落地（见 T1-4） | ✅ |
| T1-10 | 护城河硬验收 + 单测 | `Tests/Mud.Feishu.AI.FeishuTools.Tests` **55 用例 × 2 TFM 全绿**：①双 appKey `BeginScope` 切换/先于调用/finally 释放断言；②authorizer Deny 下游零调用 + 不切租户上下文；③scopes/decision 进 OTel Span 属性；分域映射单测（filter 文法、search_in 三态、RFC3339、page_size 钳制、code!=0 回填、截断标记、page_token 透传）；AIFunction 桥接（缺上下文结构化拒绝）；**工具名契约表守卫**（Schema 注册表恰为 §3.3.2 的 10 名 + 全只读 + scope 存在性 + 源码方法名隔离）；配置面守卫（三属性真实消费点） | ✅ |
| T1-11 | lark-cli 对比备忘 | [lark-cli-同场景对比备忘](./lark-cli-同场景对比备忘.md)（查记录同场景：延迟/权限模型/类型安全三维度 + 一页结论） | ✅ |
| 附 | Demo 工具模式 | `Demos/Mud.Feishu.Agent.Demo` 双模式：默认裸模型；`FEISHU_DEMO_TOOLS=1` 注册六域客户端 + 10 只读工具全启用，交互式端到端（模型 → tool_call → 执行链 → 回答） | ✅ |

**全量验证（2026-09-27）**：整包构建（4 TFM）0 错误；`Mud.Feishu.AI` 与 `Mud.Feishu.AI.FeishuTools` `-p:AotStrictMode=true --no-incremental` 均 0 `IL2026`/0 `IL3050`/0 `AOT00x`；AI.Tests 78 + FeishuTools.Tests 55（×2 TFM）+ Redis.Tests 203 全绿；`dotnet format` 对本批次项目已收敛。

### 实施偏差记录（与设计文档对照）

| 偏差 | 依据 |
| --- | --- |
| `FeishuAgentOptions` 未含 `MaxTurnCount`/`SlowModelWarning` | R5 规则 2（每个公开配置属性必须有真实消费点）：MAF 1.20.0 无 max-turns 旋钮；慢调用告警无消费点。`EnforceToolAuthorization` 本批已接线（消费点=执行链写工具门禁） |
| `IConversationStore` 存「序列化会话 JSON 字符串」而非 `AgentSession` 对象 | Phase 0 §3.3「Store 只做存原始字符串 + TTL」；序列化归属持有 Agent 的 `FeishuAgent` |
| 会话契约四件迁至 Abstractions；`SessionTtl` 移出为 `FeishuConversationOptions` | 包间只允许纵向引用；与 `FeishuDeduplicationOptions` 同模式；AI.Tests 有纵向引用守卫 |
| 样例工具接口 `IFeishuBitableQueryTool` 从 `Mud.Feishu.AI` **迁至 AI.Tests** | 源生成器在**每个**引用它的编译中产出同名 `FeishuToolSchemas`——生产程序集若含工具接口，下游同时引用 AI 与 FeishuTools 会因同名类型 CS0433 歧义；样例随测试编译产出，生产面无歧义 |
| 生成器重写为 `Collect` 单次发射 | 原实现每个工具接口各发射一份 `SchemaByToolName`，多工具并存即重复成员编译失败（T1-3 遗留缺陷，本批修复） |
| `bitable.list_fields` 映射 `IFeishuTenantV1BitableField.GetFieldsPageListAsync` | Phase 1 设计 v1.1 所述「Field 接口 `QueryRecordsPageListAsync` 方法名复用瑕疵」在当前源码已不存在（Field 域现为语义正确的 `GetFieldsPageListAsync`），工具名仍按契约表强制 `bitable.list_fields` |
| 工具结果投影走 **JsonNode 手工白名单**，不引用 DataModels 源生成 JSON 上下文 | DataModels 的 `XxxJsonContext` 为 internal 且仅 net8+ 编译，跨包不可用；JsonNode 手工投影全 TFM 一致且 AOT 净零。代价：`bitable.list_fields` 的复杂嵌套 `property` 对象不回填（Phase 2 评估） |
| `JsonArray.Add<T>` 经 `AddNode` 扩展绕行 | 泛型 `Add<T>`（T 为 JsonObject 等非原生类型）带 `RequiresDynamicCode/RequiresUnreferencedCode` 注解，AotStrictMode 下报 `IL2026/IL3050`；改走 `IList<JsonNode>` 显式接口实现（无注解，AOT 安全） |
| RFC3339 解析用 `TryParseExact` 而非 `TryParse+AssumeUniversal` | 宽松解析会把 `2026/09/27 00:00` 之类非 RFC3339 值按本地时区折算产生静默偏移；严格格式要求显式时区（`Z`/`±HH:mm`） |
| `IFeishuToolContextAccessor`（AsyncLocal）注入会话上下文 | 模型 tool_call 由 MAF 在 `RunAsync` 调用链内自动执行，工具实例为单例；上下文经事件入口 `Begin`（`ConversationalFeishuEventHandler` 已接线）/演示宿主手工 `Begin`，异步流读取。多租户隔离不允许默认 appKey 兜底 |
| 工具暴露经 `FeishuAgentToolSource` 抽象而非直接注册 `AIFunction` 服务 | `AddFeishuAgent` 聚合全部工具源产出 `ChatOptions.Tools`；避免 DI 多实现聚合歧义，AI 不反向依赖工具包（依赖方向铁律） |

## 下一批次（Phase 2 待启动项）

> **（2026-09-27 更新）Phase 2 已落地**，进度与偏差记录见 [AI-Native-实施进度-Phase2](./AI-Native-实施进度-Phase2.md)。以下为当时规划快照，仅存档。

1. **T2-1/2 流式回复**：`IMessageChannel` + `IFeishuTenantV1Message.EditMessageAsync` 分片编辑降级实现；`FeishuAgent.RunCoreStreamingAsync` 已具备桥接点（Phase 0 骨架）。
2. **T2-3 会话裁剪 + 摘要**（`ConversationSummarizer`，渐进式摘要缓存进 session）。
3. **T2-4/5 写工具 + 授权器真实消费**：`IsWrite` 源生成标记已就绪；`EnforceToolAuthorization` 门禁已在本批执行链内生效（未注册授权器默认拒绝）——Phase 2 补写工具清单与白名单键控（`FeishuAgent:Tools:WriteAllowList`）。
4. **T2-6 RAG-A**：`IFeishuV1AilyDataKnowledge` 已有；`AilyKnowledgeProvider` 门面 + `IFeishuKnowledgeBase`/`IRetriever`（Phase 2 §3.4）。
5. **MCP 消费侧评估备忘**（已决策⑧：Phase2+ 可选评估，不进 DoD）。
