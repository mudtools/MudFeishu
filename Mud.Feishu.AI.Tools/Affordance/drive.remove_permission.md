## WHEN

- 某协作者不应再拥有该文档权限（人员转岗、外部合作结束），需要收回。

## Avoid when

- 只是要降级（编辑 → 只读）——用 `drive.update_permission_member`，保留协作者、只改档位副作用更小。
- 要换文档归属——用 `drive.transfer_owner`（那是转移而不是收回）。

## Prerequisites

- 需要文档 `token` 与目标成员标识（须与当初授予时一致）。
- 建议先用 `drive.get_permission_public` 确认当前权限面，避免误伤同名的另一名成员。

## Tips

- 收回**立即生效**，对方正在打开或正在编辑的文档会失去访问。
- 建议先 `dry_run` 预演，确认移除对象。
