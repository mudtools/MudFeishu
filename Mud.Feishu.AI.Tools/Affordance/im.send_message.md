## WHEN

- 向指定群聊或个人发一条纯文本，是 im 域的默认外发通道。

## Avoid when

- 这句话有特定指向的消息——那是 `im.reply_message`。
- 通知类、要按钮或要点排版——那是 `im.send_card`。
- 送图片或文件——那是 `im.send_image` / `im.send_file`。
- 发出去的内容有错要改——那是 `im.edit_message`，别补发刷屏。

## Prerequisites

- `receive_id` 与 `receive_id_type` 形态必须匹配：oc_ 开头是群（缺省 chat_id），open_id 是个人，先 `contact.search_user` 拿。
- 首次面向陌生接收者，先以 `dry_run` 预演核对目标。

## Tips

- 回执的 message_id 是后续 `im.edit_message` / `im.revoke_message` / `im.urgent_app` 的锚点，保存好。
- `idempotency_key` 用稳定值（如单据号+动作）才有防重意义，随机数等于没传；该键不跨工具共享。
