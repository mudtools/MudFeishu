## WHEN

- 只改文档的一小段，且你能给出与正文**逐字一致**的原文（含空白与标点）。
- 想先看命中情况再动手（`dry_run=true` 返回命中数与长度变化）。

## Avoid when

- 要整体重写——用 `docx.replace_document`，它不依赖原文匹配。
- 原文可能多处出现而你只想改一处——先扩大上下文让 `find` 唯一，别用 `replace_all` 赌。
- 替换跨块或涉及表格/代码块结构——改用 `docx.*` 块级工具。

## Prerequisites

- `document_token` 是新版文档（doxcn…）；`find` 非空。
- 需 docx:document + drive:drive:readonly；写工具默认不启用。
- `idempotency_key` 必填且稳定：同一次修改的重试必须复用同一值。

## Tips

- 命中 0 次返回 `changed=false`，不是错误；命中多次且未设 `replace_all` 会被明确拒绝。
- 实现是「整篇重建」：先追加新块再删旧块，中途失败不丢内容；删除失败返回 `partial_failure`。
- 改完用 `markdown.diff` 对比远端与你的期望文本。
