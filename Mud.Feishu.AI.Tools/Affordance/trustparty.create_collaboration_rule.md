## WHEN
- 与某关联组织首次互通，要从零落一条主体对客体的范围规则。
- 对接范围改版，对端要开放新一批成员或部门，重建立规比改造旧规则更稳妥。

## Avoid when
- 调整已有规则的两侧实体——那是 `trustparty.update_collaboration_rule`（覆盖式替换）。
- 让规则作废——用 `trustparty.delete_collaboration_rule`。
- 先看已有哪些规则——`trustparty.list_collaboration_rules` 是只读检查。

## Prerequisites
- `target_tenant_key` 取自 directory 面的 `trustparty.list_all_collaboration_tenants`，别与互通面清单混用。
- subject 与 object 两侧各至少填一类 ID 清单，两侧实体数之和必须小于 100。
- 部门清单填 0 表示对全部成员生效。

## Tips
- 空侧或实体数越界属于本地硬拦截，`dry_run` 也不会放行。
- 主体=我方、客体=对端，两侧填反了规则语义即反，预演时逐侧确认。
- 回执给出的规则 ID 是后续更新与删除的凭据，立即留存。
