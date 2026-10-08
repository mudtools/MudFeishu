
# 消息（im.*）

## 路由优先级（先判断对象属于哪个域）
- **给人发消息 / 读聊天记录** → `im.*`；**给人发邮件** → `mail.*`；**给人建待办** → `task.*`。
- 会话内再分三类：普通消息（`send_message`）、**话题回复**（`reply_message`）、整条话题（`get_thread_messages` / `forward_thread`）。
- 收不到消息时先看 `receive_id_type` 是否与 ID 形态匹配（`oc_` → chat_id，`ou_` → open_id）。

## 前置链
读 `get_history_messages` / `get_message_content`；写 `send_message` / `reply_message`；
话题 `get_thread_messages` / `forward_thread`；群 → `get_chat` / `list_chat_members`。

## 避坑
- **发给张三不是一步**：先 `contact.search_user` 拿 open_id。
- `forward_message`（单条）与 `forward_thread`（整条话题）是两个工具，选错会丢上下文。
- `revoke_message` 只能撤自己的。
- 话题/回复 vs 消息的判别 → `feishu.guidance_read(im, topics-and-replies)`。

## 安全规则
- 写工具首次调用一律 `dry_run=true` 预演；确认 method/path 与字段摘要无误后，以 `dry_run=false` 重放**同一参数**。
- high-risk-write 工具须宿主注册 `IToolExecutionAuthorizer` 才会放行，未注册时一律拒绝。
- 只使用本域真实工具名，不臆造不存在的工具或 API。

## 示例
把整个话题转发给张三 → `search_user` → `forward_thread`。
