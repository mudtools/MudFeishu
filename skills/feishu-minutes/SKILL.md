---
name: feishu-minutes
description: 飞书 minutes 域工具集：10 个工具（只读 6 / 写 4）。会议纪要 / 妙记内容 → `minutes.*`；会议本身（参会人、录制、预约、会议室）属会议域，
version: 1.0.0
package_version: 3.0.3
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

## 命令（工具）清单
- `minutes.clip`（写）：从已完成的妙记按时间段切出一段新妙记，返回新妙记链接。time_ranges 每行一段、格式「起始|结束」的 ISO 8601 带时区时间（如 2026-03-21T16:15:30+08:00|2026-03-21T16:25:00+08:00），1~50 段、每段须大于 1000 毫秒，重叠或相邻区间平台自动合并。⚠️ 异步生成：调用成功只表示已提交，转写与音视频在后台产出。用户身份工具：宿主须在 FeishuAgent:AllowedIdentities 放行 user…
- `minutes.get`（只读）：按 minute_token 获取妙记的元信息（标题、时长、链接、创建时间、所有者）。拿到 token 后先用它确认是不是目标会议，再用 minutes.get_artifacts 取总结与待办。只读，需 minutes:minutes:readonly。
- `minutes.get_artifacts`（只读）：按 minute_token 获取妙记的智能产物：AI 总结、章节摘要、待办事项、关键词——'总结这周会议'这类请求的首选入口（无需自行归纳全文）。逐字稿按窗口返回（transcript_offset/transcript_limit，默认 1 万字符），并给出 transcript_total_length 与 has_more/next_offset，可据此续读后续片段。只读，需 minutes:minutes:readonly。
- `minutes.get_media`（只读）：按 minute_token 获取妙记音视频文件的下载链接（有效期 1 天）。返回的是需宿主落盘的临时 URL，不含二进制内容。只读，需 minutes:minutes:readonly。
- `minutes.get_statistics`（只读）：按 minute_token 获取妙记的访问统计数据（PV、UV、访问用户列表与访问时间）。用于了解妙记的访问情况。只读，需 minutes:minutes:readonly。
- `minutes.get_transcript`（只读）：按 minute_token 导出妙记的完整逐字稿文件（txt/srt）并交宿主落盘，结果只回 local_path/size/content_type——字节不进上下文（no-bytes-in-context）。要『会议讲了什么』先用 minutes.get_artifacts（AI 总结/章节/待办，直接进上下文）；只有需要逐字原文或按时间戳定位原话才用本工具。只读，需 minutes:minutes.transcript:export。
- `minutes.search`（只读）：按关键词、所有者、参与者与创建时间搜索妙记列表（分页）。query 关键词 10~50 字符；至少提供一个过滤条件。搜索时间范围最大 1 个月。只读，需 minutes:minutes:readonly。
- `minutes.subscribe`（写）：订阅妙记变更事件（当前 event_type 闭集只有 minutes.minute.generated_v1 = 妙记生成事件），订阅后该用户新产生的妙记会以事件回调推给应用。用户身份工具：宿主须在 FeishuAgent:AllowedIdentities 放行 user，且事件通道已接入（WebSocket/Webhook）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 minutes:minutes.basic:…
- `minutes.unsubscribe`（写）：取消订阅妙记变更事件（与 minutes.subscribe 成对；取消后该用户的妙记生成事件不再推给应用）。用户身份工具：宿主须在 FeishuAgent:AllowedIdentities 放行 user。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 minutes:minutes.basic:read。
- `minutes.upload`（写）：把云盘上的音视频文件导入生成妙记，返回新妙记链接。file_token 是云盘文件 token（需当前用户对该文件有下载权限）。支持音频 wav/mp3/m4a/aac/ogg/wma/amr、视频 avi/wmv/mov/mp4/m4v/mpeg/ogg/flv；时长 ≤6 小时、文件 ≤6GB。本工具只接受 file_token，不搬运字节。用户身份工具：宿主须在 FeishuAgent:AllowedIdentities 放行 user。写操作：默认空名单不启用，启用前…
