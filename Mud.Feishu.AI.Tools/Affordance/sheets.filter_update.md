## WHEN

- 筛选器已存在，要改其中某一列的条件（换值或换比较方式）。
- 只撤掉某一列的条件：`filter_type` 传 clear，其余列的条件维持不动。

## Avoid when

- 表上根本没有筛选器——先 `sheets.filter_create`，本工具不建筛选器。
- 要整表恢复全量显示——`sheets.filter_delete`，比逐列 clear 干净。

## Prerequisites

- `sheet_id` 与 `col` 必须落在已有筛选器上；列字母以 `sheets.filter_get` 看到的范围为准。

## Tips

- 覆盖的是这一列的条件，同列连调两次只留最后一次，不会叠成两条。
- 回执只给 updated 标记，条件实际筛掉了多少行得回 `sheets.filter_get` 看被筛掉的行数。
