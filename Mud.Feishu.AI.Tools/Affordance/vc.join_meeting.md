## WHEN

- 需要机器人以成员身份进入进行中的会议，执行收集或会中动作。
- 它是会中三件套的入口：离会与会中消息只认这次拿到的会议 ID。

## Avoid when

- 要让真人进场——用 `vc.invite_participants`。
- 只想看会议详情——用 `vc.get_meeting`，入会会让全员看到机器人成员。
- 要终止会议——用 `vc.end_meeting`；机器人自己离开也结束不了会。

## Prerequisites

- `meeting_no` 是 9 位会议号，来自 `vc.list_meetings`、`vc.get_meeting` 或 `vc.user_active_meeting`。
- 会议开了密码必须传 `password`；`action` 只允许 1（仅加入已有会议）或 2（发起并加入日程会议）。

## Tips

- 立刻保存返回的长数字会议 ID：`vc.leave_meeting` 与 `vc.send_meeting_message` 只认它，9 位会议号会被拦下。
- 平台没回传该 ID 时，用 `vc.user_active_meeting` 按参会人取回。
- 入会对全员可见，先用 `dry_run` 预演确认。
