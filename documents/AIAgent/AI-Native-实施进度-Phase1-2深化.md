# AI-Native 实施进度：Phase 1/2 功能深化（AI-FD-D12 批次 A/B）

> **日期**：2026-09-27
> **设计依据**：[AI-Native-Agent-Deepening-Phase12-Design.md](../AI/AI-Native-Agent-Deepening-Phase12-Design.md)
> **关联**：[Phase 0-1 进度](AI-Native-实施进度-Phase0-1.md) · [Phase 2 进度](AI-Native-实施进度-Phase2.md)

本文记录深化设计批次 A（P0 正确性与枢纽件）与批次 B（P1 治理与体验）的落地事实。
批次 C（自动翻页 / HITL 预留 / map-reduce 摘要 / Aily 多轮评估 / 写工具扩容）按设计为
「按需/评估」，未实现，见设计 §八。

## 批次 A（P0）

| # | 任务 | 状态 | 落点 |
| --- | --- | --- | --- |
| A1 | core 缺陷修复：`GetContentListByMessageIdAsync` 路由补 `{message_id}` | ✅ | `Mud.Feishu/Interfaces/Messages/IFeishuV1Message_Tenant.cs`（XML 注释登记缺陷语义；CHANGELOG 行为变更登记） |
| A2 | 会话串行化三层 + 事件处理器接入 | ✅ | 契约 `Abstractions/Conversations/IConversationGate.cs`（含 `ConversationBusyException`）；进程内 `Mud.Feishu.AI/Conversations/KeyedConversationGate.cs`（AddFeishuAgent 默认注册）；分布式 `Mud.Feishu.Redis/Services/RedisConversationGate.cs` + `AddFeishuRedisConversationGate`；事件处理器 Acquire/finally Release + `gate_wait_ms` Span 属性 |
| A3 | `ConversationRequest.ChatId` 解耦 | ✅ | `ConversationRequest` 末位新增带默认值参数（源兼容）；流式目标与工具上下文 ChatId 优先取之，缺省回退群聊 SubjectId |
| A4 | 卡片流通道 + 降级链 | ✅ | `Channels/CardStreamMessageChannel.cs`（feed card 按用户投放：p2p=SenderId、群聊不适用）；`Channels/StreamingChannelChain.cs`（Begin 逐通道尝试、目标重解析经 `StreamingRequestContext` 环境量）；`AddFeishuStreamingChannel` |
| A5 | 知识装配器 + knowledge.search 工具 | ✅ | `Mud.Feishu.AI/Knowledge/KnowledgeContextAssembler.cs`（Order=100、格式 `[1] …`、失败隔离）；工具接口/执行器/注册器（`Tools/FeishuKnowledgeToolInterfaces.cs`、`Internal/KnowledgeSearchTools.cs`） |
| A6 | 内置 IM 会话事件处理器 | ✅ | `Mud.Feishu.AI.FeishuTools/Events/ImMessageConversationalEventHandler.cs` + `ImConversationOptions` + `AddFeishuImConversationHandler`（Bot 自激过滤 sender_type=app；群聊 @ 过滤；单聊开关；问题文本先于装配器片段） |
| A7 | 工具面扩容 13→19 | ✅ | 6 新工具（im.get_message_content / docx.get_document_blocks / bitable.get_records_by_ids / drive.list_folder_files / drive.get_file_metas / knowledge.search）+ query_records sort 简化文法（`BitableSortParser`，≤3 子句） |
| A8 | 子域注册粒度 | ✅ | `Registration/FeishuToolDomainRegistrars.cs`（接口 + 9 域注册器 + 登记助手）；注册扩展重写（执行器 null 工厂软缺席；全域入口产物等价性用例锁定）；`AddFeishuEditMessageChannel` Phase 2 兼容入口保留 |
| A9 | JSON 感知截断 + 结果整形钩子 | ✅ | `ToolResultText.TruncateJson`（items 逐条删除 + truncated/hint，纯文本回退字符截断）；`IToolResultShaper`（绑定层⑤'步调用、失败回退默认） |
| A10 | scope 定稿 + 对照表 + 守卫精确值 | ✅ | `documents/AIAgent/工具权限对照表.md`（19 工具逐一对照；Aily 项标注控制台核对日期）；`FeishuToolContractGuards.ToolScopes_ShouldMatchThePermissionMappingTable_ExactValues` |
| A11 | 测试 + 守卫 + Demo | ✅ | 契约表守卫升格 19 工具；新增闸门/ChatId 四象限/token 窗口/卡片流/降级链/sort/分类/审计/目录/注册粒度/IM 处理器/Redis 闸门用例；Demo 增 `ImConversationDemo`（FEISHU_DEMO_IM_HANDLER=1） |

## 批次 B（P1）

| # | 任务 | 状态 | 落点 |
| --- | --- | --- | --- |
| B1 | 错误分类四分类回填 | ✅ | `Tools/ToolErrorClassifier.cs`（纯函数：异常类型 × code 段位 → retryable/invalid_args/forbidden/api_error）；`FeishuApiOutcome` 携带 `Code`；执行器错误回填全量接线；分类回填文本「分类 + 原因 + 建议」 |
| B2 | 审计出口 | ✅ | `Mud.Feishu.AI/Tools/IToolExecutionAuditSink.cs`；`Tools/ToolArgsDigester.cs`（SDK 侧脱敏：参数名+值长度+敏感键掩码）；执行链 allowed/denied/error 三路投递、sink 异常隔离 |
| B3 | 目录 + OpenAI 导出 | ✅ | `Mud.Feishu.AI/Tools/IToolCatalog.cs`（含 `ToolSchemaDialect` 方言位）；`Tools/FeishuToolCatalog.cs`（目录包装 + OpenAI functions 导出，剥离 x-feishu） |
| B4 | 工具三指标 | ✅ | `FeishuMetrics.ToolExecutions`/`ToolDuration`/`AgentLlmDuration` + `ToolOutcomes` 受控枚举；`FeishuToolDiagnostics.RecordExecution/RecordDuration`、`FeishuAgentDiagnostics.RecordLlmDuration`；高基数守卫（源码扫描）锁定 |
| B5 | token 窗口 + 摘要治理 | ✅ | `FeishuAgentOptions.MaxHistoryTokens`（默认 8000）；`Conversations/ChatTokenCounter.cs`（NET8+ Tiktoken 离线计数，初始化失败/低 TFM 回退字符÷4 估算——设计 §十一风险表授权路径）；`ConversationSummarizer.ShouldSummarize(count, tokens)` 双窗口先触发者；摘要输入每条 500 字压缩 + 30s 超时钳制（超时视为失败跳过） |
| B6 | 内置装配器集 | ✅ | `Events/ContextAssemblers.cs`（SenderInfo 默认启用 / QuoteMessage 依赖 A1 修复、300 字预览、失败跳过）；`AddFeishuImConversationHandler(assemblers:)` 位标记装配（`ImConversationAssemblers`） |
| B7 | 速率自适应分片 | ✅ | `Channels/BufferedMessageChannel.cs`（分片长度 + 800ms 最小更新间隔双阈值；Flush 终态无条件；异常隔离钩子）；两通道同构重构（`EditMessageChannel`/`CardStreamMessageChannel`，测试可注入 0 间隔） |
| B8 | RAG 引用回链 | ✅ | `AilyKnowledgeProvider` chunk Source 按 `DataAssetIds` 填充（`aily:data-knowledge:{id}` 弱引用 / 缺省 `aily:data-knowledge`）；`KnowledgeAnswer.Sources` 编号尾注投影（RAG-B wiki URL 强回链契约位预留） |

## 实施偏差记录

1. **闸门 TTL 取值**：设计 P2D-1 说「TTL = FeishuConversationOptions 处理超时上界（复用既有单一
   阈值源）」，而 `FeishuConversationOptions` 现仅含 `SessionTtl`——按「单一阈值源」实现为
   `SessionTtl`（5 分钟默认演示值/24h 默认）。租约正常路径经比较删除即时放行；持有实例崩溃时
   最长 SessionTtl 后自动过期，期间同键新事件快速失败由事件层重投递承接。
2. **卡片流投放对象**：`im/v2/app_feed_card` 按用户 open_id 投放（DTO `user_ids`），与设计
   「Begin(chatId)」表述存在目标语义差异。实现以 `IMessageChannelTargetResolver`（新增可选能力
   接口）+ `StreamingRequestContext`（AsyncLocal 环境量）让各子通道按自身语义重解析目标：
   卡片流单聊=发送者 open_id（群聊不适用），编辑通道=chat_id——事件处理器零感知，降级链成立。
3. **token 计数 AOT 路径**：`Microsoft.ML.Tokenizers` 经 MAF 元包传递可用（nuspec 已核实）；
   Tiktoken 编码器在 NET8+ 启用（`CountTokens` 离线计数），初始化异常或低 TFM 回退字符÷4 估算
   （设计 §十一风险表授权的降级路径，行为差异仅影响触发时机精度）。
4. **事件处理器过滤位置**：基类 `HandleAsync` 为 sealed，Bot 自激/@ 过滤落在
   `ProcessBusinessLogicAsync` 入口（业务幂等标记之后）——被过滤消息按「已消费」落幂等终态
   （不重投递），语义正确但幂等键会为过滤消息消费一次。
5. **`AddFeishuEditMessageChannel` 保留**：设计说降级链「替代」单通道注册；为不破坏 Phase 2
   宿主（Demo/既有用例），保留原单通道注册入口为兼容路径（TryAdd 语义不变）。

## 验证事实（2026-09-27）

- `Mud.Feishu.AI.Tests`：net8.0 118 用例全绿（含闸门串行化/取消传播、ChatId 四象限、token 窗口）。
- `Mud.Feishu.AI.FeishuTools.Tests`：net8.0 130 用例全绿（含 19 工具契约表 + scope 精确值守卫、
  卡片流全链、降级链、速率钳制、错误分类、审计脱敏、目录导出、单域注册）。
- `Mud.Feishu.Redis.Tests`：`RedisConversationGateTests` 5 用例全绿（SET NX/快速失败/比较删除）。
- Demo（`Mud.Feishu.Agent.Demo`）编译通过，新增零自定义接入演示面。
- 全量质量门禁（`scripts/verify-build.ps1`）与 AOT 冒烟结论见仓库 CI 记录。
