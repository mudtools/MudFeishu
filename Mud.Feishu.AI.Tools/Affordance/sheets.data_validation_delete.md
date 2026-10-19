## WHEN

- 某片区域的录入约束（下拉、数值范围）随业务规则变化而取消，不再需要校验。

## Avoid when

- 只是要改约束内容——用 `sheets.data_validation_update` 换规则本身，不必先删。
- 区域里还希望保留提示只是放宽限制——同样走 update 而不是删除后重建。

## Prerequisites

- 先用 `sheets.data_validation_get` 确认目标区域上当前挂了哪些校验规则。
- 用 A1 记法写准 `ranges`（形如 `ShtXxx!A1:B100`）；范围写大了会误删邻居区域的规则。

## Tips

- 删除校验不会改动已有单元格的值——旧值哪怕已不符合新规则，也原样保留。
- 批量删除按区域生效，逐区域核对后再提交。
