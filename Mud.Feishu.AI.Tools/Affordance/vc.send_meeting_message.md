## WHEN

- 机器人要在会中消息面板发文本提示参会者，或用表情作出反馈。
- 消息对象是"此刻在场的会中成员"，不是某个群、某个人。

## Avoid when

- 要发群聊或单聊消息——用 `im.send_message`，出了会就没人看消息面板。
- 机器人不在会中发不了——先 `vc.join_meeting` 拿长数字会议 ID。
- 是想把人拉进会——用 `vc.invite_participants`，发言不等于邀请。

## Prerequisites

- `meeting_id` 只认长数字会议 ID（`vc.join_meeting` 返回或 `vc.user_active_meeting` 查询）；传 9 位会议号本地即拒，`dry_run` 也不例外。
- `msg_type` 仅 text 或 reaction 两个取值；reaction 时 `content` 放表情 key。

## Tips

- 消息只投递给当时在会成员，别把它当聊天记录广播。
- 平台按 `idempotency_key` 去重；省略时由服务端自动生成，等于没有去重保护，重发务必自带同一个键。
- 非法 `msg_type` 在下发前被本地拒绝，预演同样会拦。
