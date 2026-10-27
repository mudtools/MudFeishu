## WHEN

- 已有浮动图片要挪位置、改尺寸或偏移。
- 只给要改的那几项，其余字段原样保留。

## Avoid when

- 想换成另一张图——本工具改不了素材，只能 `sheets.float_image_delete` 后重新 `sheets.float_image_create`。
- 图片该消失了——`sheets.float_image_delete`，更新不移除对象。

## Prerequisites

- `float_image_id` 来自 `sheets.float_image_query`（或 `sheets.float_image_get`），`sheet_id` 必须是它所在那张表。
- `range`、`width`、`height`、`offset_x`、`offset_y` 至少给一个，全空在本地即被拒。

## Tips

- 增量语义：未提供的字段保持原值，这与 `sheets.cond_format_update` 的全量覆盖相反。
- 回执回显更新后的完整图片，可直接核对锚点与偏移，不必再查一次。
