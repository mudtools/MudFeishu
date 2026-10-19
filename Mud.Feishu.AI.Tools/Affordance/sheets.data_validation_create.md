## WHEN

- 要限制某区域只能从给定选项里填（下拉列表），且该区域还没有校验。
- 一处范围配一条规则：`range` 直接给 A1 区域。

## Avoid when

- 已有下拉要换选项或挪区域——用 `sheets.data_validation_update`。
- 只是想去掉限制——用 `sheets.data_validation_delete`，别建一条空校验占位。

## Prerequisites

- `range` 的工作表段必须是 `sheets.list_sheets` 里的真实 ID。
- `condition_values` 须为非空字符串数组，空数组在本地就被拒。

## Tips

- 这类规则按区域寻址、没有独立 ID，回执只有 created；日后要靠同一区域走 update 或 delete。
- `multiple_values` 与 `highlight_valid_data` 都不传时不下发选项，想保持默认就别显式给值。
