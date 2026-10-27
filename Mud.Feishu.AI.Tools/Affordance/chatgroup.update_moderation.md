## WHEN
- 播报或公告时段需要让群里只剩管理者能发声。
- 把发言权开放给一批指定成员，其余人只读。
- 解除限制，恢复全员可发言。

## Avoid when
- 改群名等其他群设置——用 `chatgroup.update_chat`。
- 把人移出群——那是 `chatgroup.remove_member`，不是禁言。
- 只想看现在的可发言名单——`chatgroup.list_moderators` 是只读检查。

## Prerequisites
- `moderation_setting` 必填；选指定成员可发言的模式时，`moderator_added_list` 或 `moderator_removed_list` 至少给一个，否则本地直接拒绝。
- 名单 ID 的口径由 `member_id_type` 决定，默认 open_id。

## Tips
- 切到限制发言会即刻作用于全群，先 `dry_run` 看清模式与人数。
- 空名单等于全员不能说话，这个组合在预演之前就被拦下，不会静默下发。
- 执行后用 `chatgroup.list_moderators` 复核生效的模式与名单。
