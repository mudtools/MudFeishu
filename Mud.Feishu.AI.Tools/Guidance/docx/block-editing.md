# Docx 块级写

## 前置链
1. `docx.get_raw_content` / `docx.get_document_blocks` 读现状
2. 写入用 `docx.append_blocks`（追加）/ `docx.update_blocks`（改文本）/ `docx.delete_blocks`（删）
3. Markdown → 块：`docx.import_markdown`（**只转换不写入**，先预览再落笔）

## 避坑
- **`docx.delete_blocks` 不可撤销**：务必先 `dry_run=true` 确认区间 `[start_index, end_index)`
- `docx.update_blocks` **只能改文本**，不支持改表格结构与富样式
- `docx.replace_document` 会**整体替换正文**，且 `idempotency_key` 必填
- `block_type` 是**闭集**：传错值时错误会直接列出全部合法取值

## 示例
- "把这份 Markdown 写进文档" → `docx.import_markdown` 预览 → `docx.append_blocks`
- "把前 5 段删掉" → `docx.get_document_blocks` 确认 → `docx.delete_blocks(dry_run=true)` → 去掉 dry_run