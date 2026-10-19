---
name: feishu-wiki
description: 飞书 wiki 域工具集：12 个工具（只读 5 / 写 7）。知识库节点树的定位与移动 → `wiki.*`；取正文 → `docx.*`（文档）或 `sheets.*`（表格）；
version: 1.0.0
package_version: 3.0.3
---

# 知识库（wiki.*）

## 路由优先级（先判断对象属于哪个域）
- **知识库节点树的定位与移动** → `wiki.*`；**取正文** → `docx.*`（文档）或 `sheets.*`（表格）；
  **看文件列表/元数据** → `drive.*`。
- 知识库节点有一个 `node_token` 与一个底层对象 `obj_token`：跨域取内容时要用后者。

## 前置链
`list_nodes` / `get_node` 取 node_token 与 obj_token → 喂给 `docx.get_raw_content` / `sheets.get_range_values`；
写 `create_node` / `move_node` / `move_docs_to_space`。
不知道 `space_id` → `space_list`；一个也没有 → `space_create`（用户身份，返回的 `space_id` 直接喂 `create_node`）；看某空间开放范围 → `space_get`；
授权与撤权 → `member_list` 核对 → `member_add` / `member_remove`。

## 避坑
- **`move_docs_to_space` 是异步任务**，看返回的 `applied` 字段，不要假定立即生效。
- **`move_node` 目标全空会「成功但不移动」**，工具会提前拒绝——按提示补目标参数。
- 迁入已有云文档只能用 `move_docs_to_space`。
- **`space_list` 空列表 ≠ 企业没有知识库**：结果按可见性过滤，只代表该身份没被加入任何空间。
- **`member_remove` 靠 member_id / member_type / member_role 三项定位**，与 `member_list` 给的不一致时平台不报错。
- **`node_copy` 产出的是新 node_token 与新 obj_token**（底层文档一并复制）；要保 token 用 `move_node`。
- 公开空间（visibility=public）加 member 会被拒，只能加 admin。
- **`space_create` 只有用户身份形态**：宿主须放行 user，创建者自动成为该空间管理员；应用（机器人）看不到新空间，还要 `member_add` 把它加进去。
- 空间 / 节点树结构与 move 语义 → `feishu.guidance_read(wiki, space-node-tree)`。

## 安全规则
- 写工具首次调用一律 `dry_run=true` 预演；确认 method/path 与字段摘要无误后，以 `dry_run=false` 重放**同一参数**。
- high-risk-write 工具须宿主注册 `IToolExecutionAuthorizer` 才会放行，未注册时一律拒绝。
- 只使用本域真实工具名，不臆造不存在的工具或 API。

## 示例
把这份文档挪进知识库 → `move_docs_to_space`。

## 命令（工具）清单
- `wiki.create_node`（写）：在知识空间下创建节点（新建一篇 wiki 文档/普通页面）。space_id 可由 wiki.list_nodes 的结果推断。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 wiki:wiki。
- `wiki.get_node`（只读）：解析知识库节点信息（node_token/title/obj_type/obj_token）；obj_token 可传给 docx.get_raw_content 读取正文。只读，需 wiki:wiki:readonly。
- `wiki.list_nodes`（只读）：列出知识空间（或某父节点下）的子节点列表；node_token 可传给 wiki.get_node 解析详情。只读，需 wiki:wiki:readonly。
- `wiki.member_add`（写）：为知识空间添加成员或管理员（member_role：admin=管理员 / member=成员）。⚠️ 这是授权动作：一次调用即让该身份看到整个空间；公开空间（visibility=public）不支持加成员、只能加管理员；以应用身份调用时不能用 opendepartmentid。调用身份须为该空间管理员。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），建议先 dry_run 预演确认，需 wiki:wiki。
- `wiki.member_list`（只读）：列出知识空间的成员与管理员（member_type/member_id/member_role）。wiki.member_remove 需要同时提供 member_type 与 member_role，删前先调本工具核对；调用身份须是该空间的管理员，否则平台报权限不足。只读，需 wiki:wiki:readonly。
- `wiki.member_remove`（写）：移除知识空间的成员或管理员。high-risk-write：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 wiki:wiki。建议先 dry_run 预演确认。平台按 member_id + member_type + member_role 三元组定位，角色填错会删不到人——先用 wiki.member_list 核对三项再动手。
- `wiki.move_docs_to_space`（写）：把已有云文档（docx/sheet/bitable 等）迁移进知识空间，成为 wiki 节点——'把这份文档挪进知识库'的首选入口。⚠️ 平台以异步任务执行，返回 task_id。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 wiki:wiki。
- `wiki.move_node`（写）：移动知识空间节点（改父节点或换空间）——用于知识库整理、归档。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 wiki:wiki。
- `wiki.node_copy`（写）：复制知识空间节点副本到指定位置（原节点保留，产出新的 node_token）——'把这个页面复制一份到另一个空间/目录'用本工具；要改位置且保留 token 用 wiki.move_node。target_space_id 与 target_parent_token **至少提供一个**（两者都不传平台会拒绝，不会复制在当前空间顶层）；副本的底层文档也会被一并复制（obj_token 与原来不同）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAu…
- `wiki.space_create`（写）：创建一个新的知识空间（返回 space_id，可直接作为 wiki.create_node / wiki.member_add 的上游输入）。⚠️ 仅支持用户身份调用：宿主须在 FeishuAgent:AllowedIdentities 放行 user，否则会因身份被拒而失败。创建者自动成为该空间的管理员。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），建议先 dry_run 预演确认，需 wiki:wiki。
- `wiki.space_get`（只读）：按 space_id 获取单个知识空间信息（name/description/space_type/visibility/open_sharing）。查'这个知识库对谁开放、是什么类型'用它；visibility=public 表示租户内全员可见，个人空间（space_type=person）不支持添加其他管理员。只读，需 wiki:wiki:readonly。
- `wiki.space_list`（只读）：列出当前调用身份有权限访问的知识空间（space_id/name/space_type/visibility），space_id 可喂 wiki.list_nodes / wiki.member_list。⚠️ 结果按可见性过滤：返回空列表表示『该身份没有被加入任何空间』，不代表企业里没有知识库。只读，需 wiki:wiki:readonly。

## 参考文档（细则）
- `references/space-node-tree.md`：`wiki/space-node-tree`
