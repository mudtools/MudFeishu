## WHEN

- 要把一张图（截图、示意图、印章）浮到工作表上，不占用网格内容。
- 需要精确摆放：`width`/`height`/`offset_x`/`offset_y` 与锚点一起给。

## Avoid when

- 图已在，只是挪位或缩放——用 `sheets.float_image_update`。
- 想让内容跟着单元格走——那是写值，用 `sheets.update_range`。

## Prerequisites

- `float_image_token` 只能来自 `drive.upload_file` 的上传结果，不是本地文件路径。
- `sheet_id` 来自 `sheets.list_sheets`；`range` 只写锚点格（形如 ShtXxx!A1）。

## Tips

- 可自带 `float_image_id`（10 位字母数字）以便日后寻址；省略则由服务端生成，之后只能靠 `sheets.float_image_query` 捞回。
- 尺寸与偏移是纯像素数值字符串，写成带单位的文本会解析失败。
