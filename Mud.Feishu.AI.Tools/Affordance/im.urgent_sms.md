## WHEN

- 极端紧急且对方大概率离线：客户端通知加手机短信双通道，确保触达到手机。

## Avoid when

- 对方可能人在飞书边——先用 `im.urgent_app`，短信打扰与成本更高一档。
- 内容本身不够醒目——先把事说清楚（`im.send_card`），加急解决不了内容问题。
- 对方大概率还在线——`im.urgent_app` 的弹窗就够了，不必上短信。

## Prerequisites

- `message_id` 是本 Bot 经 `im.send_message` 已发出的 om_ 消息。
- `user_ids` 为目标 ID 数组，`user_id_type` 仅 open_id / user_id / union_id（缺省 open_id），非法值在下发短信前就报错。

## Tips

- 短信直达手机：发错是对真人的实际打扰，先 `dry_run` 核对目标数量与 ID 形态。
- 加急不可撤销、无幂等：重复调用会重复发短信，发出去只能事后解释。
