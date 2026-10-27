## WHEN

- 要基于现有仪表盘派生一份副本，用作新版本或留档备份。

## Avoid when

- 想知道有哪些仪表盘及其 ID——用 `bitable.dashboard_list`，本工具只复制不查询。
- 拿数据表或视图的 ID 当复制对象——本工具只认 `bitable.dashboard_list` 返回的 blk 前缀 ID。

## Prerequisites

- `block_id` 来自 `bitable.dashboard_list`；`name` 先定好副本名，避免与原盘无法区分。
- 后续要改副本需要新 `block_id`：原盘 ID 对副本无效。

## Tips

- 副本是独立的新仪表盘：之后改副本不影响原盘，改原盘也不会同步副本。
- 本工具没有幂等键，同一调用执行两次会得到两份重复副本。
- 先用 `dry_run` 预演确认目标 `block_id` 是你要的那个盘。
