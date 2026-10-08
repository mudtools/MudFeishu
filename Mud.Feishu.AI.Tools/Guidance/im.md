# 消息（im.*）

## 前置链
读 `get_history_messages` / `get_message_content`；写 `send_message` / `reply_message`；话题 `get_thread_messages` / `forward_thread`。

## 避坑
- **发给张三不是一步**：先 `contact.search_user` 拿 open_id。
- `forward_message`（单条）与 `forward_thread`（整条话题）是两个工具。
- `revoke_message` 只能撤自己的。
- 深层避坑 → `feishu.guidance_read(im, topics-and-replies)`。

## 示例
把整个话题转发给张三 → `search_user` → `forward_thread`。