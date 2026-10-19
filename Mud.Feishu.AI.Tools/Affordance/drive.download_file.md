## WHEN

- 确认对象是云空间的资源文件，要取到宿主本地交给后续进程打开或转存。
- 用户要的就是文件本体，而不是它的文字内容。

## Avoid when

- 想看正文——本工具不回字节，只给 `local_path`、`size`、`content_type`。
- 对象是云文档——那是 `drive.export_document`，要先起导出任务。
- 要文档的结构化内容——用 `docx.get_raw_content` 或 `sheets.get_range_values`。
- 类型还没确认——先用 `drive.get_file_metas` 分诊。

## Prerequisites

- `file_token`（形如 boxcnXxx）来自 `drive.list_folder_files` 或 `drive.get_file_metas`。
- 需 drive:drive:readonly，以应用（tenant）身份调用。
- 文件客户端与落盘器任一缺席时，本工具不进注册表。

## Tips

- 落盘目录与安全校验归宿主；被拒绝时报错而非返回空结果，文件不交付。
- 产物是交付物，执行链不清理它，生存期归宿主。
- 平台以 200 回 JSON 错误体时被防线拦下，报失败而不是给你半个文件。
