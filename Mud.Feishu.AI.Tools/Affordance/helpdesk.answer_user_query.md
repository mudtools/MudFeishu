## WHEN

- 服务台里出现一条用户提问事件，你已检索到与之匹配的 FAQ，要把命中结果回传给服务台。
- 让服务台据此向用户呈现答复——回传的是命中，不是你组织的消息。

## Avoid when

- 要自己在工单里回复用户——用 `helpdesk.create_ticket_message`；本工具不发消息。
- 会话外的主动通知——用 `helpdesk.create_bot_message`。
- 还没定位到 FAQ——先 `helpdesk.search_faqs` 找候选，不要凭记忆编 faq_ids。

## Prerequisites

- event_id 必填且只能来自服务台事件回调——工具面查不到它。
- ticket_id 来自服务台或回调；faq_ids 取自 `helpdesk.search_faqs` / `helpdesk.list_faqs`。

## Tips

- 回执只有 answered 与命中数：不会出现用户消息，也没有送达状态可查。
- faq_ids 可不传；空命中同样是一次有效回传。
