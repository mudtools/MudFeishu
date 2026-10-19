## WHEN

- 要对整篇云文档留一条**全文评论**（不锚定正文某处），如审阅后提出总体意见。

## Avoid when

- 要回应已有评论——用 `drive.reply_comment` 在原线程里说，不要再开一条新评论。
- 线程已处理完、只需结掉——用 `drive.resolve_comment`，它不写内容。
- 还不确定文档上有什么意见——先 `drive.list_comments`，再决定新评还是回复。

## Prerequisites

- 先用 `drive.list_folder_files` 或既有链接确认 `file_token`，且 `file_type` 与对象真实类型一致。
- 评论以应用身份落笔，对全部文档成员可见。

## Tips

- 只支持全文评论，局部（划词）评论无法经本工具表达。
- `text` 为空会被拒绝；成功产出的是评论标识——后续回复与解决都要拿它定位线程。
