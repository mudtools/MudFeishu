---
name: feishu-bitable
description: 飞书 bitable 域工具集：9 个工具（只读 6 / 写 3）。多维表格（记录/字段/视图）→ `bitable.*`；电子表格（A1 单元格区域）→ `sheets.*`。
version: 1.0.0
---

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

## 命令（工具）清单
- `bitable.add_record`（写）：向多维表格数据表新增一条记录，fields 为「字段名 → 值」JSON 对象（字段名先经 bitable.list_fields 确认；字段名拼错会在下发前被拒绝）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 bitable:app。
- `bitable.delete_record`（写）：删除多维表格中的指定记录（不可恢复！请谨慎使用，建议先 dry_run 预演确认）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 bitable:app。
- `bitable.get_records_by_ids`（只读）：按 record_id 批量获取多维表格记录（最多 100 条）——bitable.query_records 翻页后的精取链。只读，需 bitable:app:readonly。
- `bitable.get_view`（只读）：按 view_id 获取单个视图的详情（名称/类型/可见范围）。确认某个视图的具体配置时使用。只读，需 bitable:app:readonly。
- `bitable.list_fields`（只读）：列出数据表的字段定义（field_id/name/type），查询前先了解字段结构，配合 bitable.query_records 的 field_names/filter 使用。只读，需 bitable:app:readonly。
- `bitable.list_tables`（只读）：列出多维表格中的全部数据表，返回 table_id/name/revision；先于 bitable.list_fields、bitable.query_records 使用。只读，需 bitable:app:readonly。
- `bitable.list_views`（只读）：列出数据表下的全部视图（名称/类型/可见范围）。取记录前先用它确定 view_id，再传给 bitable.query_records 按视图取数。只读，需 bitable:app:readonly。
- `bitable.query_records`（只读）：按条件查询多维表格记录（先经 bitable.list_tables 获取 table_id，经 bitable.list_fields 了解字段）；filter 为简化筛选式，如 status = "done" and owner contains 张三。只读，需 bitable:app:readonly。
- `bitable.update_record`（写）：更新多维表格中的指定记录（按 record_id 更新 fields）。fields 为「字段名 → 值」JSON 对象字符串。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 bitable:app。

## 参考文档（细则）
- `references/filter-sort-syntax.md`：`bitable/filter-sort-syntax`
