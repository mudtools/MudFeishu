## WHEN

- 某个用户、群或部门应当获得某自定义角色定义的可见与可编辑范围。

## Avoid when

- 要把人移出角色——用 `bitable.role_member_remove`。
- 要改这个角色能看什么、改什么——用 `bitable.role_update`，本工具只建立人与角色的关联。
- 要看角色下现有谁——用 `bitable.role_member_list`。

## Prerequisites

- `role_id` 来自 `bitable.role_list`。
- `member_id` 的真实类型必须与 `member_id_type` 一致（默认 open_id；群用 chat_id、部门用 open_department_id），类型不匹配会加错对象。

## Tips

- 加群或加部门等于把角色权限授予其中全部成员，加入前先确认该群/部门的实际人数范围。
- 成员权限可能来自多个角色叠加，加完用 `bitable.role_member_list` 复核归属。
