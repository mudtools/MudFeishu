## WHEN

- 需要 Markdown 形态的正文：编辑后走整篇写回链路，或作为可移植格式外发。

## Avoid when

- 要定位到块再精修——本工具不回块 ID，块级读法在 `docx.get_document_blocks` 一族。
- 只要纯文本——用 `docx.get_raw_content`，它走文档原生接口且更省。

## Prerequisites

- 仅支持新版文档（docx）；`document_id` 形如 doxcnXxx。
- 以 tenant 身份调用，但权限走 Drive 侧：需 drive:drive:readonly，不是 docx:document:readonly。

## Tips

- 返回一整段 Markdown 纯文本，`lang` 默认中文；图片、评论等结构细节带不出来。
- 整篇替换的写回会覆盖原文：写回前先重读一次最新正文，确认期间无人改动。
