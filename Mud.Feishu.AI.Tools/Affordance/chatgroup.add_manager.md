## WHEN
- 群里的日常运营（改设置、拉人踢人、置顶）要交给几个真人分担，单靠机器人身份盯不过来。
- 新建群后需要立刻把可信成员提为管理员，让群务转由人侧处理。

## Avoid when
- 要撤销某人的管理员身份但保留其在群——那是 `chatgroup.delete_manager`。
- 要把人整个移出群——用 `chatgroup.remove_member`。
- 要看谁在群里——用 `im.list_chat_members`，不是本工具。

## Prerequisites
- `chat_id` 来自 `chatgroup.list_chats` 或 `im.search_chats`。
- `manager_ids` 的 ID 口径要与 `member_id_type` 一致（默认 open_id）。

## Tips
- 回执的 `chat_managers` 是晋升后的全量名单，不标注本次谁升上了——按名单比对确认生效。
- 晋升即刻生效：对方立即拿到改设置、移出成员、置顶等群管理能力。
- `dry_run` 只报 chat_id 与将要设为管理员的人数，不触碰下游。
