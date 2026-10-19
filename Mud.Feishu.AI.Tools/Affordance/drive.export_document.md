## WHEN

- 要把云文档变成可交付成品（pdf / docx / xlsx / csv）交给本地或下游。
- 对象是 doc / docx / sheet / bitable，不是普通资源文件。

## Avoid when

- 源文件本身已是二进制——直接 `drive.download_file`，不必起导出任务。
- 只想在上下文里读结构——用 `docx.get_raw_content` 或 `sheets.get_range_values`。
- 只要标题与所有者等元信息——那是 `drive.get_file_metas`。

## Prerequisites

- `file_token` 来自 `drive.list_folder_files`。
- `type` 与 `file_extension` 须配对（doc/docx 转 docx 或 pdf，sheet/bitable 转 xlsx 或 csv），错位即本地报错、不下发。
- 需 drive:export:readonly，以应用（tenant）身份调用，宿主须注册文件客户端与落盘器。

## Tips

- 建任务、轮询、下载在一次调用里串完；等待窗耗尽即报错，不给半成品。
- 平台侧产物有效期很短，拿到就要尽快转存。
- `dry_run=true` 只预演建任务那一步，不消耗导出配额。
