
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
