## WHEN

- 已有的文件/文件夹要在云空间里换一个归属目录（归档、整理层级）。

## Avoid when

- 要新增内容——挪动不产生新文件，用 `drive.upload_file` 或 `drive.create_folder`。
- 想「挪过去顺便调权限」——本工具只挪位置；扩大或收窄可见范围走 `drive.grant_permission`、`drive.update_permission_public` 这条独立通道。

## Prerequisites

- 源 `file_token` 与目标 `folder_token` 都要先经 `drive.list_folder_files` 确认，`type` 须与对象真实类型一致。

## Tips

- 移动是**异步执行**：返回的是任务标识，立即返回不代表已生效；汇报完成前以两端目录实际内容为准复核。
- 落点是根目录还是某个文件夹语义不同，确认 `folder_token` 后再执行；首次建议 `dry_run` 预演。
