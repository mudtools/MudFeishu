## WHEN

- 临时停掉一个误触发或暂不需要的自动化，或恢复一个已停用的工作流。

## Avoid when

- 不知道有哪些工作流、当前什么状态——用 `bitable.workflow_list`（只看与数据表相关的用 `bitable.workflow_block_list`）。
- 要改工作流的触发条件或节点内容——本工具只做启用/停用，不改流程结构。

## Prerequisites

- `workflow_id` 来自 `bitable.workflow_list` 的返回。
- `status` 只接受 Enable 与 Disable 两个字面值（含大小写差异在内的其它值在 `dry_run` 前即被拒绝）。

## Tips

- 停用是整表生效的开关：该流程对所有协作者的自动化都会暂停，共享表格上先告知再操作。
- 切换前记下当前 `status`，便于事后恢复原状。
- 结果返回后以 `bitable.workflow_list` 复查状态是否已生效。
