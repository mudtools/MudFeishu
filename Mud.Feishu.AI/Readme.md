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
