## WHEN

- 用户问「这个文件夹里有什么」，或只知道大概位置需要逐层浏览。
- 写工具需要一个目标位置，先取一个 folder 形态的 token。

## Avoid when

- 已知 token 只要确认类型与标题——用 `drive.get_file_metas`，不必列目录。
- 用户给的是标题关键词——那是 `search.doc_wiki`，搜索不等于浏览。
- 层级其实在知识库里——用 `wiki.list_nodes`，云空间目录不是知识库空间。

## Prerequisites

- `folder_token` 可选，缺省列根目录；下钻传上一层结果里的 folder 形态 `token`。
- 需 drive:drive:readonly，以应用（tenant）身份调用。

## Tips

- 每条只投影 `token`、`name`、`type`、`url`；大小、所有者与时间都不回。
- 默认单页；`has_more` 为真时用 `page_token` 续翻，`fetch_all` 触顶时回 `truncated`。
- 每页条数由绑定层固定，参数面调不了。
