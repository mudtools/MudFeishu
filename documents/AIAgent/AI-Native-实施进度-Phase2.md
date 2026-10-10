# AI-Native 实施进度与交接备忘（Phase 2 完成）

> **日期**：2026-09-27（本批次：Phase 2——流式回复 + 会话摘要 + 写工具授权 + RAG-A）
> **关联**：[路线图](../../.docs/AI/AI-Native-Agent-Roadmap-v1.md) · [总体功能设计](../../.docs/AI/AI-Native-Agent-Functional-Design.md) · [Phase 2 设计](../../.docs/AI/AI-Native-Agent-Phase2-Design.md) · [Phase 0-1 进度](./AI-Native-实施进度-Phase0-1.md)

## Phase 2（流式 + 记忆 + 写工具 + RAG-A）—— ✅ 全部任务落地（2026-09-27 批次）

### T2-1/T2-2 流式回复

| 交付                                                       | 说明                                                                                                                                                                                                                                                                                                    |
| ---------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `IMessageChannel`（Mud.Feishu.AI/Channels）                | 流式通道抽象：`BeginAsync`（占位消息）/`WriteStreamAsync`（增量）/`FlushAsync`（收尾）。失败语义入契约：Begin 失败向上抛（调用方回退非流式，模型未调用零重复成本）；单次增量写入失败由实现隔离（对齐 I1）                                                                                               |
| `EditMessageChannel`（Mud.Feishu.AI.FeishuTools/Channels） | `EditMessageAsync` 分片编辑降级实现：`SendMessageAsync` 建占位文本消息 → 增量缓冲达 `MaxStreamChunkLength`（默认 200 字符）以**累计全文**编辑一次 → Flush 落地最终文本；每次下游调用经 `IFeishuAppContextScopeFactory.BeginScope` 切换租户（TMA2-20）finally 释放                                       |
| 事件处理器流式桥接                                         | `ConversationalFeishuEventHandler<T>` 注入可选 `IMessageChannel`：群聊默认以 `SubjectId`（chat_id）为流式目标（`ResolveStreamTargetChatId` 虚方法可覆写，单聊缺省回退非流式）；流式成功不走 `ReplyAsync`；流中失败向上传播（事件层幂等键回滚、at-least-once 重投递）；OTel 属性 `feishu.agent.streamed` |
| 消息流卡片                                                 | `IFeishuTenantV2AppCardMessageStream` 通道实现为后续切换项（抽象已就位，Phase 2 §3.1「通道实现切换」）                                                                                                                                                                                                  |

### T2-3 会话裁剪 + 摘要（渐进式）

| 交付                                                    | 说明                                                                                                                                                                                                                                                                                                    |
| ------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ConversationSummarizer`（Mud.Feishu.AI/Conversations） | 历史达到 `SummaryThreshold`（默认 30，0=禁用，Validate 拒绝 1~3）时，把「保留窗（`MaxHistoryMessages/2`，钳制到阈值-2 以下）以外」的旧消息经**一次模型调用**压缩为要点纪要，以 `ChatMessage(role:system)` 注入历史首部并写回 session；重建后条数低于阈值 → 不每轮重复摘要（渐进式，摘要驻留会话内）     |
| `FeishuAgent` 集成                                      | RunAsync/RunStreamingAsync 前按需压缩（session 非空且启用时）；历史读写走 MAF `AgentSessionExtensions.TryGetInMemoryChatHistory/SetInMemoryChatHistory`（MAF 源生成序列化，AOT 安全）；摘要失败只记日志跳过（退化为既有裁剪行为，失败隔离）；OTel Span `feishu.agent.summarize`（只记条数不记内容，D5） |
| 与裁剪窗关系                                            | `MaxHistoryMessages` 由 `MessageCountingChatReducer` 折叠（模型可见窗）；摘要在其之上把「将被折叠丢弃」的内容先压成要点，避免硬截断丢上下文                                                                                                                                                             |

### T2-4/T2-5 写工具 + 授权器真实消费

| 交付           | 说明                                                                                                                                                                                                                                                                                                                                                                                                        |
| -------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 3 个写工具     | `im.send_message`（`SendMessageAsync`，msg_type=text 绑定层注入、receive_id_type 白名单校验）/ `bitable.add_record`（`AddRecordAsync`，fields JSON 对象 → `RecordOpsRequest.Fields` 以 `JsonElement` 承载——STJ 内建转换器源生成可序列化）/ `approval.create_instance`（`CreateInstanceAsync`，approval_code/form 为 body 字段）；全部 `IsWrite=true` 经 `[FeishuTool]` 源生成 Schema（`x-feishu.is_write`） |
| 白名单单独键控 | `FeishuAgentOptions.WriteAllowList`（默认空=不启用任何写工具）；注册扩展读写分离：`Tools` 只映射只读工具、`WriteAllowList` 只映射写工具，放错类别 fail-fast（守卫锁定）                                                                                                                                                                                                                                     |
| 授权器真实消费 | 执行链既有门禁本批即已生效（`EnforceToolAuthorization=true` 且未注册 `IToolExecutionAuthorizer` → 写工具拒绝、零调用下游、不切租户上下文）；HITL 三态（`NeedsUserConfirmation`）已按 Phase 3 契约统一回填「需确认」结构化错误                                                                                                                                                                               |
| 注册重构       | `AddFeishuTools`（13 工具全注册默认不启用；写域客户端缺席时对应写工具不进注册表，白名单期 fail-fast）；`AddFeishuReadonlyTools` 保留为兼容别名                                                                                                                                                                                                                                                              |

### T2-6 RAG-A（Aily 托管知识问答）

| 交付                                                           | 说明                                                                                                                                                                                                                                                                                                                                                                            |
| -------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 抽象（Mud.Feishu.AI/Knowledge）                                | `IFeishuKnowledgeBase`（数据面门面）+ `IRetriever`（检索面，正交）+ `KnowledgeAnswer`/`RetrievedChunk`（答案+切片引用 DTO）+ `AilyKnowledgeOptions`（`FeishuAilyKnowledge` 节：AppId/DataAssetIds/DataAssetTagIds，均有真实消费点）                                                                                                                                             |
| `AilyKnowledgeProvider`（Mud.Feishu.AI.FeishuTools/Knowledge） | 包装 `IFeishuTenantV1AilyDataKnowledge.AskDataKnowledgeAsync`（SSE）：载荷一次读入按 `data:` 行解析（规避 ns2.0 缺失 `ReadAsStreamAsync(ct)`/`ReadLineAsync(ct)` 的 TFM 分叉）；`JsonDocument` 手工投影（AilyJsonContext 为 internal 跨包不可用——对齐 Phase 1 投影偏差决策）；QA 答案取最后非空 content、FAQ 命中回退 answer；实现 `IFeishuKnowledgeBase` + `IRetriever` 双接口 |
| 错误语义                                                       | HTTP 200 + JSON 错误体（2700033/34/35 残余风险）按 Content-Type 自检转可读异常（对齐 `documents/ErrorHandling.md`）；SSE 正常结束无 finished 事件 → `HasAnswer=false`                                                                                                                                                                                                           |
| 租户上下文                                                     | appKey 经 `IFeishuToolContextAccessor` 异步流读取（事件入口注入，与工具执行链同一事实来源），缺失即失败（TMA2-20 禁止默认兜底）；调用期 `BeginScope` 切换 finally 释放                                                                                                                                                                                                          |
| 注册                                                           | `AddFeishuAilyKnowledge(configuration?, configure?)`：源生成配置绑定 + 工厂期 `Validate()` fail-fast                                                                                                                                                                                                                                                                            |

### 本批次验证

- 整包构建（4 TFM）0 错误；`Mud.Feishu.AI` 与 `Mud.Feishu.AI.FeishuTools` `-p:AotStrictMode=true --no-incremental` 均 **0 `IL2026` / 0 `IL3050` / 0 `AOT00x` / 0 警告**。
- AI.Tests **96**（+18：摘要器 6、流式事件处理 5、选项校验 7）× 2 TFM 全绿；FeishuTools.Tests **78**（+23：写工具 7、流式通道 7、RAG-A 8、契约守卫更新）× 2 TFM 全绿。
- 契约守卫升级：Schema 注册表恰为 13 契约名（10 只读 + 3 写）、IsWrite 与契约表逐一一致、读写白名单分离三态用例、`WriteAllowList`/`MaxStreamChunkLength` 消费点扫描。
- Demo 双模式扩展：`FEISHU_DEMO_CHAT_ID` 设置时经 `EditMessageChannel` 演示流式回复（Begin → 增量 → Flush）。
- `dotnet format` 本批次项目已收敛。

### 实施偏差记录（与设计文档对照）

| 偏差                                                                                                                                   | 依据                                                                                                                                                                                                                                                                  |
| -------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 写工具白名单配置键为 **`FeishuAgent:WriteAllowList`**（平级键），非设计 §4 的 `FeishuAgent:Tools:WriteAllowList`                       | `FeishuAgentOptions.Tools` 已是 `string[]`（Phase 1 落地形态），嵌套子节与数组绑定互斥；平级键绑定语义等价，读写分离由注册扩展 fail-fast 保证                                                                                                                         |
| `EditMessageChannel` / `AilyKnowledgeProvider` / 写工具执行器落位 **`Mud.Feishu.AI.FeishuTools`** 包，非总体设计 §5 的 `Mud.Feishu.AI` | 纵向引用治理（Phase 0-1 批次确立）：`Mud.Feishu.AI` 仅引用 Abstractions 不引 core；需强类型客户端的实现落「AI + core + DataModels」的工具包层。抽象（`IMessageChannel`/`IFeishuKnowledgeBase`/`IRetriever`/`AilyKnowledgeOptions`）仍落 `Mud.Feishu.AI`，依赖方向不变 |
| Aily `AppId` 落独立配置节 **`FeishuAilyKnowledge`**（`AilyKnowledgeOptions`），非 Phase 2 §4 的「归 `FeishuAppConfig` AI 域」          | AppId 是 Aily 平台资源标识而非飞书应用凭据；不动核心配置面（`FeishuAppConfig` 变更牵动配置审计/契约守卫/文档三线）；租户维度知识库范围治理归 Phase 4                                                                                                                  |
| 流式摘要判定阈值语义定为「0=禁用、≥4 启用」（Validate 拒绝 1~3）                                                                       | 阈值 ≤3 时「保留窗+1」无法低于阈值，会导致每轮重复摘要（渐进式失效）；fail-fast 优于静默错配                                                                                                                                                                          |
| `RetrieveAsync` 在 FAQ 命中且无 chunks 时以答案文本充当切片（Source=aily:faq）                                                         | Aily FAQ 通道不返回召回切片；检索面契约要求非空引用来源可回链，标准问答对本身即知识单元                                                                                                                                                                               |
| 摘要器构造于 `FeishuAgent` 内部（组合），未以公开服务注册                                                                              | 摘要与主对话必须共用同一 `IChatClient` 与 `ChatHistoryStateKey`（键同源）；独立注册会引入双键漂移面，且 R5 消费点仍在（`ConversationSummarizer`）                                                                                                                     |

## 审查缺陷修复批次（R1-R2 复核修订，2026-09-28）

> 依据：[`Mud.Feishu.AI 审查缺陷修复与能力完善方案-R1`](../../.docs/AI/Mud.Feishu.AI-审查缺陷修复与能力完善方案-R1.md)（含 R1-R2 复核修订与 §0 否决项证据）。

### 落地项

| 编号      | 交付                                                                      | 说明                                                                                                                                                                                                                                             |
| --------- | ------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| P0-1      | `ConversationalFeishuEventHandler<T>` 工具上下文 `Begin` 前置于上下文装配 | 原顺序颠倒 ⇒ 装配器读不到 `IFeishuToolContextAccessor.Current.AppKey`（`AilyKnowledgeProvider` 抛异常被隔离 catch 吞为 Warning），**RAG 注入模式每轮零注入且仅日志可见**                                                                         |
| P0-2      | `ConversationSummarizer` 切片起点工具组保护                               | 切片落入 `assistant(tool_calls)`+`tool(result)` 组内 ⇒ 下发 400 ⇒ 保存失败 ⇒ 重投递再次 400，**会话永久不可用**；起点遇工具相关消息向更早回退，全组则跳过本轮                                                                                    |
| P1-1      | 摘要重建的 token 预算收敛                                                 | 保留窗起点取「条数窗 ∩ token 预算窗」的**较晚者**（超限内容交给摘要而非丢弃），重建后仍越限时从保留窗**头部整组**回退；原方案「从尾部删除」会丢失最新上下文，已纠正                                                                              |
| P1-2      | 流式失败补偿收尾                                                          | 模型流中断时用 `CancellationToken.None` 补一次 `FlushAsync`，把占位消息落到终结态（通道语义为「停留上一次成功内容」）；异常仍向上传播（幂等回滚语义不变）                                                                                        |
| P1-3      | 流式路径落库令牌                                                          | 落库用不可取消令牌；**失败路径不落库**（保持既有 at-least-once，改「失败也落库」等于把「可能重复」换成「确定丢失」）                                                                                                                             |
| P1-4      | 会话反序列化 catch 面 + 坏键自愈                                          | 扩至 `ArgumentException`（MAF 对非对象根抛该类型）等四类，并 `DeleteAsync` 删除坏键（否则毒事件循环、坏载荷永不自愈）                                                                                                                            |
| P1-5a     | **`IAppKeyAccessor` 装配链贯通（复核新发现）**                            | 原实现从不向 AI 事件处理器注入访问器 ⇒ `CurrentAppKey` 恒 `null` ⇒ 生产会话键恒为 `feishu:default:…`，**多应用会话历史当前即混用**；现贯通并透传至内置 IM 处理器                                                                                 |
| P1-5b     | 缺 AppKey 分级处置                                                        | 已装配访问器但当前值缺失 ⇒ fail-fast（TMA2-20）；完全未装配（WebSocket 单应用）⇒ 降级 + Warning；`AllowMissingAppKey` 可覆写。**原方案一律 fail-fast 会打断 WebSocket 单应用宿主**                                                               |
| P1-6      | guidance 预算口径                                                         | 额度只计 guidance 本体（宿主 `Instructions` 不得挤占域资产预算）；装配结果经 `FeishuAgent.Guidance` 暴露为可断言面                                                                                                                               |
| P2-1/2/3  | MAF 基类契约一致性                                                        | `Name` 改 `override`、`GetService` 先判自身、`StreamingRequestContext.Scope` 恢复前值（嵌套正确）                                                                                                                                                |
| P2-4/7    | 小修                                                                      | 工具上下文 `ChatId` 复用 `ResolveStreamTargetChatId` 钩子；空用户消息守卫（避免 400 且已计费）                                                                                                                                                   |
| P2-5      | token 计数                                                                | 显式声明 `Microsoft.ML.Tokenizers 2.0.0`（直接使用的 API 必须直接声明）+ CJK 估算比校正（原 4 字符/token 低估中文约 4 倍，中文会话摘要窗口迟迟不触发）；`IsExactCount`/`Estimate` 为可断言面；**不采纳「启动期预热」**（静态类无日志面、无收益） |
| P2-6      | 摘要启用条件                                                              | `SummaryThreshold > 0 \|\| MaxHistoryTokens > 0`（原 token-only 配置静默失效）；并修正 token-only 下保留窗退化为 1 条的缺陷                                                                                                                      |
| P2-8/B3.3 | 可观测性                                                                  | 失败路径 `SetStatus(Error)`（流式经非迭代器包装层，规避 CS1626）+ 耗时入 `finally`（失败样本入直方图）                                                                                                                                           |
| P2-9      | 身份闭集校验                                                              | 新增 `FeishuToolIdentityNames`（tenant/user），装配期拒绝非法字面量（原会「全部工具静默拒绝」）                                                                                                                                                  |
| B3.4      | 契约守卫 4 条                                                             | `ToolContext_Begin_ShouldPrecedeContextAssembly`、`FeishuAgent_Name_ShouldBeOverrideNotHide`、`FeishuAgent_GetService_ShouldCheckSelfFirst`、`AgentStateKeys_ShouldBeUniqueAndConsumed`                                                          |

### 明确不采纳（复核结论，附证据）

| 项                                                      | 结论             | 依据                                                                                                                                                                                                                                                                                                  |
| ------------------------------------------------------- | ---------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| P2-10「工厂内改用 `CreateScope()`」                     | **否决**         | 装配链协作件全为 Singleton（唯一 Scoped 是处理器本体）；根作用域解析 Scoped 在 `ValidateScopes` 下是响亮失败，改 `CreateScope` 会把响亮失败换成「Scoped 被单例捕获后悬空」的静默缺陷。改为**契约测住**：`AddFeishuAgent_ToolSource_ShouldReceiveRootScopeOnly`                                        |
| §4.3「拒绝 `SummaryThreshold=0 && MaxHistoryTokens>0`」 | **否决**         | 与 P2-6 自相矛盾；P2-6 使该组合成为合法配置（仅按 token 触发摘要）                                                                                                                                                                                                                                    |
| B3.1「迁移 MAF Compaction 管线」                        | **否决（本轮）** | MAF 1.20.0 的 `Compaction*` 全部 `[Experimental]`；移除 `InMemoryChatHistoryProvider.ChatReducer` 后 `State.Messages` 只增不减，且 `CompactionProvider` 把含被排除组的 `MessageGroups` 另存一份 ⇒ 落库 JSON 现双份无界增长，违反「落库体积可控」。保留自研摘要器（含 B0/B1 修复），取舍已登记于类注释 |
| B3.2「HITL 与 MEAI 审批对齐」                           | **降级 Phase 4** | 跨包语义变更 + 依赖尚不存在的「飞书卡片批准回灌」落点，先做一期会造成「审批进标准通道但无人应答」                                                                                                                                                                                                     |
| P1-3「失败路径也落库」                                  | **否决**         | 与 `IdempotentFeishuEventHandler` 的 at-least-once 语义及既有用例冲突                                                                                                                                                                                                                                 |

### 本批次验证

- 整包构建（4 TFM）**0 错误**；`Mud.Feishu.slnx` 全量 `dotnet test` **0 失败**（AI.Tests 192、FeishuTools.Tests 302，× net8.0/net10.0）。
- 新增用例：上下文顺序 4、工具组边界 3、收敛 3、流式补偿 2、坏载荷自愈 2、基类契约 2/2、appKey 分级 4、空消息守卫 1、guidance 3（含改写 1 例）、身份闭集 5、token 计数 7、环境量嵌套 3、工具源契约 1、守卫 4。
- **环境限制**：本机未安装 `pwsh`，`scripts/verify-build.ps1`（AOT 严格模式 + 逐 TFM TRX 计数断言）未能执行，需在具备 `pwsh` 的环境补跑后方可发布。

## 下一批次（Phase 3 待启动项）

> **状态更新（2026-10-10，R7 收尾轮）**：下列 1~4 项**均已落地**，逐项证据见
> `.docs/MudFeishu-AI工具面功能完善方案-六域补齐与Agent可用性硬伤及遗留任务-R7.md` §10.7 / §10.8 / §10.9。
> 唯一仍待产品的决策是 `board.download_image` 是否解除"不策展"（出向二进制通道的首个消费工具）。

1. ~~**T3-5/T3-6 业务事件 + 多模态**~~ → **已完成**：`ApprovalContextAssembler` + `BitableRecordContextAssembler`（含生产者
   `BitableRecordChangedConversationalEventHandler`）；多模态按 PM 裁决（DP-C3-1）**缩为 2 条纯文本工具**
   （`ai.translate_text` / `ai.detect_language`），OCR/STT/文档识别改由宿主入向通道承担（`IFeishuBinaryArtifactSource`）。
2. ~~**T3-7 HITL 确认门**~~ → **C4a 已完成**（`IFeishuPendingApprovalStore` + `InMemoryPendingApprovalStore` +
   `PendingApprovalSnapshot` + 投影落快照 + 续跑幂等消费 + 过期放弃）；**C4b 卡片回灌按 PM 裁决延后**
   （`Mud.Feishu.EventCallback` 无卡片动作事件类型，需 3 层新增，重启条件见方案 §8.2 DP-C4-1）。
3. ~~**T3-1~4 RAG-B（条件交付）**~~ → **已完成**：`ICorpusSource` / `DocumentChunker` / `IVectorStore` / `VectorRetriever` /
   `CorpusIndexer`（**不做向量库、不引入嵌入依赖**，向量化与存储交宿主，DP-C5-1）。
4. ~~**MCP 消费侧评估备忘**~~ → **已完成**（R7 / C6b · Batch-8）：新增可选包 `Mud.Feishu.AI.Mcp`
   （stdio JSON-RPC 2.0；工具集与 `FeishuAgent:Tools`/`WriteAllowList` 同源；每个 tool call 经既有执行链；
   appKey 只来自配置、缺即 fail-fast；写工具 HITL 走授权器三态 fail-closed）。
