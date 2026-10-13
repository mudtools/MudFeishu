
# Docx：Markdown 导入 / 导出

## 前置链
1. `docx.import_markdown` **只做转换、不写入文档**：把 Markdown 转成文档块结构供预览
2. 结构无误后，用 `docx.append_blocks` 把块写入目标文档
3. 新建文档先经 `docx.create_document` 拿 `document_id`
4. 反向：`docx.get_markdown_content` 读正文 Markdown（仅新版 docx），编辑后用 `docx.replace_document` 写回

## 避坑
- **`import_markdown` 不落盘**：它不创建文档、不改任何文档；把它当"导入完成"是最常见的误解。
- `append_blocks` 是**追加**语义。要整体覆盖只能用 `docx.replace_document`——那是破坏性操作，
  先 `docx.get_raw_content` 留底再覆盖。
- 支持范围有限：文本、1~9 级标题、有序/无序列表、代码块、引用、待办、图片、表格。
  超范围语法会被降级为纯文本，**转换后必须核对块结构**。
- 图片按外链转存处理，转换前确认图片可被平台访问（内网地址会失败）。
- 写操作默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer）。

## 示例
- 「把这段 Markdown 变成文档」→ `docx.create_document` → `docx.import_markdown` 预览 → `docx.append_blocks` 写入。
- 「把文档正文导出来」→ `docx.get_markdown_content`（旧版文档会失败，如实说明不支持）。
