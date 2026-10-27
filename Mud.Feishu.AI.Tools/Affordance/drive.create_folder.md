## WHEN

- 需要在云空间里新增一层目录来归档文件（如按项目或月份建文件夹）。

## Avoid when

- 已有文件要挪进目录——那是 `drive.move_file`，建目录之前先想清楚缺的是哪一个。
- 要把网络文件落进云空间——先 `drive.create_folder` 建好目标，再 `drive.upload_file`。
- 用户说的「文件夹」可能在知识库里——先用 `drive.list_folder_files` 看实际内容再定域。

## Prerequisites

- 嵌套装目录时父 `folder_token` 必须先存在（用 `drive.list_folder_files` 核实）；父目录不存在会直接失败。

## Tips

- 省略 `folder_token` 会建到根目录——用户没明说位置时不要默认放根。
- 成功即返回新目录 token；接续的写操作先用 `dry_run` 预演再落笔。
