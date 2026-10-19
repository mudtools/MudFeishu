## WHEN

- 企业新增术语，但流程要求词典管理员先审核留痕，不能直接入库。
- 当前身份没有免审权限，只能靠草稿流程把内容递进去。

## Avoid when

- 有免审权限且不需要审核留痕——直接写库用 `lingo.create_entity`。
- 草稿已存在、只是内容要修——用 `lingo.update_draft`，别再发起一份新的。
- 词库里已有该词条——用 `lingo.update_entity` 改正式词条，草稿不是更新入口。

## Prerequisites

- `main_key` 必填；`description` 与 `rich_text` 都为空会被本地先拦（早于 `dry_run`）。
- `repo_id` 取自 `lingo.list_repos`；省略则写入默认词库。
- 需 baike:entity 或 baike:entity:exempt_review 任一，且宿主已放行该写工具。

## Tips

- 成功只代表申请已提交，词条待管理员审批通过才入词库，审核结论要找管理员确认。
- 别名最多 10 个、释义不超过 5000 字符，超限在本地即被拒绝。
- 先 `dry_run` 查看将要提交的词条名与别名数，再真正下发。
