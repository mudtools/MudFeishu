电子表格（sheets.*）：`sheets.list_sheets` 取 sheet_id，`sheets.get_range_values` 按 A1 范围取单元格——范围越小越省 token。写入走 `sheets.update_range`（覆盖写，天然幂等）与 `sheets.append_rows`（追加行，不清空既有数据），均为写操作（默认不启用，需宿主授权）。
