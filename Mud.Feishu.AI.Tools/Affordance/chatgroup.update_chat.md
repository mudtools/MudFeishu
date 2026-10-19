## WHEN
- 改群名、补群描述，或收紧谁能拉人、分享名片、@所有人。
- 把群在对话形态与话题形态之间切换，顺带只动个别设置项而不牵连其余配置。

## Avoid when
- 要改谁能发言——那是 `chatgroup.update_moderation`。
- 要增删成员——用 `chatgroup.add_member` 或 `chatgroup.remove_member`。
- 要增删管理员——分别用 `chatgroup.add_manager` / `chatgroup.delete_manager`。

## Prerequisites
- `chat_id` 先经 `chatgroup.list_chats` 或 `im.get_chat` 确认，找对群再动手。
- 可改字段（名称 / 描述 / 各项权限 / 消息模式）至少传一个；只传 `user_id_type` 不算改动。

## Tips
- 增量语义：没传的字段维持原值，不需要整份回填。
- 全空调用会被本地拦下并报空更新，不消耗一次真实请求。
- `dry_run` 预演回报将改的字段数；执行后用 `im.get_chat` 复核结果。
