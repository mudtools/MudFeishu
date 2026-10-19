## WHEN
- 误拉了不该进群的人，需要移出。
- 成员离职或转岗，不再需要留在该群。
- 清理长期不参与的外部联系人，收敛群范围。

## Avoid when
- 只是想撤销其管理员身份但保留成员资格——用 `chatgroup.delete_manager`。
- 想解散整个群——用 `chatgroup.delete_chat`，一次到位。
- 要确认某个 ID 在不在群里——用 `im.list_chat_members` 查名单；`chatgroup.is_in_chat` 问的是当前身份在不在群。

## Prerequisites
- 先用 `im.list_chat_members` 核对 chat_id 与待移出成员 ID。
- 与群主确认过：移出后对方立即失去群内可见性。
- 宿主已授权该写工具（默认不启用）。

## Tips
- 静默移出：对方不会收到单独通知，只有群成员数变化——重要变动请主动告知。
- 回执的 `invalid_id_list` 非空即有人没被移出，别当成全员清完。
- 务必先 `dry_run` 预演，核对 `id_list` 与 `member_id_type` 口径后再执行。
