---
name: feishu-workflow
description: 飞书 workflow 域工具集：2 个工具（只读 2 / 写 0）。要做「多源汇总成一份报告」→ `workflow.*`；单次查询/写入直接用对应域工具。
version: 1.0.0
package_version: 3.0.3
---

# 复合工作流（workflow.*）

## 路由优先级
- 要做「多源汇总成一份报告」→ `workflow.*`；单次查询/写入直接用对应域工具。
- 会议纪要汇总 → `workflow.meeting_summary`；站会日报 → `workflow.standup_report`。
- 两个工作流都是只读编排：内部按步骤调用分钟 / 日历 / 任务域的既有工具。

## 前置链
- 依赖工具必须已启用：会议汇总需 `minutes.search` / `minutes.get_artifacts`；
  站会日报需 `calendar.agenda` / `task.list_my_tasks`（宿主 `FeishuAgent:Tools` 白名单）。
- 先 `dry_run=true` 预演（只返回步骤与参数字段名，不调用任何下游工具），确认后再实跑。
- 失败时把结果里的 `resume_token` 与**同一组输入**一起回传，可跳过已完成步骤续跑。

## 避坑
- `resume_token` 绑定输入指纹：改了 query / 时间窗 / 数量上限后会失效并明确报错，需从头重跑。
- 结果受出站长度预算约束：调小 `max_meetings` / `max_tasks` 可获得更完整的报告。
- 依赖工具未启用 → `policy/tool_not_allowed`（文案含需加入白名单的工具名），不是平台故障。
- 会议汇总会带出参会人发言与待办（**PII**）：宿主默认不启用，需显式启用并确认合规口径。

## 安全规则
- 两个工作流均为只读，不修改任何数据；每一步都经完整执行链（授权 / 审计 / 脱敏）。
- 未启用的下游工具不会被执行；不要试图借工作流绕过白名单。
- 只用本域真实工具名；未策展能力先用 `feishu.capability_lookup` 确认「是没有还是没启用」。

## 示例
- 「汇总上周产品评审会」→ `workflow.meeting_summary`（query="产品评审"，配 start_time / end_time）。
- 「生成今天的站会日报」→ `workflow.standup_report`（calendar_id + 当天时间窗）。

## 命令（工具）清单
- `workflow.meeting_summary`（只读）：一键汇总一段时间内的会议纪要：按关键词与时间窗搜索妙记，再逐个提取 AI 摘要、章节、待办与关键词，合成结构化 Markdown 报告。报告含参会人发言与待办（PII），结果会进入第三方模型上下文：默认不启用，需宿主显式启用并确认合规口径。支持 dry_run 预演与 resume_token 断点续跑。只读，需 minutes:minutes:readonly。
- `workflow.standup_report`（只读）：一键生成站会日报：读取指定日历时间窗内的日程，再以当前用户身份列出「我负责的」任务，汇总为 Markdown 日报。依赖 calendar.agenda 与 task.list_my_tasks 同时启用，且宿主须提供当前用户 open_id（FeishuToolContext.UserId）并在 AllowedIdentities 放行 user。支持 dry_run 预演与 resume_token 断点续跑。只读，需 calendar:calendar:readonly…
