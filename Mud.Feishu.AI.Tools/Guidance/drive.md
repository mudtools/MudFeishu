# 云空间（drive.*）

## 前置链
`list_folder_files` / `get_file_metas` 只回元数据；写面 `create_folder` / `move_file` / `upload_file`。

## 避坑
- **只读工具不下载内容**，取正文请用 docx / sheets。
- `move_file` 是异步的，立即返回不代表已生效。

## 示例
这个文件夹里有什么 → `list_folder_files`。