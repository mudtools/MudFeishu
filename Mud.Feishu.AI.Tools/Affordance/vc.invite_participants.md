## WHEN

- 会议正在开，需要让指定的真人马上进场——本工具按人下发邀请。
- 用户说"把某某拉进这个会"时用，而不是把会议号转发出去让对方自己找。

## Avoid when

- 要让机器人自己进场做会中动作——那是 `vc.join_meeting`，邀请的对象是人。
- 是要交接主持权——用 `vc.set_host`，邀请不等于给管理权。
- 要让所有人退出——用 `vc.end_meeting`，与进场方向相反。

## Prerequisites

- `invitee_ids` 全部是 open_id：先用 `contact.search_user` 或 `contact.resolve_user` 把姓名换成 ID。
- `meeting_id` 是进行中的 9 位会议号；走 user 身份，宿主未放行 user 会启动即报错。

## Tips

- 一次最多 10 人是本地硬校验，超限在下发前就被拒，请分批邀请。
- 结果逐人回报，未标成功的按未邀到处理，别向用户声称全员已邀请。
- `user_type` 缺省 1（飞书用户），一般不必显式传。
