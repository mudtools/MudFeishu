## WHEN

- 机器人贴错了表情，要把那一次回复收回来。
- 用户已确认完成，想把先前的「收到」表情撤掉，而不是再发一条消息去说明。

## Avoid when

- 想撤的是别人加的表情——本工具按 reaction_id 删除**单次回复实例**，删不到同一表情上其他成员的回复。
- 想撤回的是消息本身——那是 `im.revoke_message`，表情与消息是两个对象。
- 只是不确定有没有贴过——先读 `im.list_reactions`。平台不去重，重复 `im.add_reaction` 会叠成两条。

## Prerequisites

- reaction_id 只能来自 `im.add_reaction` 的回执，或 `im.list_reactions` / `im.batch_query_reactions`（with_details=true）的明细；它是实例 ID，不是表情名。
- message_id 与 reaction_id 必须配对：表情回复挂在具体消息下，拿别的消息 ID 一起传删不到东西。

## Tips

- 删除不可逆：撤回后只能重新 `im.add_reaction`，原 reaction_id 不会复活。
- 不确定删的是哪一条时先 dry_run 预演——它只回 method/path 与字段摘要，不消耗写授权。
