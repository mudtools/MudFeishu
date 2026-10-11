## WHEN

- 要把某份文档交给**指定的人或群**协作（新增单个协作者）。

## Avoid when

- 想开放给"任何拿到链接的人"——那是公开设置，用 `drive.update_permission_public`。
- 对方已在权限列表里、只想改档位——用 `drive.update_permission_member`。

## Prerequisites

- 先确认文档 `token`，并用 `drive.get_permission_public` 看清当前分享面（防意外扩大）。
- `member_type` 取值需与传入的成员标识形态一致（`openid` / `email` / `userid` / `chatid` / `departmentid`）。

## Tips

- 本操作**扩大数据可达范围**；收窄只能用 `drive.remove_permission` 或改档位，成本更高。
- 建议先 `dry_run` 预演，核对成员标识与权限档位。
