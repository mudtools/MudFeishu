---
name: feishu-drive
description: 飞书 drive 域工具集：15 个工具（只读 3 / 写 12）。文件在哪 / 叫什么 / 多大 / 谁所有 → `drive.*`；取正文 → `docx.*`（文档）或 `sheets.*`（表格）；
version: 1.0.0
---

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

## 命令（工具）清单
- `drive.add_comment`（写）：向云文档添加一条全文评论（不支持局部评论）。参数为 file_token + file_type + text，请求体由绑定层组装（模型零 JSON 串）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 drive:drive。
- `drive.create_folder`（写）：在云空间中创建文件夹（返回 folder_token，可用 drive.list_folder_files 验证）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 drive:drive。
- `drive.get_file_metas`（只读）：按 token 批量查询文件元信息（类型/标题/链接/所有者，最多 200 个）；支撑后续读文档链（docx.get_raw_content 等）。只读，需 drive:drive:readonly。
- `drive.get_permission_public`（写）：获取云文档的公开链接权限设置（link_share_entity/external_access 等），用于分享前确认当前文档的可见范围。实际只读（GET），但因权限域风险分级须过授权门禁，建议先 dry_run 预演确认调用面，需 drive:drive:readonly。
- `drive.grant_permission`（写）：为指定云文档添加单个协作者权限（member_type 为 openid/email/userid/chatid/departmentid）。⚠️ 此操作可扩大数据可达范围，建议先 dry_run 预演确认。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 drive:drive。
- `drive.list_comments`（只读）：列出云文档的所有评论（comment_id/user_id/is_solved/reply_count/created_at）。先获取评论列表，再用 drive.reply_comment 或 drive.resolve_comment 操作。只读，需 drive:drive:readonly。
- `drive.list_folder_files`（只读）：列出云空间文件夹内的文件与子文件夹（token/name/type/url）；folder_token 缺省列根目录。与 search.doc_wiki 互补（浏览 vs 搜索）。只读，需 drive:drive:readonly。
- `drive.move_file`（写）：将文件或文件夹移动到指定文件夹下（返回异步任务 ID——移动操作为异步执行，需后续轮询确认完成）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 drive:drive。
- `drive.remove_permission`（写）：删除指定协作者的权限（删除天然幂等）。建议先 dry_run 预演确认。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 drive:drive。
- `drive.reply_comment`（写）：回复指定评论（需 comment_id + text）。空文本会在下发前被拒绝。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 drive:drive。
- `drive.resolve_comment`（写）：解决或恢复评论（is_solved 三态），不是删除——天然幂等。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 drive:drive。
- `drive.transfer_owner`（写）：转移云文档所有者（不可逆！转移后原所有者降为编辑者）。必须过授权器。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 drive:drive。建议先 dry_run 预演确认。
- `drive.update_permission_member`（写）：更新指定协作者的权限档位（view/edit/full_access），幂等操作。建议先 dry_run 预演确认。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 drive:drive。
- `drive.update_permission_public`（写）：更新云文档的公开链接权限设置。⚠️ 此操作可能使文档对组织外可见——请先 drive.get_permission_public 确认当前设置，并优先使用 dry_run 预演。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 drive:drive。
- `drive.upload_file`（写）：将网络文件上传到云空间（file_url 为 http/https 绝对地址，由宿主负责下载落盘——与 im.send_image 同机制）。返回 file_token。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 drive:drive。

## 参考文档（细则）
- `references/upload-and-import.md`：`drive/upload-and-import`
