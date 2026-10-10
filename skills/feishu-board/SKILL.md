---
name: feishu-board
description: 飞书 board 域工具集：6 个工具（只读 2 / 写 4）。要"把推理结果画出来"（架构图 / 流程图 / 时序图 / 思维导图）→ 一律走 `board.render_dsl`：
version: 1.0.0
---

# 画板（board.*）

## 路由优先级（先判断对象属于哪个域）
- **要"把推理结果画出来"**（架构图 / 流程图 / 时序图 / 思维导图）→ 一律走 `board.render_dsl`：
  模型直接产出 Mermaid / PlantUML 源码即可，**不需要**懂画板节点结构。
- **要精确摆放节点**（已有坐标 / 父子关系）→ `board.create_nodes`；**要读现有节点** → `board.list_nodes`。
- 只要主题色等元信息 → `board.get_theme`。

## 前置链（whiteboard_id 怎么来）
画板 ID **不在本域产生**，它来自文档块：
1. `docx.get_document_blocks` 列出文档块；
2. 找 `block_type=43`（画板块）的 block，其 `token` 即 `whiteboard_id`；
3. 拿到后再调用 `board.*`。
用户若只给了「文档链接」，先 `docx.get_raw_content` / `docx.get_document_blocks` 取块；用户直接给了
`whiteboard_id` 或画板链接时可直接使用。**拿不到 ID 就如实说明并给出上面两步**，不要臆造 ID。

## 避坑
- **DSL 类型只能二选一**：`board.render_dsl` 的 `dsl_type` 仅接受 `plantuml` 或 `mermaid`；
  其它语法（如 graphviz / d2）先自行转换成 Mermaid 再提交。
- **节点 ID 是返回值不是猜测值**：`board.create_nodes` 回填 `created_nodes[].node_id`，
  `board.delete_nodes` 需要真实 `node_ids`；删错不可撤销。
- **`board.delete_nodes` 无幂等键**：重复删除同一节点会报错而非静默成功——一次调用只删一次。
- 画板缩略图下载**未策展**（二进制会击穿上下文），需要图片请让用户在飞书端导出。

## 安全规则
- 写操作（`render_dsl` / `create_nodes` / `update_theme` / `delete_nodes`）默认空名单不启用，
  须宿主授权（`WriteAllowList` + `IToolExecutionAuthorizer`）；支持 `dry_run=true` 预演。
- 只使用本域真实工具名，不臆造不存在的工具或 API。

## 示例
「把这份部署流程画成时序图并放进文档的画板里」→ `docx.get_document_blocks` 找 `block_type=43` →
`board.render_dsl`（`dsl_type=mermaid`）→ 回填节点结果。

## 命令（工具）清单
- `board.create_nodes`（写）：在画板中创建节点（支持批量创建、父子关系）。支持幂等：传入 idempotency_key 可安全重试。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），可用 dry_run=true 预演；需 board:whiteboard 权限。
- `board.delete_nodes`（写）：批量删除画板节点。⚠️ 子节点会被递归删除，操作不可逆。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），可用 dry_run=true 预演确认；需 board:whiteboard 权限。
- `board.get_theme`（只读）：获取画板主题信息（默认配色方案）。whiteboard_id 来自文档块：docx.get_document_blocks 中 block_type=43 的 block.token 即画板 ID。只读，需 board:whiteboard:readonly。
- `board.list_nodes`（只读）：获取画板内所有节点（返回节点数组，可通过 parent_id/children 组装画板结构）。只读，需 board:whiteboard:readonly。
- `board.render_dsl`（写）：将 PlantUML 或 Mermaid 源码解析为画板节点（协同编辑）。模型只需输出 DSL 文本，无需了解画板节点结构。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），可用 dry_run=true 预演；需 board:whiteboard 权限。
- `board.update_theme`（写）：更新画板主题（配色方案）。幂等操作。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），可用 dry_run=true 预演；需 board:whiteboard 权限。

## 参考文档（细则）
- `references/dsl.md`：`board/dsl`
