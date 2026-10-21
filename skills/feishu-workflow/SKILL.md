---
name: feishu-workflow
description: 飞书 workflow 域工具集：2 个工具（只读 2 / 写 0）。单域取数（只看妙记、只看日程）→ 用该域原子工具（`minutes.*` / `calendar.agenda`），
version: 1.0.0
package_version: 3.0.3
---

# 复合工作流（workflow.*）

## 路由优先级（先判断"一次问答要几个域"）
- **单域取数**（只看妙记、只看日程）→ 用该域原子工具（`minutes.*` / `calendar.agenda`），
  不要用工作流：工作流的价值是**跨调用固化顺序**，单域场景它只会多一层。
- **跨域或"一个动作要连取几段"**（妙记详情 + 统计 + 逐字稿；日程 + 我的任务）→ `workflow.*`。
- 判据是**要几次调用、几次决定**，不是措辞里的"总结""汇总"。

## 能力清单
- `workflow.meeting_summary`：`minutes.get` → `minutes.get_statistics`（可关）→ `minutes.get_artifacts`
  → 结构化报告（元信息 / 发言统计 / 逐字稿窗口 + `next_offset`）。
- `workflow.standup_report`：`calendar.agenda` → `task.list_my_tasks` → 站会材料（议程 / 我的任务 / 计数）。

## 前置链与续跑
- 两个工作流都返回 `steps` 数组（每步含 `tool` / `status` / `duration_ms` / `error`）与 `report`。
- **失败即停**：中途失败返回失败结果 + `resume_from=<步骤号>`；把该值填进 `start_at` 重调即可**断点续跑**，
  已成功的前序步骤会被标 `skipped`，不会重复取数。
- 长逐字稿：报告里逐字稿段落带 `has_more` 标记时，按 `next_actions` 给出的 `transcript_offset` 续读
  （不要用 `start_at` 干这件事——那是重跑整个链）。

## 避坑
- **`minutes.*` 没有摘要接口**：`workflow.meeting_summary` 给的是事实材料，摘要与待办**由你基于
  报告里的逐字稿段落写**；不要声称"平台已生成摘要"。
- `include_statistics=false` 时报告的 `statistics` 段为 `null`，`omitted` 字段会说明原因。
- `dry_run=true` 只返回执行计划（每步 `status=planned`）**不取数**，用于确认这条链会调哪些工具。
- 工作流的每一步都是**受控工具调用**：白名单未启用的步骤会让整链在**该步**失败（不会静默跳过），
  这是宿主策略边界的正确表现，应提示宿主把对应域加入启用集。
- 只读链：两个工作流**不写**任何数据（含生成文档）；需要落文档请用 `docx.*` 显式建。

## 安全规则
- 参数只传事实（token / ID / 时间窗），不要在工作流入参里塞自然语言指令。
- 只使用本域真实工作流名，不臆造；未列出的组合请用原子工具逐步执行。

## 示例
- 「昨天那场评审会的结论和待办」→ `workflow.meeting_summary(minute_token=…)`
  → 若 `has_more` 用 `transcript_offset` 续读 → 基于 transcript 写结论与待办。
- 「今天站会我要说什么」→ `workflow.standup_report(calendar_id=…, start=…, end=…)`。

## 命令（工具）清单
- `workflow.meeting_summary`（只读）：妙记速览工作流：给定 minute_token，一次完成「妙记元信息 + 发言统计 + 逐字稿窗口」的取数与结构化汇总，返回可读报告（含逐字稿 total_length / next_offset，长录音可按 offset 续读）。摘要与待办提取由你基于报告内容完成（飞书未提供摘要接口）。只读链，需 minutes:minutes:readonly。
- `workflow.standup_report`（只读）：站会报告工作流：给定日历与时间窗，取回窗内日程 + 我负责的任务，输出结构化站会材料（今日议程 / 未完成任务 / 建议关注项）。时间窗用 RFC3339（含时区）。只读链，需 calendar:calendar:readonly + task:task:readonly。
