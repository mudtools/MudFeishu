---
name: feishu-sheets
description: 飞书 sheets 域工具集：25 个工具（只读 8 / 写 17）。A1 单元格区域（行列网格） → `sheets.*`；多维表格（字段 / 记录 / 视图） → `bitable.*`。
version: 1.0.0
package_version: 3.0.3
---

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

## 命令（工具）清单
- `sheets.append_rows`（写）：向电子表格追加行数据（在指定区域末尾追加，不清空既有行）。spreadsheet_token 来自 sheets.list_sheets。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。
- `sheets.cond_format_create`（写）：为工作表批量创建条件格式规则（按内容/数值自动着色）。一条规则形如 {"sheet_id":"ShtXxx","condition_format":{"ranges":["ShtXxx!A1:B2"],"rule_type":"containsText","attrs":[{"operator":"contains","text":"完成"}],"style":{"back_color":"#FDE2E2"}}}… 〔需 sheets:spreadsheet〕
- `sheets.cond_format_delete`（写）：批量删除工作表的条件格式规则（不可恢复！请谨慎使用，建议先 dry_run 预演确认）；每项形如 {"sheet_id":"ShtXxx","cf_id":"..."}，cf_id 来自 sheets.cond_format_get。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。
- `sheets.cond_format_get`（只读）：查询电子表格指定工作表的条件格式规则（cf_id/生效范围/规则类型）。cf_id 供 sheets.cond_format_update / sheets.cond_format_delete 使用。只读，需 sheets:spreadsheet:readonly。
- `sheets.cond_format_update`（写）：批量更新（覆盖）工作站的条件格式规则；每项 condition_format 必须携带 cf_id（来自 sheets.cond_format_get），更新按 cf_id 全量覆盖该规则。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。
- `sheets.data_validation_create`（写）：为单元格区域创建「数据校验」（如下拉列表）。range 形如 ShtXxx!A1:B100；condition_values 为下拉选项值列表。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。
- `sheets.data_validation_delete`（写）：删除单元格区域上的数据校验规则（不可恢复！请谨慎使用，建议先 dry_run 预演确认）。ranges 形如 ["ShtXxx!A1:B100"]。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。
- `sheets.data_validation_get`（只读）：查询电子表格指定区域的「数据校验规则」（下拉选项/选项值等）。range 形如 ShtXxx!A1:B100。只读，需 sheets:spreadsheet:readonly。
- `sheets.data_validation_update`（写）：更新（覆盖）指定工作表的数据校验规则——按 ranges 指定新的生效区域与选项值。sheet_id 来自 sheets.list_sheets。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。
- `sheets.filter_create`（写）：为工作表创建筛选器（按列筛选并隐藏不匹配行；每个工作表仅一个筛选器）。filter_type 可选 multiValue 多值 / number 数字 / text 文本 / color 颜色；compare_type 如 less/equals/contains；expected 为筛选值列表。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。
- `sheets.filter_delete`（写）：删除工作表的筛选器（删除后被隐藏的行会恢复显示，筛选条件不可恢复！建议先 dry_run 预演确认）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。
- `sheets.filter_get`（只读）：查询指定工作表的筛选器（生效区域/筛选列/条件/被隐藏的行号）。sheet_id 来自 sheets.list_sheets。只读，需 sheets:spreadsheet:readonly。
- `sheets.filter_update`（写）：更新（覆盖）工作表筛选器的某列筛选条件。filter_type 传 clear 可清除该列筛选。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。
- `sheets.float_image_create`（写）：在工作表中创建浮动图片（图片素材的 token 经 drive.upload_file 上传后获得；range 指定锚定区域）。width/height/offset_x/offset_y 为像素数值字符串。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。
- `sheets.float_image_delete`（写）：删除工作表中的浮动图片（不可恢复！请谨慎使用，建议先 dry_run 预演确认）。float_image_id 来自 sheets.float_image_query。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。
- `sheets.float_image_get`（只读）：按 float_image_id 获取工作表中的单个浮动图片（覆盖范围/尺寸/偏移）。float_image_id 来自 sheets.float_image_query。只读，需 sheets:spreadsheet:readonly。
- `sheets.float_image_query`（只读）：查询工作表中的全部浮动图片（float_image_id/覆盖范围/尺寸/偏移）。只读，需 sheets:spreadsheet:readonly。
- `sheets.float_image_update`（写）：更新工作表中的浮动图片（覆盖范围/尺寸/偏移）；未提供的字段保持不变，但至少提供一个。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。
- `sheets.get_range_values`（只读）：读取工作表单元格区域数据；range 形如 ShtXxx!A1:C100（sheet_id 来自 sheets.list_sheets），建议先小范围取数。只读，需 sheets:spreadsheet:readonly。
- `sheets.list_sheets`（只读）：列出电子表格的全部工作表（sheet_id/title/index）；sheet_id 供 sheets.get_range_values 构造 range。只读，需 sheets:spreadsheet:readonly。
- `sheets.protected_create`（写）：为工作表/行列区间创建保护范围（锁定后仅可编辑成员可改）；major_dimension：ROWS 按行区间 / COLUMNS 按列区间（区间下标从 0 开始、半开区间）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。
- `sheets.protected_delete`（写）：删除保护范围（保护撤销后该区域恢复全员可编辑；不可恢复！建议先 dry_run 预演确认）。protect_ids 为逗号分隔的保护范围 ID 列表。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。
- `sheets.protected_get`（只读）：查询电子表格的保护范围（受保护的工作表/区域、锁定状态）。protect_ids 为逗号分隔的保护范围 ID 列表（protect_id 来自 sheets.protected_create 的结果）。只读，需 sheets:spreadsheet:readonly。
- `sheets.protected_update`（写）：更新保护范围的可编辑成员（新增/移除）或锁定信息；protect_id 来自 sheets.protected_get（或 protected_create 的结果）。add_editors / remove_editors 为成员 ID 列表（类型由 editor_member_type 指定）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。
- `sheets.update_range`（写）：向电子表格指定区域写入数据（覆盖写，天然幂等——同一区域重复写入结果一致）。spreadsheet_token 来自 sheets.list_sheets；range 形如 ShtXxx!A1:B2。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 sheets:spreadsheet。

## 参考文档（细则）
- `references/range-and-values.md`：`sheets/range-and-values`
- 正文里形如 `feishu.guidance_read(sheets, 主题)` 的指针，在文件形态下读本包的 `references/主题.md`（本包细则就是那份资产，无需进程内工具）
