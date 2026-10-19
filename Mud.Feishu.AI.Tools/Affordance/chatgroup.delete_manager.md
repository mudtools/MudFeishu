## WHEN
- 某位管理员转岗或离职，需要收回群管理能力。
- 误设了管理员需要纠正，或把多人管理收敛回群主单人管理。

## Avoid when
- 要把人整个移出群——用 `chatgroup.remove_member`。
- 要改谁能发言——那是 `chatgroup.update_moderation`，与管理身份无关。
- 要看现任管理员都有谁——用 `im.get_chat`；`chatgroup.list_moderators` 给的是可发言名单，不是管理员。

## Prerequisites
- 先用 `im.get_chat` 核对 chat_id 与现任管理员名单。
- `manager_ids` 的 ID 口径要与 `member_id_type` 一致（默认 open_id）。
- 宿主已授权该写工具（默认不启用）。

## Tips
- 撤销不通知：对方不会收到提示，只会发现能力入口消失——重要变动请主动告知。
- 回执的 `chat_managers` 是撤销后的全量名单，复核以它为准。
- 务必先 `dry_run` 预演，核对名单后再执行。
