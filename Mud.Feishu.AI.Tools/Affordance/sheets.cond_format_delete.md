## WHEN

- 一条或几条突出显示规则已经过时（阈值改了、字段废弃了），要整条撤下。

## Avoid when

- 只是要调阈值或换颜色——用 `sheets.cond_format_update` 改规则，规则 id 保留。
- 还在排查"为什么某格是红的"——先 `sheets.cond_format_get` 看规则全貌再决定，别先删。

## Prerequisites

- 先用 `sheets.cond_format_get` 拿到目标 `cf_id`（每个 `sheet_id` 下的规则 id）。
- 确认要删的规则与仍要保留的规则不混在同一次调用里（批量删除没有逐项回执）。

## Tips

- 删除后格子的高亮即消失；若要恢复只能按记忆重配，参数务必留底。
- 一条规则只影响其作用范围，删除不会改动单元格里的数据本身。
