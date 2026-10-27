## WHEN

- 一个文件已存在于 **http/https 网络地址**，要把它取进云空间成为文件实体。

## Avoid when

- 要把云空间里的文件取回本地——方向相反，用 `drive.download_file`。
- 只有本地路径 / 没有可下载 URL——本工具不接受本地路径。
- 想要的是文档正文内容——云文档走 `drive.export_document` 域，不是上传。

## Prerequisites

- 宿主必须已实现附件暂存件；未实现时本工具**不注册**（软缺席，不是你的参数错了）。
- 目标 `folder_token` 经 `drive.list_folder_files` 确认；缺省落根目录。

## Tips

- 执行是两段式：宿主先按 `file_url` 下载落盘，再上传；失败先分清是 URL 不可达还是安全域拒绝了来源地址。
- 成功返回文件实体 token；上传本身不带任何共享设置。
