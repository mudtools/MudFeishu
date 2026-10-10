# 云空间上传 / 移动 / 元数据

## 前置链
1. `drive.list_folder_files` 拿 `folder_token` / `file_token`
2. 需要文件信息（名称/类型/大小/所有者）用 `drive.get_file_metas`
3. 落笔：`drive.create_folder`（建目录） / `drive.move_file`（移动） / `drive.upload_file`（上传）

## 避坑
- **只读工具不下载正文**：`list_folder_files` / `get_file_metas` 只回元数据。
  要正文请按对象类型进 `docx.*`（文档）或 `sheets.*`（表格）。
- **`move_file` 是异步的**：立即返回不代表已生效；跨文件夹/跨知识库的移动尤其如此。
- `upload_file` **不接受本地路径**：只接受 http/https 绝对地址，且宿主必须实现附件暂存件
  （未实现时该工具不注册——这是软缺席，不是参数错误）。
- `create_folder` 的父目录若不存在会失败；先用 `list_folder_files` 确认父 token。
- 元数据里的名称可能与用户口语不同（含扩展名/后缀），匹配不到时先列目录而不是猜 token。

## 示例
- 「这个文件夹里有什么」→ `list_folder_files`
- 「把这份文件挪到归档夹」→ `list_folder_files` 找两个 token → `move_file`