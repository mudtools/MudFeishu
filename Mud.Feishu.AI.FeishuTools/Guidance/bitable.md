多维表格（bitable.*）：先 `bitable.list_tables` 取 app_token/table_id，再 `bitable.list_fields` 确认字段名，最后 `bitable.query_records` 取数（filter/sort 用受控语法，不要写自由表达式）。写入走 `bitable.add_record`（默认不启用，需宿主授权）。
