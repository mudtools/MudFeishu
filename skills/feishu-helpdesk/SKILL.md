---
name: feishu-helpdesk
description: 飞书 helpdesk 域工具集：10 个工具（只读 6 / 写 4）。「这个问题有没有现成答复」→ `helpdesk.search_faqs`（服务台 FAQ 是客服口径的知识源）
version: 1.0.0
package_version: 3.0.3
---

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

## 命令（工具）清单
- `helpdesk.answer_user_query`（写）：回传用户提问命中的 FAQ（event_id + 命中的 FAQ 列表）——用于「用户问的问题对应哪个 FAQ」的场景；注意它只回传命中结果，不直接给用户发消息。写操作：默认不启用，需宿主授权，需 helpdesk:ticket（⚠️ scope 名待控制台核对）。
- `helpdesk.create_bot_message`（写）：以服务台机器人身份发一条消息给指定会话或用户（msg_type 支持 text/post/image/interactive）——「主动通知用户」。写操作：默认不启用，需宿主授权，需 helpdesk:ticket（⚠️ scope 名待控制台核对）。
- `helpdesk.create_ticket_message`（写）：在工单里发一条消息（msg_type=text 纯文本 / post 富文本）——「回复工单」。⚠️ 富文本内容不出站（回执只给 message_id）；客户对话原文仍不走工具面。写操作：默认不启用，需宿主授权，需 helpdesk:ticket（⚠️ scope 名待控制台核对）。
- `helpdesk.get_faq`（只读）：按 faq_id 查询单条服务台 FAQ 的完整内容（问题/答案全文/富文本片段/分类/标签/生效与过期时间）。⚠️ 不返回创建人与更新人（出站净化）。只读，需 helpdesk:faq:readonly。
- `helpdesk.get_ticket`（只读）：按 ticket_id 查询工单的状态与流转信息（状态/类型/渠道/评分/是否解决/创建·更新·关闭时间/标签/客户与客服的 user_id 与姓名/处理时长）。⚠️ 不返回备注正文、对话消息与客户联系方式（工单内容属敏感面，另行评审）。只读，需 helpdesk:ticket:readonly。
- `helpdesk.list_agent_skills`（只读）：列出服务台的客服技能（技能组）配置（skill_id/name/is_default）——回答「服务台配了哪些技能组」「这类问题该转给哪一组」。只读，需 helpdesk:agent:readonly。
- `helpdesk.list_categories`（只读）：列出服务台 FAQ 分类目录树（category_id/name/parent_id/children）——用于回答「服务台都有哪些分类」或拿 category_id 收窄 helpdesk.list_faqs。只读，需 helpdesk:faq:readonly。
- `helpdesk.list_faqs`（只读）：分页列出服务台 FAQ（可选按 category_id / status 过滤，或用 search 做粗过滤）——用于浏览「这个服务台有哪些常见问题」；按关键词找具体答复请用 helpdesk.search_faqs。只读，需 helpdesk:faq:readonly。
- `helpdesk.search_faqs`（只读）：按关键词检索飞书服务台 FAQ（返回 faq_id/问题/答案摘要/分类/标签）——回答「这个问题客户服务台有没有现成答复」的首选；与 knowledge.search（Aily 知识）、wiki.*（云文档知识库）数据源不同，别混用。只读，需 helpdesk:faq:readonly。
- `helpdesk.start_service`（写）：把工单转人工服务（可指定客服；返回 chat_id 与 ticket_id）——「机器人答不了，转人工」的第一跳。写操作：默认不启用，需宿主授权（IToolExecutionAuthorizer），需 helpdesk:ticket（⚠️ scope 名待控制台核对）。
