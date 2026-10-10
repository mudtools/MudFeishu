# IM 话题与回复

## 前置链
1. `im.get_history_messages` / `im.search_messages` 拿 message_id
2. `im.reply_message` 传 message_id；**当前会话在话题中时 `reply_in_thread` 会自动为 true**

## 避坑
- **"发给张三"不是一步**：先 `contact.search_user` 拿 open_id，再 `im.send_message(receive_id_type="open_id")`
- `im.forward_message` 与 `im.forward_thread` 是**两个工具**：转发单条 vs 整条话题，选错会丢上下文
- 撤回只能撤**自己**发的；`im.revoke_message` 对他人消息无效

## 示例
- "把这句回复到那条消息上" → `im.get_history_messages` → `im.reply_message`
- "把整个话题转发给张三" → `contact.search_user` → `im.forward_thread`