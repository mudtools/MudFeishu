## WHEN

- 要把整篇正文换成你手头的 Markdown（重排、批量修订、从模板重建）。
- 远端内容已不可信，需要以本地版本为准整体覆盖。

## Avoid when

- 只改一小段——用 `markdown.patch`，命中校验能防误改。
- 文档里有必须保留的评论或非正文结构——重建会替换正文块，先确认这些不随块丢失。
- 对象不是新版文档——本工具只作用于 docx。

## Prerequisites

- `document_token`（doxcn…）+ 非空 `markdown`。
- 需 docx:document；写工具默认不启用，启用前须宿主授权。
- `idempotency_key` 必填：重试复用同一值，否则可能重复插入。

## Tips

- 先 `dry_run=true` 看步骤，再真写。
- 写入顺序是「先追加、后删除」：任何一步失败都不会让正文变空。
- 删除失败时返回 `partial_failure=true` 与修复指引，按指引清理旧块，不要盲目重跑整篇。
