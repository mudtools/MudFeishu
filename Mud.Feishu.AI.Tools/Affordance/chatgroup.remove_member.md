## WHEN
- 误拉了不该进群的人，需要移出。
- 成员离职/转岗，不再需要留在该群。
- 清理长期不参与的外部联系人，收敛群范围。

## Avoid when
- 只是想撤销其管理员身份但保留成员资格——用 `chatgroup.delete_manager`。
- 想解散整个群——用 `chatgroup.delete_chat`（一次到位）。
- 对方是群主——群主不能被移出，需先转让群主。

## Prerequisites
- 先用 `chatgroup.list_chats` / `im.list_chat_members` 核对 chat_id 与待移出成员 ID。
- 与群主确认过：移出影响对方能否继续看到群内后续消息。
- 宿主已授权该写工具（默认不启用）。

## Tips
- **静默移出**：对方不会收到"你被移出了"的单独通知，只有群成员数变化——重要变动请主动告知。
- 务必先 `dry_run` 预演，核对 id_list 与 member_id_type 口径后再执行。
- ID 口径不一致时平台会把这些 ID 放进 `invalid_id_list` 返回——回执里非空就要逐个核对，不要当成已移出。
- 执行后用 `chatgroup.is_in_chat` 抽查确认已不在群内。
