---
name: feishu-minutes
description: 飞书 minutes 域工具集：5 个工具（只读 5 / 写 0）。会议纪要 / 妙记内容 → `minutes.*`；会议本身（参会人、录制、预约、会议室）属会议域，
version: 1.0.0
---

# 妙记（minutes.*）

## 路由优先级（先判断对象属于哪个域）
- **会议纪要 / 妙记内容** → `minutes.*`；**会议本身**（参会人、录制、预约、会议室）属会议域，
  本宿主未策展时先用 `feishu.capability_lookup` 确认「是没有还是没启用」，不要臆造调用。
- 只要**结论 / 待办 / 章节** → `minutes.get_artifacts`；要先确认「是不是这场会」→ `minutes.get`；
  不知道是哪场会 → `minutes.search`。

## 前置链
（有 token）`minutes.get` 取妙记元信息（标题、时长、链接、所有者）确认对象 → `minutes.get_artifacts` 取
总结 / 章节 / 待办 / 关键词 / 逐字稿。
（无 token）`minutes.search` 检索 → `minutes.get` 确认 → `minutes.get_artifacts`。

## 找会议（拿不到 token 时）
- **没有 token 也能找**：`minutes.search` 按关键词（10~50 字符）+ 所有者 / 参会人 / 创建时间检索，
  时间范围最大 1 个月；再用 `minutes.get` 确认对象。
- 只有用户**明确给了链接或 token** 时才直接 `minutes.get`，不要跳过搜索去猜 token。

## 避坑
- **逐字稿很长**：`minutes.get_artifacts` 的逐字稿**按窗口返回**（默认 1 万字符），
  结果里带 `transcript_total_length` / `has_more` / `next_offset`；需要后续内容时**带 `transcript_offset` 再调一次**，
  `has_more=false` 即读完。优先用总结 / 章节 / 待办回答，逐字稿只在用户明确要原话时读。
- **媒体只是链接**：`minutes.get_media` 返回的是有效期 1 天的下载 URL（文本），不含音视频内容本身；
  需要文件时把链接交给用户或宿主落盘。
- token 不存在时工具返回明确的「未找到」，据此如实告知用户，不要换着 token 反复试。

## 安全规则
- 本域全为只读工具（无写操作、无 `dry_run` 语义）；调用仍受 scope 审计约束（需 `minutes:minutes:readonly`）。
- 只使用本域真实工具名，不臆造不存在的工具或 API。

## 示例
- 「总结上周例会」→ 用户给链接 → `minutes.get` 确认 → `minutes.get_artifacts`。
- 「上个月的复盘会都聊了什么」→ `minutes.search`（创建时间窗 ≤ 1 个月）→ `minutes.get` 确认 → `minutes.get_artifacts`。

## 命令（工具）清单
- `minutes.get`（只读）：按 minute_token 获取妙记的元信息（标题、时长、链接、创建时间、所有者）。拿到 token 后先用它确认是不是目标会议，再用 minutes.get_artifacts 取总结与待办。只读，需 minutes:minutes:readonly。
- `minutes.get_artifacts`（只读）：按 minute_token 获取妙记的智能产物：AI 总结、章节摘要、待办事项、关键词——'总结这周会议'这类请求的首选入口（无需自行归纳全文）。逐字稿按窗口返回（transcript_offset/transcript_limit，默认 1 万字符），并给出 transcript_total_length 与 has_more/next_offset，可据此续读后续片段。只读，需 minutes:minutes:readonly。
- `minutes.get_media`（只读）：按 minute_token 获取妙记音视频文件的下载链接（有效期 1 天）。返回的是需宿主落盘的临时 URL，不含二进制内容。只读，需 minutes:minutes:readonly。
- `minutes.get_statistics`（只读）：按 minute_token 获取妙记的访问统计数据（PV、UV、访问用户列表与访问时间）。用于了解妙记的访问情况。只读，需 minutes:minutes:readonly。
- `minutes.search`（只读）：按关键词、所有者、参与者与创建时间搜索妙记列表（分页）。query 关键词 10~50 字符；至少提供一个过滤条件。搜索时间范围最大 1 个月。只读，需 minutes:minutes:readonly。
