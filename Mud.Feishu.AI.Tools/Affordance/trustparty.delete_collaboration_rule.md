## WHEN
- 一条互通规则确认不再需要（合作项目结束、对接范围收窄），且已与对端管理员沟通过影响面。

## Avoid when
- 只是想缩小范围——用 `trustparty.update_collaboration_rule` 覆盖主体/客体，rule_id 保持不变。
- 只是想暂时停用——平台没有"禁用规则"这一态：删除即作废，重建会拿到新的 rule_id。
- 还没确认目标规则——先 `trustparty.list_collaboration_rules` 看清 rule_id 与两侧实体。

## Prerequisites
- 先用 `trustparty.list_collaboration_rules` 拿到目标 `rule_id`，并确认 `target_tenant_key` 取自 directory 面。
- 与对端管理员确认：删除后对方**立即**失去互通能力，正在进行中的共享访问会中断。
- 宿主已授权该写工具（默认不启用），且 `MaxToolRisk` 未把它挡掉。

## Tips
- **删除不可撤销**：平台不提供"恢复已删规则"入口，只能按原主体/客体重建。
- 先 `dry_run` 预演，核对 rule_id 与租户标识后再以 `dry_run=false` 执行。
- 执行后建议立刻 `list_collaboration_rules` 复核：目标规则已消失、其余规则未受影响。
- 若误删，重建时务必用原来的主体/客体 open_id 清单——少一项，对端的可见范围就缺一块。
