## WHEN

- 要在某个工单下直接回复客户或补充进展说明，内容已确定、需要立刻进入工单会话。
- 以客服口径在工单语境内发言——不是脱离工单的主动通知。

## Avoid when

- 想回传 FAQ 命中让服务台自动答复——用 `helpdesk.answer_user_query`，别重复人工外发。
- 要发图片或卡片——工单消息面不收，用 `helpdesk.create_bot_message`。
- 还没确认工单现状——先 `helpdesk.get_ticket` 看状态与处理人。

## Prerequisites

- ticket_id 来自服务台或事件回调，本域没有查工单列表的工具。
- msg_type 只接受 text 或 post，非法值在预演之前就会被执行器拦下。

## Tips

- msg_type=post 时 content 为 JSON 字符串，回执只回 message_id，原文不回显。
- 消息发出即进工单会话、不可撤回；可先 `dry_run` 核对内容长度再执行。
