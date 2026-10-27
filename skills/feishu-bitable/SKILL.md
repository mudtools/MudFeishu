---
name: feishu-bitable
description: 飞书 bitable 域工具集：25 个工具（只读 12 / 写 13）。多维表格（记录/字段/视图）→ `bitable.*`；电子表格（A1 单元格区域）→ `sheets.*`。
version: 1.0.0
package_version: 3.0.3
---

# 多维表格（bitable.*）

## 路由优先级（先判断对象属于哪个域）
- **多维表格**（记录/字段/视图）→ `bitable.*`；**电子表格**（A1 单元格区域）→ `sheets.*`。
  两者都叫「表格」，判据是「有没有字段类型与视图」。
- 知识库里的多维表格先经 `wiki.get_node` 拿 token，再以 app_token 进 `bitable.*`。

## 前置链
`list_tables` → `list_fields` → `query_records`；按视图取数先 `list_views`；手上已有一批 record_id 时直接给 `query_records` 的 `record_ids`（≤100，不与 filter/view_id/sort 同用）。

## 写入
- 新增（含单条）→ `record_batch_create`，`records` 数组放几项就写几条。
- 改（含单条）→ `record_batch_update`，每项带 `record_id`。
- 删 → `delete_record`（不可恢复）。

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
- `bitable.dashboard_copy`（写）：复制多维表格仪表盘（生成一份命名副本）。block_id 来自 bitable.dashboard_list。写操作：需 bitable:app。
- `bitable.dashboard_list`（只读）：分页列出多维表格中的全部仪表盘（block_id/name）；block_id 供 bitable.dashboard_copy 使用。只读，需 bitable:app:readonly。
- `bitable.delete_record`（写）：删除多维表格中的指定记录（不可恢复！请谨慎使用，建议先 dry_run 预演确认）。写操作：需 bitable:app。
- `bitable.form_get`（只读）：获取多维表格表单的元数据（名称/描述/是否共享/分享范围/填写次数限制/分享链接）。修改表单前先用本工具确认现状。只读，需 bitable:app:readonly。
- `bitable.form_question_list`（只读）：分页列出表单的题目列表（field_id/title/description/required/visible）；field_id 供 bitable.form_question_update 使用。只读，需 bitable:app:readonly。
- `bitable.form_question_update`（写）：更新表单中某道题目（字段）的展示属性（标题/描述/是否必填/是否可见/前序题目位置）；未提供的字段保持不变，但至少提供一个。field_id 来自 bitable.form_question_list。写操作：需 bitable:app。
- `bitable.form_update`（写）：更新多维表格表单的元数据（名称/描述/是否共享/分享范围/仅可填写一次）；未提供的字段保持不变，但至少提供一个。写操作：需 bitable:app。
- `bitable.form_upgrade`（写）：把旧版表单升级为「新版表单」并指定布局（traditional 传统布局 / one_question_per_page 一页一题布局）；升级后表单形态会改变、不可撤销，请先 dry_run 预演确认。写操作：需 bitable:app。
- `bitable.get_view`（只读）：按 view_id 获取单个视图的详情（名称/类型/可见范围）。确认某个视图的具体配置时使用。只读，需 bitable:app:readonly。
- `bitable.list_fields`（只读）：列出数据表的字段定义（field_id/name/type），查询前先了解字段结构，配合 bitable.query_records 的 field_names/filter 使用。只读，需 bitable:app:readonly。
- `bitable.list_tables`（只读）：列出多维表格中的全部数据表，返回 table_id/name/revision；先于 bitable.list_fields、bitable.query_records 使用。只读，需 bitable:app:readonly。
- `bitable.list_views`（只读）：列出数据表下的全部视图（名称/类型/可见范围）。取记录前先用它确定 view_id，再传给 bitable.query_records 按视图取数。只读，需 bitable:app:readonly。
- `bitable.query_records`（只读）：查询多维表格记录。两种用法：① 按条件检索（默认，先经 bitable.list_tables 取 table_id、bitable.list_fields 了解字段；filter 为简化筛选式，如 status = "done" and owner contains 张三，可翻页）；② 传 record_ids 按 ID 精取（最多 100 条，不可与 filter/view_id/sort 同用，不翻页）。只读，需 bitable:app:readonly。
- `bitable.record_batch_create`（写）：新增多维表格记录（单次最多 1000 条；只新增一条也走这里，传长度 1 的数组）。records 为 JSON 数组字符串，每项形如 {"fields":{"字段名":"值"}}；字段名先经 bitable.list_fields 确认（拼错会在下发前被拒绝）。写操作：需 bitable:app。
- `bitable.record_batch_update`（写）：更新多维表格记录（单次最多 1000 条；只更新一条也走这里，传长度 1 的数组）。records 为 JSON 数组字符串，每项形如 {"record_id":"recXxx","fields":{"字段名":"新值"}}。写操作：需 bitable:app。
- `bitable.role_create`（写）：创建多维表格高级权限的自定义角色（role_name 必填；table_roles / block_roles 为 JSON 数组字符串，省略时按官方默认权限创建）。写操作：需 bitable:app。
- `bitable.role_delete`（写）：删除多维表格高级权限的自定义角色（不可恢复！请谨慎使用，建议先 dry_run 预演确认）。写操作：需 bitable:app。
- `bitable.role_list`（只读）：分页列出多维表格高级权限的自定义角色（role_id/role_name）；role_id 供 bitable.role_update / role_delete / role_member_* 使用。只读，需 bitable:app:readonly。
- `bitable.role_member_add`（写）：把成员（用户 / 群 / 部门，由 member_id_type 指定 ID 类型）加入多维表格自定义角色；加入后该成员按角色权限访问多维表格。写操作：需 bitable:app。
- `bitable.role_member_list`（只读）：分页列出某个自定义角色下的成员（member_type/member_id/member_name）；member_id 供 bitable.role_member_remove 使用。只读，需 bitable:app:readonly。
- `bitable.role_member_remove`（写）：把成员从多维表格自定义角色中移除（仅收回该角色的高级权限，不影响成员本身；移除后成员将立即失去该角色的可见范围，建议先 dry_run 预演确认）；member_id 类型由 member_id_type 指定，默认 open_id。写操作：需 bitable:app。
- `bitable.role_update`（写）：更新多维表格高级权限的自定义角色（role_name 必填：官方接口要求即便不改名也需回传现有名称；table_roles / block_roles 可选，未提供时保持现状）。role_id 与现有名称可先用 bitable.role_list 取回。写操作：需 bitable:app。
- `bitable.workflow_block_list`（只读）：列出多维表格中与数据表相关的工作流（workflow_id/status/title）。与 bitable.workflow_list 的区别：本接口只返回与数据表（block）相关的工作流。只读，需 bitable:app:readonly。
- `bitable.workflow_list`（只读）：列出多维表格中的全部工作流（workflow_id/status/title），不区分是否挂在数据表上；只看与数据表相关的工作流用 bitable.workflow_block_list。workflow_id 供 bitable.workflow_update 使用。只读，需 bitable:app:readonly。
- `bitable.workflow_update`（写）：启用 / 停用多维表格工作流（status：Enable 启用 / Disable 停用）。workflow_id 来自 bitable.workflow_list。写操作：需 bitable:app。

## 参考文档（细则）
- `references/filter-sort-syntax.md`：`bitable/filter-sort-syntax`
- 正文里形如 `feishu.guidance_read(bitable, 主题)` 的指针，在文件形态下读本包的 `references/主题.md`（本包细则就是那份资产，无需进程内工具）
