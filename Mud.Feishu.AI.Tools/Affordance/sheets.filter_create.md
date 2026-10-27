## WHEN

- 这张表还没有筛选器，需要按某列把不匹配的行隐藏起来。
- 建的时候直接带上条件：`filter_type` 决定形态，`compare_type` 与 `expected` 只在需要的类型下有意义。

## Avoid when

- 筛选器已在，只是换条件——用 `sheets.filter_update`，不必先删再建。
- 要恢复全量显示——`sheets.filter_delete`，不是把每列条件清空。

## Prerequisites

- 每个工作表只有一个筛选器：先 `sheets.filter_get` 确认这张表上还没有。
- `range` 是筛选作用范围（含表头行），`col` 用该范围内的列字母。

## Tips

- 筛选是共享视图状态：隐藏对所有协作者同步生效，单元格数据本身不变。
- 建完筛选器再按范围读数据，读回的行集合会和筛前不同，先想清楚要哪批行。
