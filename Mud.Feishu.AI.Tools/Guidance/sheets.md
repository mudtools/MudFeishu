
# 电子表格（sheets.*）

## 路由优先级（先判断对象属于哪个域）
- **A1 单元格区域（行列网格）** → `sheets.*`；**多维表格（字段 / 记录 / 视图）** → `bitable.*`。
  判据是「有没有字段类型与视图」，不是文件后缀。
- 「算一下 / 汇总一下」若数据在电子表格里，先取小范围再本地计算，不要整表拉取。

## 前置链
`list_sheets` 取 sheet_id → `get_range_values` 按 A1 范围取；写 `update_range` / `append_rows`。

## 避坑
- **范围越小越省 token**，别习惯性取整表。
- `update_range` 是覆盖写（幂等）；`append_rows` 不清空既有数据。
- 公式单元格读回的是**结果值**；空单元格可能不出现在返回里（缺失 ≠ 0）。
- A1 记法与写入语义 → `feishu.guidance_read(sheets, range-and-values)`。

## 安全规则
- 写工具首次调用一律 `dry_run=true` 预演；确认 method/path 与字段摘要无误后，以 `dry_run=false` 重放**同一参数**。
- high-risk-write 工具须宿主注册 `IToolExecutionAuthorizer` 才会放行，未注册时一律拒绝。
- 只使用本域真实工具名，不臆造不存在的工具或 API。

## 示例
B2:D10 是什么数 → `list_sheets` → `get_range_values`。
