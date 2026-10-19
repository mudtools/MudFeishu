## WHEN

- 调整已有自定义角色的名称、数据表权限或仪表盘等区块权限。

## Avoid when

- 加人、移人——用 `bitable.role_member_add` / `bitable.role_member_remove`，本工具不管成员。
- 角色还不存在——用 `bitable.role_create`。
- 要整个撤掉角色——用 `bitable.role_delete`，不是清空权限。

## Prerequisites

- 先用 `bitable.role_list` 取回 `role_id` 与现有 `role_name`：`role_name` 必填，即便不改名也要原样回传，传错就等于改名。

## Tips

- `table_roles` / `block_roles` 省略时保持旧值；传空数组会清空该侧权限，角色成员的可见面随即收缩。
- 只想调一张表的权限，也要把其余授权项原样带上——数组是整体替换，不是逐项打补丁。
- 真实执行前先 `dry_run`，核对将下发的权限数组摘要。
