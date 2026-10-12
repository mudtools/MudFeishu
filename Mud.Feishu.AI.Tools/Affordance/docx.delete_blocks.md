## WHEN

- 文档里有一批段落需要整段移除（过期章节、重复粘贴的内容）。

## Avoid when

- 只是想改文案——用 `docx.update_block` / `docx.update_blocks`。
- 想用一份新内容换掉整篇——用 `docx.replace_document`，一次调用语义更清晰。

## Prerequisites

- 先用 `docx.get_document_blocks`（或 `docx.get_raw_content`）确认父块与子块索引区间。
- 区间是**父块下的子块序号（从 0 开始）**，必须按当前结构计算，不能凭记忆。

## Tips

- **删除不可撤销**；且区间按"执行时的索引"生效——先删前面的块会让后面的索引整体前移。
- 务先 `dry_run` 预演，核对待删区间后一次执行完。
