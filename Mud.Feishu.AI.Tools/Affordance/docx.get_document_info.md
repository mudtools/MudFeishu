## WHEN

- 动手改文档前确认目标：拿标题与版本号，核对是不是这篇。

## Avoid when

- 要正文内容——用 `docx.get_raw_content`（纯文本）或 `markdown.fetch`（Markdown）。
- 要块结构或块 ID——用 `docx.get_document_blocks`，本工具不含块信息。

## Prerequisites

- 只需 `document_id`（形如 doxcnXxx），来自搜索/知识库定位或用户给出。
- 以 tenant 身份调用，需 docx:document:readonly 权限。

## Tips

- 回执就三个键：`document_id`、`revision_id`、`title`。
- 编辑前记下 `revision_id` 作基线，可复查期间是否有人改动；封面与显示设置不投影。
