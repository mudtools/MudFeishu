## WHEN

- 已有确切块 ID，要看这一块自身的类型、文本预览与直接子块名单。

## Avoid when

- 还没有块 ID——先 `docx.get_document_blocks` 或 `docx.list_block_children` 枚举。
- 要成批翻页看子块——用 `docx.list_block_children`，本工具一次只看一块。

## Prerequisites

- `block_id` 来自块序列回执或另一块回传的 `children`；`document_id` 与其同源。
- 以 tenant 身份调用，需 docx:document:readonly 权限。

## Tips

- `block_type` 回的是块型名称字符串，不是数值编码——别和整篇块序列的取值口径混用。
- `text` 是截断预览且只含文本元素；`children` 仅在确有子块时出现，且只有 ID，内容需逐个再查。
