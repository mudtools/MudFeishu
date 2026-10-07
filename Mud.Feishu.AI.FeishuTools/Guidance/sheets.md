# 电子表格（sheets.*）

## 前置链
`list_sheets` 取 sheet_id → `get_range_values` 按 A1 范围取；写 `update_range` / `append_rows`。

## 避坑
- **范围越小越省 token**，别习惯性取整表。
- `update_range` 是覆盖写（幂等）；`append_rows` 不清空既有数据。

## 示例
B2:D10 是什么数 → `get_range_values`。