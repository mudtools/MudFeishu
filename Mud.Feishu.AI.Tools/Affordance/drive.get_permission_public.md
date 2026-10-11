## WHEN

- 分享文档**之前**要确认它当前的公开链接权限（`link_share_entity` / `external_access`），避免把内部资料发到组织外。

## Avoid when

- 只关心某个人有没有权限——那是协作者维度，不是公开设置。
- 只想要文件元数据（所有者 / 类型 / 时间）——用 `drive.get_file_metas`。

## Prerequisites

- 需要文档 `token`（文件或文件夹均可）。
- 需云文档只读权限。

## Tips

- 本工具是只读（GET），但属**权限域**、风险分级高，**仍会经过授权门禁与审计**。
- 它是 `drive.update_permission_public` 的**前置核对**：先看清现状再改。
