
# 多维表格（bitable.*）

## 路由优先级（先判断对象属于哪个域）
- **多维表格**（记录/字段/视图）→ `bitable.*`；**电子表格**（A1 单元格区域）→ `sheets.*`。
  两者都叫「表格」，判据是「有没有字段类型与视图」。
- 知识库里的多维表格先经 `wiki.get_node` 拿 token，再以 app_token 进 `bitable.*`。

## 前置链
`list_tables` → `list_fields` → `query_records`；按视图取数先 `list_views`。

## 避坑
- 不要猜字段名，先 `list_fields`；字段类型错配是最高频的失败原因。
- `delete_record` 不可恢复。
- filter/sort 用受控语法，不写自由表达式；完整语法 → `feishu.guidance_read(bitable, filter-sort-syntax)`。

## 安全规则
- 写工具首次调用一律 `dry_run=true` 预演；确认 method/path 与字段摘要无误后，以 `dry_run=false` 重放**同一参数**。
- high-risk-write 工具须宿主注册 `IToolExecutionAuthorizer` 才会放行，未注册时一律拒绝。
- 只使用本域真实工具名，不臆造不存在的工具或 API。

## 示例
项目表有哪些任务 → `list_tables` → `list_fields` → `query_records`。
