## WHEN

- 需要一篇**新文档**来承载后续内容：先建空壳，再用块级工具往里写。
- 要把新文档直接落到某个云空间文件夹里。

## Avoid when

- 内容要在知识空间里——那是 `wiki.create_node`；已有文档挪进知识库用 `wiki.move_docs_to_space`。
- 只是给现有文档加正文——用 `docx.append_blocks`，不要另起一篇。
- 想拿 Markdown 直接当正文——本工具不收 Markdown，先 `docx.import_markdown` 看块。

## Prerequisites

- `folder_token` 是云空间文件夹的 token，可用 `drive.list_folder_files` 浏览取得；不填就落在根目录。
- `title` 可缺省，缺省即用平台默认标题，之后再定名。

## Tips

- 返回只有一个文档 ID，它同时充当后续所有块级工具的父块（根块）坐标。
- 本工具没有幂等键：调用失败后盲目重发会建出第二篇同名文档，先确认是否已建成。
- 建完是空壳，正文要靠 `docx.append_blocks` 写；可用 `docx.get_raw_content` 读回核对。
