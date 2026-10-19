## WHEN
- 在既有互通规则上增删部分成员或部门，微调范围而不改规则主体结构。
- 对端实体整体换一批，但想保留同一条规则及其 ID，避免删除重建。

## Avoid when
- 从零建新规则——那是 `trustparty.create_collaboration_rule`。
- 让规则整体作废——用 `trustparty.delete_collaboration_rule`，本工具做不到清空。
- 只想看规则现状——`trustparty.list_collaboration_rules` 是只读检查。

## Prerequisites
- `collaboration_rule_id` 与 `target_tenant_key` 都必填，且与读出该规则的那次清单同源。
- 先取回规则的完整主体/客体，合并改动后整份提交，不能只报新增项。

## Tips
- 覆盖式替换是最常见事故：只提交一侧，另一侧会被静默清空。
- 两侧都不填会被本地拦下，规则不会被更新成空壳。
- `dry_run` 回报规则 ID 与两侧条数；某侧条数为 0 即执行后该侧清零，放行前务必复核。
