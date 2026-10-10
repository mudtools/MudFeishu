# 电子表格范围与写入语义

## 前置链
1. `sheets.list_sheets` 拿 `sheet_id` 与表格元信息（行列数、标题）
2. 读：`sheets.get_range_values` 按 A1 记法传范围
3. 写：`sheets.update_range`（覆盖写） / `sheets.append_rows`（追加行）

## 避坑
- **范围越小越省上下文**：不要习惯性取整表；先确认行列边界再取 `B2:D10` 这类小范围。
- **A1 记法**：`<sheet_id>!A1:C10`；省略起始行/列会改变语义，别只写 `A1`。
- `update_range` 是**覆盖写**（幂等：同范围同值重放无副作用）；`append_rows` **不清空**既有数据，只往后追加。
- 写请求的 `values` 是二维数组，行列数必须与 range 匹配——不匹配会被拒（不要靠平台截断）。
- 读回的数值有类型（数字/文本/公式结果）；**公式单元格读回的是结果值**，不是公式本身。
- 空单元格可能不出现在返回里：不要把「缺失」当成 0。

## 示例
- 「B2:D10 是什么数」→ `list_sheets` → `get_range_values(range="<sheet_id>!B2:D10")`
- 「在末尾再加一行」→ `append_rows(values=[[…]])`