## WHEN

- 保护区域保持不变，只调整谁能编辑（加人/减人）或锁定信息。
- 交接、临时给某人开编辑权、结束后收回。

## Avoid when

- 要改锁住的是哪一段——本工具改不了区域，走 `sheets.protected_create` 建新范围再删旧范围。
- 整片不再需要保护——用 `sheets.protected_delete`。

## Prerequisites

- `protect_id` 来自 `sheets.protected_get`（或当初创建的返回）。
- `add_editors` / `remove_editors` 的成员 ID 类型要与 `editor_member_type` 配套；三项待更新内容至少给一项，全空在本地即被拒。

## Tips

- 成员是增删动作，不是覆盖整份名单：传 `remove_editors` 不会把其他人一起清掉。
- 回执只回显该范围的工作表与 lock_info，加减后的完整名单要回 `sheets.protected_get` 核对。
