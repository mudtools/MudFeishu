## WHEN

- 修正或补充已有词条：改释义、换别名、调高亮与搜索命中开关。
- 改动不需要审核留痕，且宿主已经放行免审直写的写工具。

## Avoid when

- 词条还不存在——先 `lingo.create_entity`，或走审核用 `lingo.create_draft`。
- 词条要作废移除——那是 `lingo.delete_entity`，别靠清空字段模拟删除。
- 改的是未审批的草稿——用 `lingo.update_draft`。

## Prerequisites

- `entity_id` 从 `lingo.search_entities` 或 `lingo.get_entity` 取得；`main_key` 必填，只想改释义也要带上原词条名。
- 需 baike:entity:exempt_review；提交前先 `lingo.get_entity` 读出原值并合并。

## Tips

- 覆盖式提交：不传 `aliases` 会清空原别名；`description` 与 `rich_text` 至少一个非空，本地校验早于 `dry_run`。
- `dry_run` 回执给将改动的字段数，可用来确认没漏带原内容。
- 免审即生效，没有审核缓冲。
