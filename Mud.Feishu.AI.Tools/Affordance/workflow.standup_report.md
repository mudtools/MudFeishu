## WHEN

- 要生成当日/本周**站会日报**：时间窗内的日程 + 当前用户负责的任务。
- 需要一份可直接粘贴到群里的 Markdown 汇总。

## Avoid when

- 只看日程 → `calendar.agenda`；只看任务 → `task.list_my_tasks`。
- 要跨用户汇总 → 本工具以当前用户身份读「我负责的」任务。

## Prerequisites

- `calendar.agenda` 与 `task.list_my_tasks` 已启用（宿主 `FeishuAgent:Tools`）。
- 具备 `calendar:calendar:readonly` + `task:task:readonly`；`calendar_id` 为群日历/共享日历 ID。

## Tips

- 首次先 `dry_run=true` 预演步骤；依赖未启用会返回 `policy/tool_not_allowed`。
- `include_completed_tasks=true` 可把已完成任务纳入；`max_tasks` 默认 20（1~100）。
- 失败后带 `resume_token` + 同一组输入重跑，可跳过已完成的日程步骤。
