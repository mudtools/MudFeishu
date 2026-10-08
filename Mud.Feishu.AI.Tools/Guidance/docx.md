# 云文档（docx.*）

## 前置链
读 `get_raw_content`（要结构用 `get_document_blocks`）；写 `append_blocks` / `update_blocks` / `delete_blocks`。

## 避坑
- **`delete_blocks` 不可撤销**：先 `dry_run` 确认区间。
- `update_blocks` 只能改文本，不支持表格结构与富样式。
- 深层避坑 → `feishu.guidance_read(docx, block-editing)`。

## 示例
删前 5 段 → 确认 → `delete_blocks(dry_run)` → 去掉 dry_run。