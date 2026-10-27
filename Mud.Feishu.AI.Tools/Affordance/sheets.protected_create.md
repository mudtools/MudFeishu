## WHEN

- 要把一段行或列锁住，只允许指定成员编辑。
- 锁定对象是整段行列区间，按维度给下标，而不是按 A1 区域。

## Avoid when

- 只是换可编辑成员或改锁状态——用 `sheets.protected_update`，区域与 ID 都不变。
- 想让某区域在外观上显眼但不限制编辑——那是 `sheets.cond_format_create`。

## Prerequisites

- `major_dimension` 只接受 ROWS / COLUMNS；`start_index` 从 0 起（含），`end_index` 不含且须更大——按 1 起始的习惯填就锁错行。
- `users` 里的成员 ID 类型必须与 `user_id_type` 一致（默认 open_id）。

## Tips

- 返回给出新建范围的 protect_ids，必须当场记下：后续更新与删除都靠它，一张表常并存多个范围。
- 一次只建一个范围；锁定即时对所有协作者生效。
