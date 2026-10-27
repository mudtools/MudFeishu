## WHEN

- 评论线程的处理已经完成，要把它**标记为已解决**（或误解决的重新打开）。

## Avoid when

- 想删除评论——本工具做不到，它只切解决状态，评论永远还在。
- 要在线程里写答复——用 `drive.reply_comment`。
- 要提一条新问题——用 `drive.add_comment`。

## Prerequisites

- comment_id 先经 `drive.list_comments` 取得（其解决状态筛选可帮你定位未处理线程）。
- `is_solved` 必须显式给：true=解决，false=恢复；漏传会被执行器直接拒绝。

## Tips

- 语义是**状态切换、非破坏**，天然幂等：重复设同一值不产生新后果，可安全重试。
- 解决动作对文档所有成员可见，批量处理前先 `dry_run` 核对线程。
