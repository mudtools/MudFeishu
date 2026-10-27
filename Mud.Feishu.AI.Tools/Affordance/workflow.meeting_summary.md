## WHEN

- 要把一段时间内**多场**会议（妙记）的摘要、章节与待办汇总成一份报告。
- 需要 AI 摘要 + 统一格式化输出，而不是逐场手工整理。

## Avoid when

- 只要单场纪要 → `minutes.get_artifacts`；要搜妙记 → `minutes.search`。
- 要自由裁剪步骤或换输出格式 → 直接组合 `minutes.*` 工具。

## Prerequisites

- `minutes.search` 与 `minutes.get_artifacts` 已启用（宿主 `FeishuAgent:Tools`）。
- 具备 `minutes:minutes:readonly`；内容含参会人发言与待办（**PII**），宿主默认不启用。

## Tips

- 首次先 `dry_run=true` 预演步骤；依赖未启用会返回 `policy/tool_not_allowed`。
- `max_meetings` 默认 5（1~20）；报告被截断时调小它可拿到完整内容。
- 失败后带 `resume_token` + 同一组输入重跑，可跳过已完成的搜索步骤。
