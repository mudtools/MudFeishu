
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
