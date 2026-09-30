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
- **`AuthorizationDecision.NeedsUserConfirmation && tool.IsWrite → Pass()`**：MAF 已批准则执行链不得二次拦截（P4-1 死胡同修复，必须保留）。

### 接口

- `IFeishuToolApprovalChannel`：宿主批准通道（SDK 定义契约，宿主实现）。通知宿主有待确认的工具调用，携带参数摘要（已脱敏）。
- `IToolExecutionAuthorizer`：批准状态的唯一所有者。每次工具调用时被咨询。
- `ToolApprovalRequest`：待确认要素（不含令牌，只含参数摘要与原因）。
