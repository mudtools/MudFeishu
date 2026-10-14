# MudFeishu 项目长期记忆

## AI 包族边界（Mud.AI.*）

- `Mud.AI.Abstractions`：**零 PackageReference**（除 `Microsoft.CodeAnalysis.PublicApiAnalyzers`）+ **禁 MAF** ⇒ 只放「零依赖契约」。
- `Mud.AI.Agent`：MAF 运行期（工具执行骨架 + 会话/配置/通道/审批/检索机制）；**不得引用任何 `Mud.Feishu*`**。
- `Mud.Feishu.AI`：生态适配层（产品语义：`feishu.*` 遥测命名面、飞书实测预算值、`AppKey/ChatId/ThreadId` 租户语义、`x-feishu.*` 词汇、飞书 SDK 客户端、飞书事件 DTO）。
- `Mud.Feishu.AI.Tools` / `.Mcp`：工具面/协议面，**产品面**，不参与中立化搬迁。
- `Mud.Feishu.Abstractions` / `Mud.Feishu.Redis`：**已发布 SDK，字节不动**；不得反向引用 `Mud.AI.*`。

## 三条方向性不变量（永不放宽）

1. `MudAiFamily_ShouldNotDependOnFeishuProjects`：`Mud.AI.*` ↛ `Mud.Feishu*`。
2. `CoreSdkProjects_ShouldNotDependOnMudAiFamily`：核心 SDK ↛ `Mud.AI.*`。
3. `MudAiAbstractions_ShouldNotDeclareMafPackages`：Abstractions 禁 MAF。

⇒ 这三条强制出「**中立端口 + 产品防腐层适配器**」的定式（已证先例：`ISessionStore` + `ConversationStoreSessionAdapter`）。形状相同的重复类型（如 `ConversationScope`）是**有意的接缝**，不是可以合并的冗余——必须在 XML 注释里写明理由。

## 中立化工艺：产品事实五式

| 式 | 手法 | 先例 |
| --- | --- | --- |
| ① 参数注入 | 产品事实改为构造/方法参数，中立侧不留默认值 | `GuidanceComposer.Compose(host, blocks, maxGuidanceLength)`；`AgentSessionStore(codec, store, historyStateKey, logger)` |
| ② 端口抽象 | 中立定 interface，产品给实现 | `ISessionStore`；`IPendingApprovalStore`；`IAgentTelemetry`；`ITurnScopeFactory` |
| ③ 委托回调 | 产品多项式用委托注入 | 会话键治理 `Func<ConversationRequest,string>` |
| ④ 门面保留 | 飞书侧保留同名同形门面（`global using` / 空标记接口），调用点零改 | `IFeishuToolApprovalChannel : IToolApprovalChannel` |
| ⑤ 语义中性化 | 含产品词汇的字面量改中立表述或提出去 | 摘要提示词「飞书资源」→「资源标识」 |

## 「不改」清单（宿主可见面，未发布窗口**不**豁免）

- `FeishuAgentOptions` 的全部属性名与 `SectionName`
- `FeishuAgent` 的公开构造签名
- 全部 `feishu.*` 遥测键（R2-13：改名会让宿主仪表盘失效）
- 全部 `x-feishu.*` 词汇
- 全部飞书 SDK 契约
