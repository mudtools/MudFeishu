
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
- **要逐字原文或时间轴才导出**：`minutes.get_transcript`（txt/srt，落盘回 `local_path`）；「讲了什么」仍走 `get_artifacts`。
- **剪辑是异步生成**：`minutes.clip` 调用成功只表示已提交，转写与音视频在后台产出，回执给的是新妙记链接。
- token 不存在时工具返回明确的「未找到」，据此如实告知用户，不要换着 token 反复试。

## 安全规则
- 读面全为只读；**写面 4 枚**（`clip` / `upload` / `subscribe` / `unsubscribe`）都是**用户身份**工具——
  宿主须在 `FeishuAgent:AllowedIdentities` 放行 user，否则会因身份被拒；首次调用一律 `dry_run=true` 预演。
- `get_transcript` 是**导出文件**出口：逐字稿字节落宿主盘，结果只回 `local_path`（no-bytes-in-context）。
- 只使用本域真实工具名，不臆造不存在的工具或 API。

## 示例
- 「总结上周例会」→ 用户给链接 → `minutes.get` 确认 → `minutes.get_artifacts`。
- 「上个月的复盘会都聊了什么」→ `minutes.search`（创建时间窗 ≤ 1 个月）→ `minutes.get` 确认 → `minutes.get_artifacts`。
