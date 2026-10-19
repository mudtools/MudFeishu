---
name: feishu-im
description: 飞书 im 域工具集：20 个工具（只读 8 / 写 12）。给人发消息 / 读聊天记录 → `im.*`；给人发邮件 → `mail.*`；给人建待办 → `task.*`。
version: 1.0.0
package_version: 3.0.3
---

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
话题 `get_thread_messages` / `forward_thread`；群 → `get_chat` / `search_chats` / `list_chat_members`。

## 避坑
- **发给张三不是一步**：先 `contact.search_user` 拿 open_id。
- `forward_message`（单条）与 `forward_thread`（整条话题）是两个工具，选错会丢上下文。
- `revoke_message` 只能撤自己的。
- `edit_message` / `patch_card_message` 改的是**已发出的那条**，需要原 `message_id`；卡片补丁只认卡片消息。
- 催办只在发送之后才有意义（`urgent_*` 的目标必须是已发出的消息）；`urgent_sms` 触达手机，打扰度最高，非必要不用。
- 话题/回复 vs 消息的判别 → `feishu.guidance_read(im, topics-and-replies)`。
- 交互式卡片（`send_card` 的结构化语法）→ `feishu.guidance_read(im, cards)`。

## 安全规则
- 写工具首次调用一律 `dry_run=true` 预演；确认 method/path 与字段摘要无误后，以 `dry_run=false` 重放**同一参数**。
- high-risk-write 工具须宿主注册 `IToolExecutionAuthorizer` 才会放行，未注册时一律拒绝。
- 只使用本域真实工具名，不臆造不存在的工具或 API。

## 示例
把整个话题转发给张三 → `search_user` → `forward_thread`。

## 命令（工具）清单
- `im.edit_message`（写）：编辑本 Bot 已发送的文本消息内容（发错字时纠正，对方看到的消息被更新）。message_id 来自 im.send_message 的返回。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:message。
- `im.forward_message`（写）：把一条消息转发给用户或群。receive_id 来自 im.search_user / 事件上下文。⚠️ 底层 SDK 方法名为 ReceiveMessageAsync 但语义是转发（POST /messages/{id}/forward），不是接收消息。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:message:send_as_bot。
- `im.forward_thread`（写）：把整个话题（thread）转发给用户或群——一次性把讨论上下文带过去。thread_id 来自事件上下文。⚠️ 底层 SDK 方法名为 ReceiveThreadsAsync 但语义是转发话题。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:message:send_as_bot。
- `im.get_chat`（只读）：读取群基础信息（群名/描述/成员数/群主）。chat_id 可由事件上下文获得。⚠️ 底层 SDK 方法名拼写为 GetChatGroupInoByIdAsync（Ino 应为 Info），此处按正确语义命名。只读，需 im:chat:readonly。
- `im.get_history_messages`（只读）：读取群聊的历史消息（chat_id 可由事件上下文获得），按创建时间倒序返回最近消息预览。只读，需 im:message:readonly。
- `im.get_message_content`（只读）：按 message_id 回查单条消息的完整内容——与 im.get_history_messages 组成两步链（历史消息列表 → 指定消息内容）。只读，需 im:message:readonly。
- `im.get_message_read_users`（只读）：查询某条消息已被哪些人读到（user_id + 读取时间）。⚠️ 底层 SDK 方法名拼写为 GetMessageReadUsesAsync（Uses 应为 Users），此处按正确语义命名。只读，需 im:message:readonly。
- `im.get_thread_messages`（只读）：读取某个话题（thread）内的消息——群话题场景下用 thread_id 取代 chat_id 读话题内容。thread_id 可由事件上下文获得。只读，需 im:message:readonly。
- `im.list_chat_members`（只读）：分页列出群聊成员（member_id/name/tenant_key）——'这个群里有哪些人'的多步流程地基。chat_id 可由事件上下文获得。只读，需 im:chat:readonly。
- `im.patch_card_message`（写）：更新本 Bot 已发送的卡片消息内容（整卡覆盖）。⚠️ 用结构化语法描述内容，**不要写 JSON**：body 每行一个元素（text:内容 / quote:内容 / code:内容 / divider），buttons 每行一个按钮（按钮文本|url|链接 或 按钮文本|value|回调值）。message_id 来自 im.send_card 的返回。写操作：默认空名单不启用，需 im:message。
- `im.reply_message`（写）：回复指定消息，形成话题串避免刷屏。message_id 来自 im.get_history_messages 或事件上下文；content 为 JSON 字符串（msg_type=text 时如 {"text":"回复内容"}）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:message。
- `im.revoke_message`（写）：撤回一条自己发出的消息（发错内容时纠正）。只能撤回本 Bot 发送的消息；message_id 来自事件上下文或 im.get_history_messages。high-risk-write：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:message。建议先 dry_run 预演确认目标消息。
- `im.search_chats`（只读）：按关键词搜索群聊（返回 chat_id/name/描述）——用户只记得群名片段时的入口。只读，需 im:chat:readonly。
- `im.search_messages`（只读）：按关键词搜索可见会话中的消息——支持按会话/发送者/时间过滤。返回消息 ID 与命中片段预览，可用 im.get_message_content 回查完整内容。只读，需 im:message:readonly。
- `im.send_card`（写）：发送一张交互式卡片（标题＋正文＋按钮），用于让对方一眼看到要点并能直接点按钮——比纯文本更适合通知、待办、审批提醒。⚠️ 用结构化语法描述内容，**不要写 JSON**：body 每行一个元素（text:内容 / quote:内容 / code:内容 / divider），buttons 每行一个按钮（按钮文本|url|链接 或 按钮文本|value|回调值）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:mess…
- `im.send_file`（写）：把一个网络文件作为消息发送到指定会话（file_url 为 http/https 绝对地址，由宿主负责下载落盘）。file_name 必须带扩展名。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:resource 与 im:message:send_as_bot。
- `im.send_image`（写）：把一张网络图片作为消息发送到指定会话（image_url 为 http/https 绝对地址，由宿主负责下载落盘）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:resource 与 im:message:send_as_bot。
- `im.send_message`（写）：发送文本消息到指定群聊或用户。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:message。首次面向陌生接收者时建议先以 dry_run=true 预演。
- `im.urgent_app`（写）：把一条已发送消息加急给指定用户（飞书客户端来弹窗强提醒）——适用于对方可能漏看的紧急场景。⚠️ 会打扰对方，仅在确有需要时使用。message_id 来自 im.send_message；user_ids 为对方 open_id 数组。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:message。
- `im.urgent_sms`（写）：把一条已发送消息以短信加急给指定用户（飞书客户端通知＋手机短信双通道）——比 im.urgent_app 打扰更强，仅在对方可能长时间离线的紧急场景使用。message_id 来自 im.send_message；user_ids 为对方 open_id 数组。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:message。

## 参考文档（细则）
- `references/cards.md`：`im/cards`
- `references/topics-and-replies.md`：`im/topics-and-replies`
