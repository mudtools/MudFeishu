## WHEN
- 某位管理员转岗/离职，需要收回群管理能力。
- 误设了管理员，需要纠正。
- 群管理角色收敛（从多人管理收回为群主单人管理）。

## Avoid when
- 只是想把人移出群——用 `chatgroup.remove_member`，那会同时取消其管理员身份。
- 想给管理员换人——先删再加，或直接用本工具删掉旧的那位。
- 对方是群主——群主身份不能通过本工具撤销（需先转让群主）。

## Prerequisites
- 先用 `chatgroup.list_moderators` 核对现任管理员名单与 chat_id。
- 与管理员的直属主管或群主确认过：撤销后对方立即失去改设置/踢人/置顶等能力。
- 宿主已授权该写工具（默认不启用）。

## Tips
- **撤销不通知**：对方不会收到"你被撤销管理员"的提示，只会发现功能入口消失——重要变动请主动告知。
- 务必先 `dry_run` 预演，核对 manager_ids 与 member_id_type 口径后再执行。
- `member_id_type` 必须与名单里的 ID 口径一致（open_id / user_id / union_id），不一致会报"无效 ID"。
- 执行后用 `chatgroup.list_moderators` 复核名单。
