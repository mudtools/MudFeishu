## WHEN

- 机器人的会中事务办完需要退场，而会议本身要继续。
- 需要撤回机器人的在场存在感——退的只是它自己，会议不变，再进场归 `vc.join_meeting` 管。

## Avoid when

- 要让会议终止、所有人被移出——用 `vc.end_meeting`，本工具做不到。
- 机器人还没入会——先 `vc.join_meeting`，没有在场就没有离会。
- 想走前在会中留一句话——那是 `vc.send_meeting_message` 的事。

## Prerequisites

- `meeting_id` 必须是长数字 ID：来自 `vc.join_meeting` 的返回或 `vc.user_active_meeting` 的查询结果。
- 传 9 位会议号会被本地校验拦下，连 `dry_run` 预演也绕不过这一步。

## Tips

- 离会后会议照常进行，别向用户报告"会议已结束"。
- 走租户身份的机器人动作，不需要放行 user——与 `vc.end_meeting` 不同。
