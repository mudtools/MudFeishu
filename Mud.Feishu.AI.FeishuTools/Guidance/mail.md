# 邮件（mail.*）

## 前置链
`list_messages` / `get_message`；按关键字 `search`；会话 `get_thread`；标签 `list_labels`。

## 避坑
- **mail 写面需用户身份**，宿主须放行 `user`。
- `send_message` 两步合一，模型无需感知草稿态。
- Agent 读完应 `mark_read` 回写，否则用户邮箱堆积未读。

## 示例
找张三发的合同邮件 → `mail.search`。