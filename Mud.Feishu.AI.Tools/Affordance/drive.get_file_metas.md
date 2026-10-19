## WHEN

- 手上一批 token 要一次问清类型、标题与所有者，再决定后续读法。
- 判断对象属于资源文件还是云文档时的分诊入口。

## Avoid when

- 要逐层看目录里有什么——那是 `drive.list_folder_files`。
- 要正文或成品——按类型改走 `docx.get_raw_content` 或 `drive.export_document`。
- 要看评论或分享设置——用 `drive.list_comments`、`drive.get_permission_public`。

## Prerequisites

- `tokens` 必填且一次最多 200 个；`types` 若给须与 `tokens` 等长，错位即本地报错、不下发。
- 不给 `types` 时一律按 file 处理，云文档也会被问成资源文件。
- 需 drive:drive:readonly，以应用（tenant）身份调用。

## Tips

- 每条只投影 `doc_token`、`doc_type`、`title`、`url`、`owner_id`；不回目录归属与大小。
- 部分失败不拖垮整批：查不到的进 `failed_lists`（每项 `token` 加 `code`）。
