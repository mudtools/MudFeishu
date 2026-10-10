# 邮件的回复与转发

## 前置链
1. `mail.search` 或 `mail.list_messages` 拿 `mail_id`
2. `mail.get_message` 确认对象（主题、发件人、会话线）
3. 需要落笔时 `mail.send_message`；只想标记已处理用 `mail.mark_read`

## 避坑
- **本域没有独立的「回复」/「转发」工具**：`mail.send_message` 只能**新建**邮件。
  要把原邮件带入新邮件，必须自行把原文要点写进正文——不要指望自动携带引用链。
- **`send_message` 是两步合一**（建草稿 → 发送）：工具内部完成，模型不感知草稿态；
  中途失败会返回结构化错误（含 draft_id 供排查），此时不要盲目重发——先确认是否已发出。
- `get_thread` 只回**摘要与成员数**，正文要按 `mail_id` 用 `get_message` 逐封取（避免整条会话灌入上下文）。
- 读完不 `mark_read` 会让用户邮箱持续堆积未读——这是可感知的副作用。
- 发件需**用户身份**：宿主须放行 `user`，否则写工具不可用（不是参数问题）。

## 示例
- 「回一封说收到」→ `mail.get_message` 拿上下文 → `mail.send_message`（正文写明回复对象与原要点）
- 「把这封转给张三」→ `contact.search_user` 拿 open_id → `mail.send_message`（收件人为张三，正文附原文要点）