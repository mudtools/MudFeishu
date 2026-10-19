## WHEN

- 表格是流水式记录，要在已有内容下面续几行，且不能动旧行。
- 只知道列范围、不知道当前最后一行在哪：`range` 给整列范围（如 ShtXxx!A1:Z1），落点由服务端推算。

## Avoid when

- 要改写已存在的区域——那是 `sheets.update_range`，覆盖写还能安全重放。
- 落笔前要先看现状——用 `sheets.get_range_values` 读，别靠追加试探。

## Prerequisites

- `spreadsheet_token` 来自 `sheets.list_sheets`；`range` 的工作表段必须是该表真实 ID。
- `values` 是二维数组 JSON 字符串，列数与 `range` 对齐，对不上会被拒。

## Tips

- 追加不幂等：同一批参数重放就多一批行。下游超时后先读回确认，别直接重试。
- 回执只给 revision，不含实际写入范围；数字与布尔会转成文本形态下发。
