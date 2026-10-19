## WHEN

- 高级权限需要一类新的授权对象（如「外部协作人只读某几张表」），从零建一个自定义角色。

## Avoid when

- 角色已存在，只是改名称或权限——用 `bitable.role_update`，不必新建。
- 只是把已有成员加进已有角色——用 `bitable.role_member_add`。
- 要看现有哪些角色——用 `bitable.role_list`。

## Prerequisites

- 先经 `bitable.role_list` 确认新 `role_name` 不与既有角色重名（重名会被拒绝）。
- `table_roles` 里的 table_id 来自 `bitable.list_tables`，`block_roles` 里的 block_id 来自 `bitable.dashboard_list`。

## Tips

- 两个权限数组都可省略：省略时按官方默认权限创建，之后再经 `bitable.role_update` 细化。
- 新角色不含任何成员，须用 `bitable.role_member_add` 加入后才对人生效。
- 结果返回新建角色的 role_id，保留它做后续授权。
