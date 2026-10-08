
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
