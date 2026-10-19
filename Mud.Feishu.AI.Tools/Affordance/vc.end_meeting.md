## WHEN

- 用户明确要求把这场正在开的会就地终止、所有人散会——需要硬停时。
- 判据：只有本工具能结束进行中的会议，撤预约或机器人退场都到不了这一步。

## Avoid when

- 会议还没开始、要撤掉的是预约——那是 `vc.delete_reserve`，对象是预约不是实时会议。
- 只想让机器人自己退出而会议继续——用 `vc.leave_meeting`。
- 只是换个主持人接管——用 `vc.set_host`，不必散会。

## Prerequisites

- 这里的 `meeting_id` 是 9 位会议号且会议须进行中：从 `vc.list_meetings`、`vc.get_meeting` 或 `vc.user_active_meeting` 取号。
- 走 user 身份：宿主未放行 user 时启动即报错；需 vc:meeting 权限点。

## Tips

- 不可逆：结束后全部参会人被移出，先 `dry_run` 预演并向用户复述会议号确认后再执行。
- 预演回执只给字段长度摘要、不回显会议号原文，核对对象要另走查询工具。
