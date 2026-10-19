## WHEN

- 进行中的会议需要把主持人移交给指定用户，接管会后管理。
- 用户点名"让某某当主持人"时走这里，不是邀请也不是结束。

## Avoid when

- 目标人还在会外——先用 `vc.invite_participants` 请进场，再谈移交。
- 要散会——用 `vc.end_meeting`；要机器人进出场——用 `vc.join_meeting` 或 `vc.leave_meeting`。
- 只想查现在谁在会中——用 `vc.list_participants`，别用写操作探状态。

## Prerequisites

- `host_user_id` 是目标用户的 open_id：先用 `contact.resolve_user` 或 `contact.search_user` 解析。
- 这里 `meeting_id` 用 9 位会议号且会议须进行中——与机器人工具要求的长数字 ID 不是同一种。

## Tips

- 改设立即生效，本工具走租户身份，不依赖 user 放行。
- `user_type` 缺省 1（飞书用户），一般不用显式传。
- 先 `dry_run` 预演核对会议与目标用户，再关掉预演执行。
