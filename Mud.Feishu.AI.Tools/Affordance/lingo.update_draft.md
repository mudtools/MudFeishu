## WHEN

- 此前提交的草稿内容有错漏（释义写错、别名要换、展示开关要调），且它还停在未审批状态。
- 词条尚未进入词库，需要反复修订内容、等待管理员那一步放行。

## Avoid when

- 草稿已审批通过——平台不允许再编辑，会直接报错；重新发起 `lingo.create_draft`。
- 要改的是词库里的正式词条——那是 `lingo.update_entity`，不是本工具。
- 还没提交过草稿、想新建——用 `lingo.create_draft`。

## Prerequisites

- `draft_id` 来自 `lingo.create_draft` 的回执；`main_key` 必填，不改名也要带上原词条名。
- 需 baike:entity 或 baike:entity:exempt_review 任一，宿主已放行该写工具。

## Tips

- 覆盖式提交：未传的释义/别名会被清空，把整份新内容合并好再发。
- 更新草稿不会跳过审核，仍需管理员通过后词条才进词库。
- `dry_run` 返回 `draft_id` 与将改字段数，用它核对改动范围。
