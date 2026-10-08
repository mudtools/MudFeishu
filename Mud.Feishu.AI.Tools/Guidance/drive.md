
# 云空间（drive.*）

## 路由优先级（先判断对象属于哪个域）
- **文件在哪 / 叫什么 / 多大 / 谁所有** → `drive.*`；**取正文** → `docx.*`（文档）或 `sheets.*`（表格）；
  **知识库层级** → `wiki.*`。
- 用户的「文件夹」在飞书里可能是云空间目录、也可能是知识库空间——先用 `list_folder_files`
  看到实际内容再判断，不要凭措辞选域。

## 前置链
`list_folder_files` / `get_file_metas` 只回元数据；写面 `create_folder` / `move_file` / `upload_file`。
链式路径：先拿 `folder_token` / `file_token`，再动写工具。

## 避坑
- **只读工具不下载内容**，取正文请用 docx / sheets。
- `move_file` 是异步的，立即返回不代表已生效。
- `upload_file` 只接受 http/https 绝对地址（不接受本地路径），且宿主须实现附件暂存件；
  未实现时该工具**不注册**（软缺席，不是参数错误）。
- 上传 / 移动 / 导入的差别与前置件 → `feishu.guidance_read(drive, upload-and-import)`。

## 安全规则
- 写工具首次调用一律 `dry_run=true` 预演；确认 method/path 与字段摘要无误后，以 `dry_run=false` 重放**同一参数**。
- high-risk-write 工具须宿主注册 `IToolExecutionAuthorizer` 才会放行，未注册时一律拒绝。
- 只使用本域真实工具名，不臆造不存在的工具或 API。

## 示例
这个文件夹里有什么 → `list_folder_files`。
