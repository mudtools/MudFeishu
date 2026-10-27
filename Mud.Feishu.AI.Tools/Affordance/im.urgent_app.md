## WHEN

- 消息已发出但对方可能没看，且确实需要"立刻被看到"：用飞书客户端弹窗强提醒。

## Avoid when

- 常规提醒、进度同步——用 `im.send_card` 传达要点即可，催办有打扰成本。
- 对方长时间不在飞书、弹窗够不着——那是 `im.urgent_sms`，短信打扰更重。
- 消息还没发出去——催办只认已存在的消息，先 `im.send_message`。

## Prerequisites

- `message_id` 须为本 Bot 已发送的消息（om_ 开头，来自 `im.send_message` 的回执）。
- `user_ids` 默认按 open_id 填（`user_id_type` 仅 open_id / user_id / union_id，非法值提前拒绝）；目标必须在该消息的接收范围内。

## Tips

- 回执的 invalid_user_id_list 明确给出"谁没被催到"，空数组也是有效结论，别靠猜。
- 没有幂等参数：重复调用会给对方再弹一次强提醒。
