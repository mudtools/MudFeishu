# ADR-0001：会话摘要器与 MAF 压缩管线的取舍

- 状态：**已决策（否决迁移）**
- 日期：2026-10-14（由代码注释迁出；原始决策记录见 `documents/AIAgent/AI-Native-实施进度-Phase2.md` 的 B3.1 行）
- 影响面：`Mud.Feishu.AI/Conversations/ConversationSummarizer.cs`，
  `FeishuAgentOptions.MaxHistoryMessages` / `MaxHistoryTokens` / `SummaryThreshold`

## 背景

MAF 1.20.0 提供 `CompactionProvider` / `PipelineCompactionStrategy` /
`SummarizationCompactionStrategy`，表面上是自研渐进式摘要器的替代品。

## 决策

不迁移，继续使用自研 `ConversationSummarizer`（本仓维护保留窗、收敛兜底与工具组保护）。

## 理由

1. MAF 1.20.0 的 Compaction 系列全部标注 `[Experimental]`。
2. 二者叠加会造成会话历史**双份无界增长**：
   - `InMemoryChatHistoryProvider.StoreChatHistoryAsync` 原样追加请求 + 响应消息；
     迁移 Compaction 的前提是移除其 `ChatReducer`，移除后 `State.Messages` 只增不减；
   - `CompactionProvider` 会把**含被排除组**的 `MessageGroups` 也写入会话状态袋。
   - 结果：落库 JSON 同时保存两份会话历史且均无界（Redis 后端按 key 存整包），
     违反「落库体积可控」。

## 后果

- 摘要策略的语义与边界由本仓拥有并负有测试责任（见 `Tests/Mud.Feishu.AI.Tests/Conversations/`）。
- 待 MAF 提供「存储侧压缩」语义后重新评估迁移。
