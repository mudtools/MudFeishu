
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
3. FAQ 列表分页：`has_more=true` 时带 `page_token`；一次拿全用 `fetch_all=true`（配 `max_items`）

## 避坑
- **三个「知识」别混**：本域 FAQ 是**服务台自助问答**；`knowledge.search` 是 Aily 数据知识；`wiki.*` 是云文档知识空间。选错会拿到"像答案但不是服务台口径"的内容。
- `helpdesk.get_ticket` **只返回状态与流转事实**：备注正文、工单对话、客户自定义字段值，以及客户 / 客服的**邮箱 · 部门 · 城市 · 国家**都不返回。不要据此断言"工单里没有细节"，也不要承诺"帮你看工单里写了什么"。
- 工单时间字段以 `_ms` 结尾 = **毫秒时间戳**（不是秒）。
- FAQ 列表里的 `answer` 是**摘要**（已截断）；要全文用 `helpdesk.get_faq`。
- 空结果常见于"服务台还没建知识库"，不是调用失败——读哨兵里的提示再决定下一步。
- 本域权限常见要求**仅自建应用**：应用商店应用可能拿不到 FAQ / 工单读取。

## 安全规则
- 6 个工具全部**只读**：FAQ 与分类需服务台 FAQ 只读权限、工单需工单只读权限、技能组需客服只读权限（准确 scope 名见《工具权限对照表》#122~127）。
  ⚠️ 本域 SDK 注释未给 scope 字面量，开通前请在开发者后台按名称核对。
- 出站净化：不返回 FAQ 的创建人 / 更新人；工单相关人只给 `user_id` 与姓名，**不带邮箱 / 部门 / 城市 / 国家**。
- 工单正文与客户对话**未纳入工具面**：需要时走服务台的人工流程，不要让模型代读客户对话。
- 软缺席：宿主未启用服务台 API 时返回明确的"未注册客户端"说明，不要反复重试。

## 示例
- 「发票怎么开」→ `helpdesk.search_faqs(query="发票")` → 命中后 `helpdesk.get_faq(faq_id=…)`
- 「工单 694… 处理到哪了」→ `helpdesk.get_ticket(ticket_id="694…")`（只回答状态与时间线）
- 「服务台有哪些技能组」→ `helpdesk.list_agent_skills()`
