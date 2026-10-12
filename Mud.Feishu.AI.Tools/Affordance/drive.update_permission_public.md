## WHEN

- 需要改变文档对"拿到链接的人"的开放程度（关闭外链、改为组织内可读、改为任何人可编辑）。

## Avoid when

- 只需给指定的人或群开权限——用 `drive.grant_permission`，比放开外链可控得多。
- 只是要看当前设置——用 `drive.get_permission_public`。

## Prerequisites

- **先 `drive.get_permission_public` 确认现状**，再决定改哪一项（`link_share_entity` / `external_access`）。
- 需要文档 `token`。

## Tips

- 可能使文档**对组织外可见**——这是最难回收的一类暴露面。
- 优先 `dry_run` 预演；改完后再用 `drive.get_permission_public` 复核一次。
