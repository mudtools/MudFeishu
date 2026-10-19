
# 消息（im.*）

## 路由优先级（先判断对象属于哪个域）
- **给人发消息 / 读聊天记录** → `im.*`；**给人发邮件** → `mail.*`；**给人建待办** → `task.*`。
- 会话内再分三类：普通消息（`send_message`）、**话题回复**（`reply_message`）、整条话题（`get_thread_messages` / `forward_thread`）。
- 收不到消息时先看 `receive_id_type` 是否与 ID 形态匹配（`oc_` → chat_id，`ou_` → open_id）。
- 「发出去的内容有错要改」→ `edit_message`（文本）/ `patch_card_message`（卡片），不要撤回再重发（撤回会留下痕迹且丢引用）。
- 「必须让对方立刻看到」才用催办族 `urgent_app` / `urgent_sms`——催办是有打扰成本的对外动作。

## 前置链
读 `get_history_messages` / `get_message_content` / `search_messages`；写 `send_message` / `send_card` / `reply_message`；
改 `edit_message` / `patch_card_message`（`message_id` 来自发送结果或 `get_history_messages`）；催办 `urgent_app` / `urgent_sms`；
表情 `list_reactions` / `batch_query_reactions` 读明细 → `add_reaction` / `delete_reaction` 写；
话题 `get_thread_messages` / `forward_thread`；群 → `get_chat` / `search_chats` / `list_chat_members`。

## 避坑
- **发给张三不是一步**：先 `contact.search_user` 拿 open_id。
- `forward_message`（单条）与 `forward_thread`（整条话题）是两个工具，选错会丢上下文。
- `revoke_message` 只能撤自己的。
- `edit_message` / `patch_card_message` 改的是**已发出的那条**，需要原 `message_id`；卡片补丁只认卡片消息。
- 催办只在发送之后才有意义（`urgent_*` 的目标必须是已发出的消息）；`urgent_sms` 触达手机，打扰度最高，非必要不用。
- **表情回复不去重**：同一表情调两次会叠成两条；`delete_reaction` 删的是单条实例，`reaction_id` 只能从 `add_reaction` 回执或 `list_reactions` / `batch_query_reactions`（`with_details=true`）取。
- 话题/回复 vs 消息的判别 → `feishu.guidance_read(im, topics-and-replies)`。
- 交互式卡片（`send_card` 的结构化语法）→ `feishu.guidance_read(im, cards)`。

## 安全规则
- 写工具首次调用一律 `dry_run=true` 预演；确认 method/path 与字段摘要无误后，以 `dry_run=false` 重放**同一参数**。
- high-risk-write 工具须宿主注册 `IToolExecutionAuthorizer` 才会放行，未注册时一律拒绝。
- 只使用本域真实工具名，不臆造不存在的工具或 API。

## 示例
把整个话题转发给张三 → `search_user` → `forward_thread`。
