# ADR-0012：MCP `tools/call` 结果形态与进度通道的边界

- 状态：**已接受**
- 日期：2026-10-23
- 影响面：`Mud.Feishu.AI.Mcp`（`FeishuMcpToolServer` 的结果信封）、
  `documents/AIAgent/MCP多租户部署Runbook.md` §6、`McpProtocol` 的加版判据

## 背景

MCP 的两个能力面在协议演进中被反复问起，且都与 `tools/call` 相邻：

1. **结构化工具输出**（2025-06-18 修订引入 `outputSchema` 与 `structuredContent`）：服务端除
   text content 外，可回一份机器可读的结构化结果；
2. **进度通知**（`notifications/progress`，自 2024-11-05 起存在）：长耗时调用可携带
   `progressToken` 逐步上报进度。

本仓的 `tools/call` 只回 text content，也没有进度通道。二者的"缺失"适合被当成遗漏反复重提，
因此在此登记为**决策**而非待办。

## 决策 1：不产出 `structuredContent` / `outputSchema`

理由（任一成立即足以否决）：

- **结果文本不是纯 JSON**：执行链的输出契约是"第一行紧凑 JSON（`tool_error`）+ 第二行 `[next]` 恢复提示
  + 人类可读正文"。把它塞进 `structuredContent` 要么丢错误语义，要么伪造一个与正文不一致的结构。
- **截断破坏 JSON 合法性**：执行链按 `MaxResultLength` 做 **JSON 感知截断**并追加截断标记；
  截断后的文本已不是合法 JSON。声明了 `outputSchema` 却回一个不满足它的 `structuredContent`，
  比不声明更糟（客户端会按 Schema 反序列化并失败）。
- **没有 `outputSchema` 的真相源**：编译期 `FeishuToolSchemas` 只描述**入参**；每枚工具的出参形态
  由各域执行器的投影代码决定，没有可对齐的声明面。手写 351 份输出 Schema 是第二真相源。

**协议合规性**：MCP 允许工具只回 text content；`outputSchema` 是可选的。

## 决策 2：不实现 `notifications/progress`

- **穿透面跨越仓库边界**：进度上报需要从 `AIFunction` 逐层传到生成工具链
  （`Mud.Feishu.AI.Tools` 的各域执行器 → `FeishuToolBinding` → 中立层 `Mud.AI.Agent`），
  而中立包按"零产品词汇 + 不依赖 `Mud.Feishu.*`"的准入条件治理——为一个可选能力加一条贯穿
  三层的管道，改动面与收益不成比例。
- **收益与现状不匹配**：工具结果有界（截断 + 只读重试策略），单次调用不产生"长时间无输出"的形态；
  取消已由 HTTP 请求中止与 stdio 断开覆盖（无需进度事件表达）。

## 后果

- 客户端拿到的是统一文本：成功结果是 JSON 文本（可自行解析），失败结果是错误契约行 + 恢复提示；
  决策依据写在 `feishu-errors://contract` 资源里，客户端读一次即可机械处置。
- **不因此拒绝 2025-06-18 修订**：该修订的其他差异（`title` / annotations 细化）已实现；
  加版判据只看 `initialize` / `tools/list` / `tools/call` / `resources/*` 的方法形态，
  结构化输出属"可选能力"，不影响版本声明。
- Runbook §6 登记指向本 ADR，避免运维把两者当缺陷追查。

## 复议条件

满足任一条件时重开本决策：

1. 执行链能提供**出参 Schema 的单一真相源**（与入参 Schema 同一 pass 生成）；
2. 截断策略改为"结构化结果不截断，或截断时保留可解析的分片信封"；
3. 出现真实的长耗时工具（分钟级）且客户端明确要求进度事件——此时按"穿透中立包"的成本重新评估，
   而不是在 MCP 传输层临时补一条流。
