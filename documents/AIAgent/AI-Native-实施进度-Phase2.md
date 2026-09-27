# AI-Native 实施进度与交接备忘（Phase 2 完成）

> **日期**：2026-09-27（本批次：Phase 2——流式回复 + 会话摘要 + 写工具授权 + RAG-A）
> **关联**：[路线图](../../.docs/AI/AI-Native-Agent-Roadmap-v1.md) · [总体功能设计](../../.docs/AI/AI-Native-Agent-Functional-Design.md) · [Phase 2 设计](../../.docs/AI/AI-Native-Agent-Phase2-Design.md) · [Phase 0-1 进度](./AI-Native-实施进度-Phase0-1.md)

## Phase 2（流式 + 记忆 + 写工具 + RAG-A）—— ✅ 全部任务落地（2026-09-27 批次）

### T2-1/T2-2 流式回复

| 交付 | 说明 |
| --- | --- |
| `IMessageChannel`（Mud.Feishu.AI/Channels） | 流式通道抽象：`BeginAsync`（占位消息）/`WriteStreamAsync`（增量）/`FlushAsync`（收尾）。失败语义入契约：Begin 失败向上抛（调用方回退非流式，模型未调用零重复成本）；单次增量写入失败由实现隔离（对齐 I1） |
| `EditMessageChannel`（Mud.Feishu.AI.FeishuTools/Channels） | `EditMessageAsync` 分片编辑降级实现：`SendMessageAsync` 建占位文本消息 → 增量缓冲达 `MaxStreamChunkLength`（默认 200 字符）以**累计全文**编辑一次 → Flush 落地最终文本；每次下游调用经 `IFeishuAppContextScopeFactory.BeginScope` 切换租户（TMA2-20）finally 释放 |
| 事件处理器流式桥接 | `ConversationalFeishuEventHandler<T>` 注入可选 `IMessageChannel`：群聊默认以 `SubjectId`（chat_id）为流式目标（`ResolveStreamTargetChatId` 虚方法可覆写，单聊缺省回退非流式）；流式成功不走 `ReplyAsync`；流中失败向上传播（事件层幂等键回滚、at-least-once 重投递）；OTel 属性 `feishu.agent.streamed` |
| 消息流卡片 | `IFeishuTenantV2AppCardMessageStream` 通道实现为后续切换项（抽象已就位，Phase 2 §3.1「通道实现切换」） |

### T2-3 会话裁剪 + 摘要（渐进式）

| 交付 | 说明 |
| --- | --- |
| `ConversationSummarizer`（Mud.Feishu.AI/Conversations） | 历史达到 `SummaryThreshold`（默认 30，0=禁用，Validate 拒绝 1~3）时，把「保留窗（`MaxHistoryMessages/2`，钳制到阈值-2 以下）以外」的旧消息经**一次模型调用**压缩为要点纪要，以 `ChatMessage(role:system)` 注入历史首部并写回 session；重建后条数低于阈值 → 不每轮重复摘要（渐进式，摘要驻留会话内） |
| `FeishuAgent` 集成 | RunAsync/RunStreamingAsync 前按需压缩（session 非空且启用时）；历史读写走 MAF `AgentSessionExtensions.TryGetInMemoryChatHistory/SetInMemoryChatHistory`（MAF 源生成序列化，AOT 安全）；摘要失败只记日志跳过（退化为既有裁剪行为，失败隔离）；OTel Span `feishu.agent.summarize`（只记条数不记内容，D5） |
| 与裁剪窗关系 | `MaxHistoryMessages` 由 `MessageCountingChatReducer` 折叠（模型可见窗）；摘要在其之上把「将被折叠丢弃」的内容先压成要点，避免硬截断丢上下文 |

### T2-4/T2-5 写工具 + 授权器真实消费

| 交付 | 说明 |
| --- | --- |
| 3 个写工具 | `im.send_message`（`SendMessageAsync`，msg_type=text 绑定层注入、receive_id_type 白名单校验）/ `bitable.add_record`（`AddRecordAsync`，fields JSON 对象 → `RecordOpsRequest.Fields` 以 `JsonElement` 承载——STJ 内建转换器源生成可序列化）/ `approval.create_instance`（`CreateInstanceAsync`，approval_code/form 为 body 字段）；全部 `IsWrite=true` 经 `[FeishuTool]` 源生成 Schema（`x-feishu.is_write`） |
| 白名单单独键控 | `FeishuAgentOptions.WriteAllowList`（默认空=不启用任何写工具）；注册扩展读写分离：`Tools` 只映射只读工具、`WriteAllowList` 只映射写工具，放错类别 fail-fast（守卫锁定） |
| 授权器真实消费 | 执行链既有门禁本批即已生效（`EnforceToolAuthorization=true` 且未注册 `IToolExecutionAuthorizer` → 写工具拒绝、零调用下游、不切租户上下文）；HITL 三态（`NeedsUserConfirmation`）已按 Phase 3 契约统一回填「需确认」结构化错误 |
| 注册重构 | `AddFeishuTools`（13 工具全注册默认不启用；写域客户端缺席时对应写工具不进注册表，白名单期 fail-fast）；`AddFeishuReadonlyTools` 保留为兼容别名 |

### T2-6 RAG-A（Aily 托管知识问答）

| 交付 | 说明 |
| --- | --- |
| 抽象（Mud.Feishu.AI/Knowledge） | `IFeishuKnowledgeBase`（数据面门面）+ `IRetriever`（检索面，正交）+ `KnowledgeAnswer`/`RetrievedChunk`（答案+切片引用 DTO）+ `AilyKnowledgeOptions`（`FeishuAilyKnowledge` 节：AppId/DataAssetIds/DataAssetTagIds，均有真实消费点） |
| `AilyKnowledgeProvider`（Mud.Feishu.AI.FeishuTools/Knowledge） | 包装 `IFeishuTenantV1AilyDataKnowledge.AskDataKnowledgeAsync`（SSE）：载荷一次读入按 `data:` 行解析（规避 ns2.0 缺失 `ReadAsStreamAsync(ct)`/`ReadLineAsync(ct)` 的 TFM 分叉）；`JsonDocument` 手工投影（AilyJsonContext 为 internal 跨包不可用——对齐 Phase 1 投影偏差决策）；QA 答案取最后非空 content、FAQ 命中回退 answer；实现 `IFeishuKnowledgeBase` + `IRetriever` 双接口 |
| 错误语义 | HTTP 200 + JSON 错误体（2700033/34/35 残余风险）按 Content-Type 自检转可读异常（对齐 `documents/ErrorHandling.md`）；SSE 正常结束无 finished 事件 → `HasAnswer=false` |
| 租户上下文 | appKey 经 `IFeishuToolContextAccessor` 异步流读取（事件入口注入，与工具执行链同一事实来源），缺失即失败（TMA2-20 禁止默认兜底）；调用期 `BeginScope` 切换 finally 释放 |
| 注册 | `AddFeishuAilyKnowledge(configuration?, configure?)`：源生成配置绑定 + 工厂期 `Validate()` fail-fast |

### 本批次验证

- 整包构建（4 TFM）0 错误；`Mud.Feishu.AI` 与 `Mud.Feishu.AI.FeishuTools` `-p:AotStrictMode=true --no-incremental` 均 **0 `IL2026` / 0 `IL3050` / 0 `AOT00x` / 0 警告**。
- AI.Tests **96**（+18：摘要器 6、流式事件处理 5、选项校验 7）× 2 TFM 全绿；FeishuTools.Tests **78**（+23：写工具 7、流式通道 7、RAG-A 8、契约守卫更新）× 2 TFM 全绿。
- 契约守卫升级：Schema 注册表恰为 13 契约名（10 只读 + 3 写）、IsWrite 与契约表逐一一致、读写白名单分离三态用例、`WriteAllowList`/`MaxStreamChunkLength` 消费点扫描。
- Demo 双模式扩展：`FEISHU_DEMO_CHAT_ID` 设置时经 `EditMessageChannel` 演示流式回复（Begin → 增量 → Flush）。
- `dotnet format` 本批次项目已收敛。

### 实施偏差记录（与设计文档对照）

| 偏差 | 依据 |
| --- | --- |
| 写工具白名单配置键为 **`FeishuAgent:WriteAllowList`**（平级键），非设计 §4 的 `FeishuAgent:Tools:WriteAllowList` | `FeishuAgentOptions.Tools` 已是 `string[]`（Phase 1 落地形态），嵌套子节与数组绑定互斥；平级键绑定语义等价，读写分离由注册扩展 fail-fast 保证 |
| `EditMessageChannel` / `AilyKnowledgeProvider` / 写工具执行器落位 **`Mud.Feishu.AI.FeishuTools`** 包，非总体设计 §5 的 `Mud.Feishu.AI` | 纵向引用治理（Phase 0-1 批次确立）：`Mud.Feishu.AI` 仅引用 Abstractions 不引 core；需强类型客户端的实现落「AI + core + DataModels」的工具包层。抽象（`IMessageChannel`/`IFeishuKnowledgeBase`/`IRetriever`/`AilyKnowledgeOptions`）仍落 `Mud.Feishu.AI`，依赖方向不变 |
| Aily `AppId` 落独立配置节 **`FeishuAilyKnowledge`**（`AilyKnowledgeOptions`），非 Phase 2 §4 的「归 `FeishuAppConfig` AI 域」 | AppId 是 Aily 平台资源标识而非飞书应用凭据；不动核心配置面（`FeishuAppConfig` 变更牵动配置审计/契约守卫/文档三线）；租户维度知识库范围治理归 Phase 4 |
| 流式摘要判定阈值语义定为「0=禁用、≥4 启用」（Validate 拒绝 1~3） | 阈值 ≤3 时「保留窗+1」无法低于阈值，会导致每轮重复摘要（渐进式失效）；fail-fast 优于静默错配 |
| `RetrieveAsync` 在 FAQ 命中且无 chunks 时以答案文本充当切片（Source=aily:faq） | Aily FAQ 通道不返回召回切片；检索面契约要求非空引用来源可回链，标准问答对本身即知识单元 |
| 摘要器构造于 `FeishuAgent` 内部（组合），未以公开服务注册 | 摘要与主对话必须共用同一 `IChatClient` 与 `ChatHistoryStateKey`（键同源）；独立注册会引入双键漂移面，且 R5 消费点仍在（`ConversationSummarizer`） |

## 下一批次（Phase 3 待启动项）

1. **T3-5/T3-6 业务事件 + 多模态**：`ApprovalContextAssembler`/`BitableRecordContextAssembler`（`IContextAssembler` 插件位已就绪）；OCR/STT/翻译三工具（`[FeishuTool]` 清单扩容，Phase 3 §3.3）。
2. **T3-7 HITL 确认门**：`AuthorizationResult.NeedsUserConfirmation` 三态已统一（Phase 2 执行链已回填「需确认」）；补确认卡片挂起/恢复流（`IConversationStore` 存 pending 快照）。
3. **T3-1~4 RAG-B（条件交付，已决策⑨）**：仅离线/敏感域/自定义检索策略场景启动；`IRetriever` 抽象与 `RetrievedChunk` 引用 DTO 本批已就位，RAG-B 实现并存于 `IFeishuKnowledgeBase` 门面下。
4. **MCP 消费侧评估备忘**（已决策⑧：Phase2+ 可选评估，不进 DoD）——仍未启动，顺延。
