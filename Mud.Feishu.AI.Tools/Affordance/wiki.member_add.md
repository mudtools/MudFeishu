## WHEN

- 要让某个人（或某个部门、某个群）拿到整个知识空间的访问权。
- 对方要长期打理这个空间，需要把角色直接给到管理员那一档。

## Avoid when

- 只是给某一篇文档授权——那是 `drive.grant_permission`，不必开整库权限。
- 收回权限——用 `wiki.member_remove`。
- 先看现有成员与角色——用 `wiki.member_list`。

## Prerequisites

- 调用身份必须是该空间的管理员；`member_type` 与 `member_id` 要成对匹配（如 openid 配 ou_xxx）。
- 以应用身份调用时不能用 opendepartmentid；公开空间（visibility=public）只能加管理员，加不上普通成员。

## Tips

- 把 `member_role` 填成 admin 是权限升级：对方随即能改空间设置与成员名单，给之前先确认。
- 默认不通知对方，需要提示时显式传 `need_notification`。
- 一次只加一个成员，多人要逐条调用；先 `dry_run` 核对身份类型与 ID 是否匹配。
