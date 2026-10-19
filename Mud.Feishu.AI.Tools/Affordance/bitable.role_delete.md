## WHEN

- 团队重组或权限收敛时，某个自定义角色已无人需要，要把它整条撤掉。

## Avoid when

- 只是要把某些人移出该角色——用 `bitable.role_member_remove` 逐个移除，角色本身保留给其他成员。
- 只是要调整角色的权限点——用 `bitable.role_update` 改配置，不必删除重建。

## Prerequisites

- 先用 `bitable.role_list` 取 `role_id` 并核对角色名，确认它确实是要撤掉的那个。
- 用 `bitable.role_member_list` 确认角色下没有仍需该权限的成员（删除会连带截断他们的可见面）。

## Tips

- 删除角色会解除全部成员与它的关联，相关成员将立即失去该角色带来的可见范围。
- 角色删除后无法恢复；重建只能得到一个空角色，成员与权限都要重新配置。
