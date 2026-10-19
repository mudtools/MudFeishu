## WHEN

- 目标区域明确，要修正或替换那几格的值。
- 需要可重放：同 `range` 同 `values` 重复写入结果一致。

## Avoid when

- 只想在末尾续行、不碰旧数据——用 `sheets.append_rows`。
- 改的是着色、下拉或编辑权——本工具只写值，不碰格式与规则。
- 要写哪几格还没确定——先 `sheets.get_range_values` 读回边界。

## Prerequisites

- `spreadsheet_token` 与 `range` 里的工作表 ID 都来自 `sheets.list_sheets`。
- `values` 的行列数必须与 `range` 一致，不匹配即被拒，别指望平台截断。

## Tips

- 覆盖写会替换目标区域原有值，只想改几列就把 `range` 收窄到那几列。
- 首次先 `dry_run` 看下发的 method/path，再以同一组参数正式写入——幂等让这步零风险。
- 回执只有 revision，写对没写对区域要自己读回核对。
