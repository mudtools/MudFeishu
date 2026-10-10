---
name: feishu-docx
description: 飞书 docx 域工具集：17 个工具（只读 8 / 写 9）。新版文档正文（块结构） → `docx.*`；电子表格 → `sheets.*`；知识库节点定位 → `wiki.*`；
version: 1.0.0
---

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

## 命令（工具）清单
- `docx.append_blocks`（写）：在文档根块下创建子块（向文档追加内容，如段落、标题等）。document_id 可由 docx.create_document 创建后获得或来自已有文档。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 docx:document。
- `docx.create_block`（写）：在指定父块下创建单个子块（精确块级编辑）。与 docx.append_blocks 的区别：本工具要求显式指定父块 ID，面向'在某个块下插入单个子块'。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 docx:document。
- `docx.create_descendant_blocks`（写）：在指定块下创建结构化子树（带父子关系的多层块）。descendants 为块数组（JSON），children_id 指定顶层子块顺序。⚠️ children_id 中的临时 ID 必须在 descendants 中有对应块，否则返回 invalid_args。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 docx:document。
- `docx.create_document`（写）：在云空间中创建一个飞书文档（返回 document_id，可用 docx.get_raw_content 读回）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 docx:document。
- `docx.delete_blocks`（写）：删除某个父块下指定索引区间的子块（如删掉文档里过时的一批段落）。索引是父块下的子块序号（从 0 开始），可由 docx.get_raw_content 或 docx.append_blocks 的返回推算。⚠️ 删除不可撤销——务必先用 dry_run 确认区间。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 docx:document。
- `docx.get_block`（只读）：获取文档中指定块的详情（块类型/文本/子块结构）。用于精确定位块后再编辑。只读，需 docx:document:readonly。
- `docx.get_chat_announcement`（只读）：读取群公告的块内容（分页）。群公告以文档形式存储，chat_id 来自 im.list_chats。只读，需 docx:document:readonly。
- `docx.get_document_blocks`（只读）：分块读取飞书文档结构（block_id/block_type/文本），表格/代码块等结构化场景使用；document_id 可来自 wiki.get_node 的 obj_token。只读，需 docx:document:readonly。
- `docx.get_document_info`（只读）：获取文档基本信息（标题/版本号/文档 ID）。改文档前先确认目标的链路首环。只读，需 docx:document:readonly。
- `docx.get_markdown_content`（只读）：读取飞书文档的 Markdown 正文（仅支持新版文档 docx）。跨域使用 Drive 接口获取内容，需 drive:drive:readonly。返回的 Markdown 可用于编辑后通过 docx.replace_document 写回。
- `docx.get_raw_content`（只读）：读取飞书文档的纯文本正文；document_id 可来自 wiki.get_node 的 obj_token 或 search.doc_wiki 结果的 token。只读，需 docx:document:readonly。
- `docx.import_markdown`（只读）：把 Markdown 内容转换成文档块结构（不写入文档）——用于先预览转换结果，再用 docx.append_blocks 写入。支持文本、一到九级标题、有序/无序列表、代码块、引用、待办、图片、表格。只读转换，需 docx:document。
- `docx.list_block_children`（只读）：列出指定块的所有子块（分页）。block_id 缺省为文档根块（等同列出文档顶层块）。用于定位要修改的块。只读，需 docx:document:readonly。
- `docx.replace_document`（写）：用 Markdown 内容整体替换文档正文（保留为新内容的旧块会被删除）。适用于'把这篇文章重建一遍'。执行顺序为先追加新块、再删除旧块，因此中途失败不会导致内容丢失。⚠️ 必须提供 idempotency_key（重试语义）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 docx:document。
- `docx.set_chat_announcement`（写）：批量更新群公告中块的文本内容。⚠️ 群公告是全员可见的广播面，更新后所有群成员将看到新内容。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 docx:document。
- `docx.update_block`（写）：更新文档中单个块的文本内容（PATCH 语义天然幂等）。仅支持文本块，不支持表格结构。改少量块用本工具，整篇重写用 docx.replace_document。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 docx:document。
- `docx.update_blocks`（写）：更新文档中已有块的文本内容（改写段落/标题的文字）。block_id 来自 docx.append_blocks 或 docx.get_raw_content 的返回。⚠️ 只支持改文本，不支持改表格结构与富样式（见工具说明）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 docx:document。

## 参考文档（细则）
- `references/block-editing.md`：`docx/block-editing`
