## WHEN

- 已有规则的阈值、匹配文案或配色要改，但规则本身应当延续。
- 一批改多张表：`condition_formats` 里每项都带上 cf_id。

## Avoid when

- 规则不该存在了——用 `sheets.cond_format_delete`，别用一条残缺规则去"顶替"。
- 目标规则还不存在——那是 `sheets.cond_format_create`，cf_id 无从提供。

## Prerequisites

- 先 `sheets.cond_format_get` 取回各表 cf_id；缺 cf_id 的项在本地校验就被拒，不会下发。
- 这次想保留的属性必须照原样一起带上。

## Tips

- 全量覆盖：漏写的字段等于被清掉，与 `sheets.float_image_update` 的增量语义相反。
- 逐条 res_code 回执；改多条时按返回里的 cf_id 对齐，确认哪一条没吃到。
