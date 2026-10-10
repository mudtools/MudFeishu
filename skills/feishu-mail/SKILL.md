---
name: feishu-mail
description: 飞书 mail 域工具集：7 个工具（只读 5 / 写 2）。邮件（mailbox / thread）→ `mail.*`；即时消息 → `im.*`。同一收件人不代表同一渠道。
version: 1.0.0
---

# 邮件（mail.*）

## 路由优先级（先判断对象属于哪个域）
- **邮件**（mailbox / thread）→ `mail.*`；**即时消息** → `im.*`。同一收件人不代表同一渠道。
- 找「谁发来的合同」优先 `mail.search`（关键字），不要先全量 `list_messages` 再翻页。

## 前置链
`list_messages` / `get_message`；按关键字 `search`；会话 `get_thread`；标签 `list_labels`；写 `send_message` / `mark_read`。
`get_message` 拿 mail_id 前先由 `list_messages` 或 `search` 获得。

## 避坑
- **mail 写面需用户身份**，宿主须放行 `user`。
- `send_message` 两步合一（建草稿→发送），模型无需感知草稿态。
- Agent 读完应 `mark_read` 回写，否则用户邮箱堆积未读。
- 回复 / 转发 vs 新建的差别 → `feishu.guidance_read(mail, reply-forward)`。

## 安全规则
- 写工具首次调用一律 `dry_run=true` 预演；确认 method/path 与字段摘要无误后，以 `dry_run=false` 重放**同一参数**。
- high-risk-write 工具须宿主注册 `IToolExecutionAuthorizer` 才会放行，未注册时一律拒绝。
- 只使用本域真实工具名，不臆造不存在的工具或 API。

## 示例
找张三发的合同邮件 → `mail.search` → 需要正文再 `mail.get_message`。

## 命令（工具）清单
- `mail.get_message`（只读）：获取邮件详情（主题/收发件人/正文预览）。message_id 来自 mail.list_messages。只读，需 mail:mailbox:readonly。
- `mail.get_thread`（只读）：取一封邮件的会话线程（同一主题下的往来邮件）——回复/追问类问题的完整上下文来源。只读，需 mail:mailbox:readonly。
- `mail.list_labels`（只读）：列出邮箱的邮件标签（label）——用于查清可用的 label_id，再传给 mail.list_messages 做标签过滤。只读，需 mail:mailbox:readonly。
- `mail.list_messages`（只读）：列出用户邮箱中的邮件（返回 message_id 列表，可传给 mail.get_message 获取详情）。user_mailbox_id 为用户邮箱地址。只读，需 mail:mailbox:readonly。
- `mail.mark_read`（写）：把邮件标记为已读（或未读）——Agent 读完邮件后应回写，避免用户邮箱堆积未读。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 mail:mailbox。
- `mail.search`（只读）：按关键字搜索邮箱邮件（如'找张三发的关于合同的邮件'）。user 身份接口，支持分页。只读，需 mail:mailbox:readonly。
- `mail.send_message`（写）：以用户身份发送邮件（两步操作：先创建草稿再发送，模型无需感知中间态）。to/cc/bcc 为邮箱地址数组，subject 为主题，body 为纯文本正文。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 mail:mailbox。用户身份工具——宿主须在 AllowedIdentities 放行 user。

## 参考文档（细则）
- `references/reply-forward.md`：`mail/reply-forward`
