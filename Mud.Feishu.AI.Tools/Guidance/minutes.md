
# 妙记（minutes.*）

## 路由优先级（先判断对象属于哪个域）
- **会议纪要 / 妙记内容** → `minutes.*`；**会议本身**（参会人、录制、预约、会议室）属会议域，
  本宿主未策展时先用 `feishu.capability_lookup` 确认「是没有还是没启用」，不要臆造调用。
- 只要**结论 / 待办 / 章节** → `minutes.get_artifacts`；要先确认「是不是这场会」→ `minutes.get`。

## 前置链
`minutes.get` 取妙记元信息（标题、时长、链接、所有者）确认对象 → `minutes.get_artifacts` 取
总结 / 章节 / 待办 / 关键词 / 逐字稿。

## 避坑
- **本域没有「列出我的妙记」工具**：`minute_token` 必须由用户给出（粘贴链接或 token），拿不到就先问。
- **逐字稿很长**：原样取会挤占上下文，优先用 `get_artifacts` 的结论与待办，逐字稿已按预览长度截断并标记。
- token 不存在时工具返回明确的「未找到」，据此如实告知用户，不要换着 token 反复试。

## 安全规则
- 本域全为只读工具（无写操作、无 `dry_run` 语义）；调用仍受 scope 审计约束（需 `minutes:minutes:readonly`）。
- 只使用本域真实工具名，不臆造不存在的工具或 API。

## 示例
「总结上周例会」→ 用户给链接 → `minutes.get` 确认 → `minutes.get_artifacts`。
