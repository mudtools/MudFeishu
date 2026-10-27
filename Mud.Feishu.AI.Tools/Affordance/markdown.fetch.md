## WHEN

- 写入前要拿到远端当前的 Markdown 全文（patch 的定位依据、diff 的左侧输入）。
- 用户问「文档里现在写了什么」，对象是新版文档或云空间里的 `.md` 资源文件。

## Avoid when

- 要的是块结构与样式——用 `docx.get_raw_content`，Markdown 出口会丢掉版式细节。
- 对象是表格、幻灯片、PDF——分别走 `sheets.get_range_values`、`slides.*`、`drive.export_document`。
- 只确认元信息（标题/属主/更新时间）——用 `drive.get_file_metas`，不必读正文。

## Prerequisites

- `token` + `token_type`：新版文档用 `docx`（默认），云空间 `.md` 资源文件用 `file`。
- 需 drive:drive:readonly；无权限的文件报错，而不是返回空正文。

## Tips

- 正文按窗口返回：`has_more=true` 时用 `next_offset` 续读，别把一窗当全文。
- `total_length` 是字符数，可据此判断还要读几窗。
- `token_type=file` 只接受 UTF-8 文本；二进制或编码不符会被拒绝。
