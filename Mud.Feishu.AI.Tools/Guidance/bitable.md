# 多维表格（bitable.*）

## 前置链
`list_tables` → `list_fields` → `query_records`；按视图取数先 `list_views`。

## 避坑
- 不要猜字段名，先 `list_fields`。
- `delete_record` 不可恢复。
- filter/sort 用受控语法，不写自由表达式。

## 示例
项目表有哪些任务 → tables → fields → `query_records`。