## WHEN

- 词条与释义已经和业务方敲定，新术语要立刻进入词库并生效。
- 宿主已放行免审直写的写工具，这一步也不需要管理员再把关。

## Avoid when

- 需要管理员把关或流程要留痕——那是 `lingo.create_draft` 的场景。
- 词条已在库中——改内容用 `lingo.update_entity`，别重复建一条。
- 不确定有没有同名词条——先 `lingo.search_entities` 查一遍再决定建不建。

## Prerequisites

- `main_key` 必填；`description` 与 `rich_text` 至少一个非空；别名不超过 10 个。
- 不传 `repo_id` 时落到默认词库；要指定词库先查 `lingo.list_repos`。
- 需 baike:entity:exempt_review，宿主已授权该写工具（默认不启用）。

## Tips

- 免审直写没有管理员二次确认环节，先 `dry_run` 核对词条名与别名数再执行。
- 回执中的 entity_id 是全新 ID，后续更新、删除都以它为准。
- 建错只能 `lingo.delete_entity` 撤掉，而删除不可逆。
