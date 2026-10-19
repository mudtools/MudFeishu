## WHEN

- 要在一件事下面留一条可追溯的补充说明或结论。

## Avoid when

- 要改任务本身的标题、描述或截止时间——那是 `task.update_task`，评论不动任务字段。
- 只是想让某人知道一声——发消息更轻；评论只挂在任务上，不产生任何会话通知。

## Prerequisites

- `task_guid` 来自 `task.create_task` 或 `task.list_my_tasks`，评论归属的就是这个 ID。
- `content` 必填，最长 3000 个 utf8 字符。
- 要回复谁才传 `reply_to_comment_id`，它的来源只有本工具返回的 comment_id。

## Tips

- 本工具没有幂等键入参：重试同一意图会再插一条评论，失败后先核对再重发。
- 不确定挂得对不对时先 `dry_run` 看请求摘要；走的是租户令牌面。
