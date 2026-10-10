# Mud.Feishu.AI

Mud.Feishu 的 AI 模块：飞书工具集成、会话式事件处理、HITL（Human-in-the-Loop）审批管线。

## HITL（人工确认）契约

WP3 后，HITL 的批准状态所有权**单一化**到宿主授权器——SDK 侧不保留任何"待下线机制"（自研确认令牌已删除）。

### 三层分工

| 层 | 负责 | 不负责 |
| --- | --- | --- |
| **MAF 审批管线**（`ApprovalRequiredAIFunction`） | 写类工具的**调用前**拦截；批准只能来自宿主 | 非写工具的动态确认 |
| **宿主授权器**（`IToolExecutionAuthorizer`） | **批准状态的唯一所有者**：首次收到 `NeedsUserConfirmation` → 记为挂起；经批准通道拿到摘要后建立"已批准"上下文；下次同 `(tool, argsDigest, appKey, userId)` 调用返回 `Allowed` | 不得依赖 SDK 提供凭据 |
| **SDK 执行链**（`FeishuToolBinding`） | 咨询授权器、通知宿主、**中性拒绝**（不含任何凭据） | 不签发/不校验/不缓存批准状态 |

### 批准之后：续跑义务（R5-2 / R5-11 / R5-12）

批准**不等于**放行，且「批准了却没人继续跑」是有后果的：

- **续跑必须显式重建租户上下文**：续跑轮不在事件流内，`IFeishuToolContextAccessor` 的 `AsyncLocal`
  上下文不存在 ⇒ 写工具 fail-closed 结构化拒绝（禁止默认 appKey 兜底，TMA2-20）。
  **推荐用 SDK 闭环一次调用完成**：`FeishuAgent.RunApprovalContinuationAsync(...)`
  （加载会话 → 取框架记录的原始审批请求 → 重建工具上下文 → 经框架绑定层续跑 → 落库）；
  宿主仍需自行把返回文本投递给用户。
- **完全不续跑**：SDK 在事件层自愈——新的用户轮次会放弃该待确认项、摘除会话历史里的孤儿审批请求并清空
  框架的待审批记录，避免 MEAI `FunctionInvokingChatClient` 对整段入站历史做配对校验时抛
  `InvalidOperationException` 而把会话**永久毒化**；迟到的批准随后被框架绑定层丢弃（fail-closed）。

### 关键约束

- **SDK 不签发任何凭据**：删除了 `ToolConfirmationToken` 全套（签发/验签/HMAC/常数时间比较），批准状态由 `IToolExecutionAuthorizer` 在每次调用时被咨询。
- **未注册批准通道 = 降级为纯提示**（fail-closed）：模型只会收到"需要用户确认"，拿不到任何凭据，无法自批复。
- **执行链不放行任何未获 `Allowed` 的待确认调用（读、写工具同路径）**（R4-1）：`ApprovalRequiredAIFunction` 是 MEAI
  **纯标记类型**，其拦截只在 `FunctionInvokingChatClient` 内生效——宿主直调 `InvokeAsync` 或绕开该管线时，
  它**不会**阻止写工具执行。故 `AuthorizationDecision.NeedsUserConfirmation` 一律进入无令牌版挂起解析
  （通知宿主通道 + 中性拒绝），**不**存在"框架已批准 ⇒ 执行链放行"的旁路。
  ⇒ 宿主在 MAF 批准回调中**必须同步把授权器状态置为已批准**，否则表现为「框架已批准、执行链仍拒绝」。

### DI 生命周期约束（R4-6）

`FeishuAgent` 是 **Singleton**（对齐 AGENTS.md TMA-13）。SDK 自身的装配（`AddFeishuAgent`）
**不把根 `IServiceProvider` 传给 MAF**——工具与 guidance 在装配期已解析为实例，会话历史由显式
`InMemoryChatHistoryProvider` 提供。宿主若自行向 `FeishuAgent` 传入 `services`，
**经该容器解析的依赖必须是 Singleton/Transient**；Scoped 依赖会被单例钉住成为 Captive Dependency
（装配期可用 `BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true })` 兜底，
使错误注册在**启动期**暴露而非运行期漂移）。

### 接口

- `IFeishuToolApprovalChannel`：宿主批准通道（SDK 定义契约，宿主实现）。通知宿主有待确认的工具调用，携带参数摘要（已脱敏）。
- `IToolExecutionAuthorizer`：批准状态的唯一所有者。每次工具调用时被咨询。
- `ToolApprovalRequest`：待确认要素（不含令牌，只含参数摘要与原因）。
- `FeishuAgent.RunApprovalContinuationAsync`：批准回灌续跑的 SDK 闭环（R5-11；见上方续跑义务）。
- `IFeishuPendingApprovalStore` + `PendingApprovalSnapshot`（R7 / C4a）：待确认项的**持久化 / 查询 / 取消**契约
  （默认实现 `InMemoryPendingApprovalStore` 已由 `AddFeishuAgent` 注册）。执行链在通知宿主通道**之前**落快照；
  `RunApprovalContinuationAsync(..., pendingApprovalStore: store)` 启用幂等消费（同一 `RequestId` 只生效一次，
  过期项自动放弃、迟到批准丢弃）。快照**不含任何凭据**（由用例反射断言）。
- `IContextAssembler` / `ContextBudgets`（R7 / C2）：事件上下文的装配位与**预算单一源**；
  `ApprovalContextAssembler`（审批任务事件）与 `BitableRecordContextAssembler`（多维表格记录变更事件，
  Order = 200）把事件载荷转成结构化片段（带 untrusted 标注、超预算截断、缺字段降级为空片段）。
  宿主把装配器传给事件处理器的 `contextAssemblers` 参数即启用（不传则行为与既有完全一致）。
- `FeishuEventFact` / `FeishuEventKeys`（R7 / C2）：事件 → 装配器的**有序**事实载体（`Dictionary` 枚举顺序
  在契约上未定义 ⇒ 用它会让同一事件两次装配产出不同 prompt）。
- RAG-B 自建检索（R7 / C5）：`ICorpusSource` / `IVectorStore`（**宿主契约**）、`DocumentChunker`
  （结构优先切片：标题层级 → 空行块边界 → 定长窗口含重叠）、`TokenEstimator`（可解释的 token 近似）、
  `VectorRetriever`（`IRetriever` 的第二实现）、`CorpusIndexer`（拉取→切片→写入），
  DI 入口 `AddFeishuVectorKnowledge` / `AddFeishuCorpusIndexing`。向量库与嵌入由宿主提供（SDK 不引入依赖），
  `CorpusChunk.ScopeKey` 必须由实现方按它隔离（跨租户召回即数据泄露）。与 RAG-A（Aily）**并存**：两个检索面
  都用 `TryAddSingleton`，先注册者生效。
