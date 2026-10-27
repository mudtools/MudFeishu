## WHEN

- 要把某份文档交给**指定的人或群**协作——对方还没有权限（新增）或已有权限要换档位（改档），都用这一个入口。

## Avoid when

- 想开放给"任何拿到链接的人"——那是公开设置，用 `drive.update_permission_public`。
- 想收回权限——用 `drive.remove_permission`。
- 想连所有权一起交割——用 `drive.transfer_owner`（比改档位重得多）。

## Prerequisites

- 先确认文档 `token`，并用 `drive.get_permission_public` 看清当前分享面（防意外扩大）。
- `member_type` 取值需与传入的成员标识形态一致（`openid` / `email` / `userid` / `openchat` / `opendepartmentid` / `unionid` / `groupid`）。

## Tips

- 工具层先读协作者列表再决定走新增还是改档：无需自己先判断"对方是否已有权限"，返回的 `action` 会告诉你实际走了哪条（`created` / `updated`）。
- 本操作**扩大数据可达范围**；收窄只能用 `drive.remove_permission`，成本更高，改档位时别把 `view` 误写成 `edit`。
- 建议先 `dry_run` 预演，核对成员标识与权限档位。
