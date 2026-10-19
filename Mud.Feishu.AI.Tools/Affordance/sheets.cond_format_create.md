## WHEN

- 需要让某些格按内容或数值自动着色，而这类规则还不存在。
- 一次给多张表立规则：`condition_formats` 是数组，每项带工作表 ID 与规则体。

## Avoid when

- 规则已在，只是换阈值或配色——用 `sheets.cond_format_update`（要带 cf_id）。
- 想先弄清表上已有哪些规则——`sheets.cond_format_get`，别靠反复创建试探。

## Prerequisites

- `spreadsheet_token` 来自 `sheets.list_sheets`；每条规则的作用范围用 A1 记法写在自己的 ranges 里，须与所属工作表一致。
- cf_id 由服务端生成，请求里不要自填。

## Tips

- 批量接口逐条回执：res_code 非 0 的那条没生效，整体成功不等于每条都成。
- 同一区域叠多条规则会让最终着色难以预判，调样式走 update，不要再建一条。
