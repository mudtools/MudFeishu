
# Helpdesk · 飞书服务台

## 路由优先级
- 「这个问题有没有现成答复」→ `helpdesk.search_faqs`（服务台 FAQ 是**客服口径**的知识源）
- 「服务台有哪些常见问题」→ `helpdesk.list_faqs`（可按分类 / 状态浏览）
- 要看某条问答全文 → `helpdesk.get_faq`
- 「都有哪些分类」→ `helpdesk.list_categories`
- 「工单处理到哪了 / 谁在处理 / 评分多少」→ `helpdesk.get_ticket`
- 「这类问题该转给哪个技能组」→ `helpdesk.list_agent_skills`

## 前置链
1. `faq_id` 来自 `search_faqs` / `list_faqs`；`category_id` 来自 `list_categories`
2. `ticket_id` 来自服务台界面或事件回调——本域**没有工单列表工具**，必须先有 ID
3. FAQ 列表分页：`has_more=true` 带 `page_token`；一次拿全用 `fetch_all=true`

## 避坑
- **三个「知识」别混**：本域 FAQ 是**服务台自助问答**；`knowledge.search` 是 Aily 数据知识；`wiki.*` 是云文档知识空间。选错会拿到"像答案但不是服务台口径"的内容。
- `get_ticket` **只返回状态与流转事实**：备注正文、工单对话、客户/客服的**邮箱 · 部门 · 城市 · 国家**都不返回。不要承诺"帮你看工单里写了什么"。
- 工单时间字段以 `_ms` 结尾 = **毫秒时间戳**（不是秒）。
- FAQ 列表里的 `answer` 是**摘要**；要全文用 `helpdesk.get_faq`。
- 空结果常见于"服务台还没建知识库"，不是调用失败。
- 本域权限常见要求**仅自建应用**：应用商店应用可能拿不到 FAQ / 工单读取。

## 写面
- 4 个写工具（**全部 tenant 侧**）：`start_service`（转人工）/ `answer_user_query`（回传 FAQ 命中）/ `create_ticket_message`（工单内回复）/ `create_bot_message`（机器人主动发消息）。
- **scope 待核对**：SDK 注释只给自然语言，按官方文档取 `helpdesk:ticket`（⚠️ 控制台开通前请按名称核对）。
- `answer_user_query` **不直接给用户发消息**——只回传命中结果；要回复用户用 `create_ticket_message`。
- `create_ticket_message` 的富文本（`msg_type=post`）**内容不出站**：回执只给 `message_id`；**客户对话原文仍不进工具面**（发与读是两回事，不要让模型代读客户对话）。
- 其余 26 个写方法**未纳入**：全在 User 侧接口（需 `user_access_token` + 用户身份上下文，Notification/Event 还需服务台 token）；其中 7 个命中危险词（`delete_faq` / `delete_category` / `delete_agent_skill` / `delete_agent_schedule` / `delete_customized_field` / `cancel_notification_send` / `cancel_notification_approve`），纳入时必须各配四节 affordance。

## 安全规则
- 6 只读 + 4 写；权限点见《工具权限对照表》#122~131。
- 出站净化：不返回 FAQ 的创建人 / 更新人；工单相关人只给 `user_id` 与姓名，**不带邮箱 / 部门 / 城市 / 国家**。
- 软缺席：宿主未启用服务台 API 时返回"未注册客户端"说明，不要反复重试。

## 示例
- 「发票怎么开」→ `helpdesk.search_faqs(query="发票")` → 命中后 `helpdesk.get_faq(faq_id=…)`
- 「工单 694… 处理到哪了」→ `helpdesk.get_ticket(ticket_id="694…")`（只回答状态与时间线）
- 「机器人答不了，转人工」→ `helpdesk.start_service(open_id="ou_…", dry_run=true)` → 确认后执行
