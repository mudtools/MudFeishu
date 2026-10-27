## WHEN

- 要在既有知识空间里新开一页，这次只把位置与标题定下，正文之后再补。
- 需要的对象不只是文档：表格、思维笔记、多维表格、文件都从这里建。

## Avoid when

- 内容已是一篇云文档，只想收进库——用 `wiki.move_docs_to_space`，本工具不搬正文。
- 页面已在库里，只是位置不对——用 `wiki.move_node`。
- 要的是同样内容的第二份——用 `wiki.node_copy`。
- 还没有可用空间——先 `wiki.space_list`，空列表时用 `wiki.space_create`。

## Prerequisites

- `space_id` 形如 7xxx，来自 `wiki.space_list` 或 `wiki.list_nodes`，它与文档 token 不是同一个东西。
- `title` 必填；要挂到某页之下时 `parent_node_token` 取自 `wiki.get_node`，留空即落在空间顶层。

## Tips

- 回执给两个 token：一个是树上的位置，一个是底层对象；跨域取内容要用后者。
- 回执没有节点信息不等于失败，用 `wiki.list_nodes` 复核后再决定要不要重试。
