
# 云文档（docx.*）

## 路由优先级（先判断对象属于哪个域）
- **新版文档正文（块结构）** → `docx.*`；**电子表格** → `sheets.*`；**知识库节点定位** → `wiki.*`；
  **只想知道文件在哪、多大** → `drive.*`。
- 从知识库/搜索结果拿到的 token，要先确认它是文档而非表格，再进 `docx.*`。

## 前置链
读 `get_raw_content`（要结构用 `get_document_blocks`）；写 `create_document` / `append_blocks` / `update_blocks` / `delete_blocks` / `replace_document` / `import_markdown`。

## 避坑
- **`delete_blocks` 不可撤销**：先 `dry_run` 确认区间 `[start_index, end_index)`。
- `update_blocks` 只能改文本，不支持表格结构与富样式。
- `import_markdown` **只转换不写入**：先预览块，再 `append_blocks` 落笔。
- 块索引语义与 `get_raw_content` 推断索引 → `feishu.guidance_read(docx, block-editing)`。

## 安全规则
- 写工具首次调用一律 `dry_run=true` 预演；确认 method/path 与字段摘要无误后，以 `dry_run=false` 重放**同一参数**。
- high-risk-write 工具须宿主注册 `IToolExecutionAuthorizer` 才会放行，未注册时一律拒绝。
- 只使用本域真实工具名，不臆造不存在的工具或 API。

## 示例
删前 5 段 → `get_document_blocks` 确认 → `delete_blocks(dry_run=true)` → 去掉 dry_run。
