---
name: feishu-wiki
description: 飞书 wiki 域工具集：5 个工具（只读 2 / 写 3）。知识库节点树的定位与移动 → `wiki.*`；取正文 → `docx.*`（文档）或 `sheets.*`（表格）；
version: 1.0.0
---

# 知识库（wiki.*）

## 路由优先级（先判断对象属于哪个域）
- **知识库节点树的定位与移动** → `wiki.*`；**取正文** → `docx.*`（文档）或 `sheets.*`（表格）；
  **看文件列表/元数据** → `drive.*`。
- 知识库节点有一个 `node_token` 与一个底层对象 `obj_token`：跨域取内容时要用后者。

## 前置链
`list_nodes` / `get_node` 取 node_token 与 obj_token → 喂给 `docx.get_raw_content` / `sheets.get_range_values`；
写 `create_node` / `move_node` / `move_docs_to_space`。

## 避坑
- **`move_docs_to_space` 是异步任务**，看返回的 `applied` 字段，不要假定立即生效。
- **`move_node` 目标全空会「成功但不移动」**，工具会提前拒绝——按提示补目标参数。
- 迁入已有云文档只能用 `move_docs_to_space`。
- 空间 / 节点树结构与 move 语义 → `feishu.guidance_read(wiki, space-node-tree)`。

## 安全规则
- 写工具首次调用一律 `dry_run=true` 预演；确认 method/path 与字段摘要无误后，以 `dry_run=false` 重放**同一参数**。
- high-risk-write 工具须宿主注册 `IToolExecutionAuthorizer` 才会放行，未注册时一律拒绝。
- 只使用本域真实工具名，不臆造不存在的工具或 API。

## 示例
把这份文档挪进知识库 → `move_docs_to_space`。

## 命令（工具）清单
- `wiki.create_node`（写）：在知识空间下创建节点（新建一篇 wiki 文档/普通页面）。space_id 可由 wiki.list_nodes 的结果推断。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 wiki:wiki。
- `wiki.get_node`（只读）：解析知识库节点信息（node_token/title/obj_type/obj_token）；obj_token 可传给 docx.get_raw_content 读取正文。只读，需 wiki:wiki:readonly。
- `wiki.list_nodes`（只读）：列出知识空间（或某父节点下）的子节点列表；node_token 可传给 wiki.get_node 解析详情。只读，需 wiki:wiki:readonly。
- `wiki.move_docs_to_space`（写）：把已有云文档（docx/sheet/bitable 等）迁移进知识空间，成为 wiki 节点——'把这份文档挪进知识库'的首选入口。⚠️ 平台以异步任务执行，返回 task_id。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 wiki:wiki。
- `wiki.move_node`（写）：移动知识空间节点（改父节点或换空间）——用于知识库整理、归档。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 wiki:wiki。

## 参考文档（细则）
- `references/space-node-tree.md`：`wiki/space-node-tree`
