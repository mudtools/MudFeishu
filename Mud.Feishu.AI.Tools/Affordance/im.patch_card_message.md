## WHEN

- 本 Bot 已发的卡片情况变了（待办办结、审批出结果）：整卡覆盖更新状态，不再补发新消息。

## Avoid when

- 原消息是纯文本——那是 `im.edit_message` 的地盘。
- 卡片还没发出去——先 `im.send_card`，本工具必须有既存 `message_id` 才能落。
- 要让卡片消失——那是 `im.revoke_message`。

## Prerequisites

- `message_id` 来自 `im.send_card` 的回执。
- `body` 与 `buttons` 按逐行结构化语法写（text: / quote: / code: / divider；按钮 文本|url|… 或 文本|value|…），不要手写 JSON。

## Tips

- 非法语法组合在编译期就报错并附合法组合，不进下游；先 `dry_run` 看编译出的卡片结构再落写。
- 整卡覆盖天然幂等：重复提交相同内容结果一致。
