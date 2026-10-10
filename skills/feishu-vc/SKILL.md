---
name: feishu-vc
description: 飞书 vc 域工具集：10 个工具（只读 6 / 写 4）。会议本身（谁参加了、开了多久、有没有录制、会议室）→ `vc.*`；会议纪要内容 → `minutes.*`；
version: 1.0.0
---

# 视频会议（vc.*）

## 路由优先级（先判断对象属于哪个域）
- **会议本身**（谁参加了、开了多久、有没有录制、会议室）→ `vc.*`；**会议纪要内容** → `minutes.*`；
  **日程/会议室忙闲** → `calendar.*`（`vc.list_rooms` 只给会议室清单，不给忙闲）。
- 「最近开了什么会」用 `vc.list_meetings`（**历史统计视角**，按时间范围查）；
  「这个会现在什么情况」用 `vc.get_meeting`（单个会议当前详情）。两者不是同一份数据。
- 参会人明细必须用 `vc.list_participants`，它按**会议号 + 会议时段**定位，不接受 meeting_id。

## 前置链
`vc.list_meetings`（拿会议号/时段）→ `vc.get_meeting`（详情）/ `vc.list_participants`（明细）/
`vc.get_recording`（录制）；会议室 `vc.list_rooms`；预约 `vc.list_reserves` → `vc.delete_reserve`。
写面：`vc.set_host` / `vc.invite_participants` / `vc.end_meeting`（均作用于**进行中的会议**）。

## 避坑
- **会议号 ≠ meeting_id**：`vc.get_meeting` / `vc.get_recording` 收的是 9 位会议号；
  `vc.list_participants` 收的也是会议号，但**必须同时给会议起止时间**（平台按时段定位）。
- 会议**未录制**时 `vc.get_recording` 返回空记录（`available=false`）——如实告知，不要臆造链接。
- `vc.invite_participants` 一次**最多 10 人**；超量会被本地拒绝，请分批。
- **`vc.invite_participants` / `vc.end_meeting` 是用户身份工具**：宿主须在
  `FeishuAgent:AllowedIdentities` 放行 `user`，否则**启动即报错**（不是运行期被拒）。
- `vc.end_meeting` **不可逆**：会把所有参会人移出会议——必须先 `dry_run=true` 并与用户确认会议号。
- 本域**没有**创建预约的工具（`apply_reserve` 未策展）：用户要"约个会"时用 `calendar.create_event`，
  但要**如实说明**"无法通过本工具创建 VC 预约"。

## 安全规则
- 写工具首次调用一律 `dry_run=true` 预演；确认 method/path 与字段摘要无误后，以 `dry_run=false` 重放**同一参数**。
- high-risk-write 工具（`vc.delete_reserve`）须宿主注册 `IToolExecutionAuthorizer` 才会放行，未注册时一律拒绝。
- 参会人明细已做**最小披露**投影（不含手机号/邮箱/内网 IP）：不要向用户声称"我可以查与会者联系方式"。
- 只使用本域真实工具名，不臆造不存在的工具或 API。

## 示例
- 「上周有哪些会」→ `vc.list_meetings(start_time, end_time)`
- 「这个会的参会人和录制」→ `vc.get_meeting` 拿会议号与时段 → `vc.list_participants` → `vc.get_recording`
- 「把张三设成主持人」→ `contact.search_user` → `vc.set_host(dry_run=true)` → 确认后去掉 dry_run

## 命令（工具）清单
- `vc.delete_reserve`（写）：删除一个预约（Reserve）——预约对应的会议号随即失效且不可恢复。high-risk-write：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 vc:reserve。**务必先 dry_run=true 预演（用 vc.list_reserves 确认对象）**。
- `vc.end_meeting`（写）：结束一个进行中的会议（对所有参会人立即生效，不可逆——你会把所有人移出会议）。**用户身份工具**：宿主须在 FeishuAgent:AllowedIdentities 放行 user。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 vc:meeting。**务必先 dry_run=true 预演并与用户确认会议号**。
- `vc.get_meeting`（只读）：按会议 ID（9 位会议号）获取单个会议的当前详情：主题、会议号、入会链接、密码、主持人、参会人数、累计参会人数、会议状态、关联会议产物。要历史列表用 vc.list_meetings；要参会人明细用 vc.list_participants。只读，需 vc:meeting:readonly。
- `vc.get_recording`（只读）：获取指定会议的录制文件信息（下载地址 url 与时长 duration）。会议未录制或录制仍在处理中时返回空记录——此时如实告知用户『该会议没有可用录制』，不要臆造链接。只读，需 vc:recording:readonly。
- `vc.invite_participants`（写）：邀请用户加入进行中的会议（一次性最多 10 人，返回每人邀请是否成功）。invitee_ids 为 open_id 列表（先用 contact 解析）。**用户身份工具**：宿主须在 FeishuAgent:AllowedIdentities 放行 user，否则启动即报错。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 vc:meeting。建议先 dry_run 预演确认会议与人员。
- `vc.list_meetings`（只读）：按时间范围查询会议列表（历史/统计视角）：会议号、主题、起止时间、时长、参会人数、是否录制、是否外部会议。start_time/end_time 为必填的 ISO8601 带时区时间（如 2026-10-01T00:00:00+08:00），单次范围建议不超过 30 天。只读，需 vc:meeting:readonly。
- `vc.list_participants`（只读）：按『会议号 + 会议时段』查询参会人明细列表：姓名、部门、用户 ID、入会/离会时间、在会时长、设备/IP/网络、音频视频共享状态、是否外部参会人。注意：本接口用 meeting_no(9 位会议号) + meeting_start_time/meeting_end_time 定位（不是 meeting_id），三者均必填。只读，需 vc:meeting:readonly。
- `vc.list_reserves`（只读）：按预约 ID 获取预约（Reserve）详情：预约 ID、9 位会议号、入会链接、app_link、直播链接、密码、预约人、结束时间、失效状态、会议设置（主题/自动录制等）。只读，需 vc:reserve:readonly。
- `vc.list_rooms`（只读）：分页列出会议室：room_id、展示 ID（display_id）、名称、容纳人数、层级路径与所属层级 ID。找空档前先用它确认会议室范围（会议室忙闲请用 calendar.find_free_slots 传 room_id）。只读，需 vc:room:readonly。
- `vc.set_host`（写）：把进行中会议的主持人改设为指定用户（会中管理动作，立即生效）。host_user_id 为该用户的 open_id（先用 contact.resolve_user / contact.search_user 解析）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 vc:meeting。建议先 dry_run 预演确认目标会议与用户。
