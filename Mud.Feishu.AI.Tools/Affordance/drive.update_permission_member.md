## WHEN

- 协作者已在权限列表内，需要调整其权限档位（`view` / `edit` / `full_access`）。

## Avoid when

- 对方还不在权限列表里——用 `drive.grant_permission`（先授予再调整）。
- 要改的是"公开链接"层面——用 `drive.update_permission_public`。

## Prerequisites

- 需要文档 `token` 与目标成员标识。
- 先确认目标成员**当前档位**，避免把 `full_access` 误降为 `view` 而中断协作。

## Tips

- 本操作幂等：重复设置同一档位无副作用。
- 由 `view` 提升到 `edit` / `full_access` 会**扩大可达范围**，风险与新增授权同级。
- 建议先 `dry_run` 预演。
