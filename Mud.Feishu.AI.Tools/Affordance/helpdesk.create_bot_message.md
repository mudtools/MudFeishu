## WHEN

- 要以服务台机器人的身份主动推送通知（进展同步、结果告知）给某个会话或用户。
- 需要发图片（image）或卡片（interactive）做富媒体通知时，也走本工具。

## Avoid when

- 要在某工单语境下回复——用 `helpdesk.create_ticket_message`；机器人消息不挂在工单上。
- 要回传提问的 FAQ 命中——用 `helpdesk.answer_user_query`，那是回传不是发消息。

## Prerequisites

- receiver_id 的含义由 receive_type 决定：chat=会话、user=用户（默认 chat），配错就发错对象。
- content 是 JSON 字符串，形态随 msg_type（text / post / image / interactive）。

## Tips

- 工单消息面只收 text/post；要发图片或卡片必须用本工具。
- 回执只给 message_id，不回显内容，也查不到对方是否已读。
