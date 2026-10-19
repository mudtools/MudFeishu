## WHEN

- 要落一条可跟踪、有人负责的待办事项。

## Avoid when

- 只是口头提示某人一句——发消息就够，别为它建任务。
- 一次要立一整批同项目待办——先 `task.tasklist_create` 把协作面立起来。
- 是给已有任务拆步骤——那是 `task.create_subtask`。

## Prerequisites

- 负责人先 `contact.search_user` 拿 open_id，`assignee_ids` 不收姓名。
- `due` 必须是带时区的 RFC3339；工具层才转成平台要的毫秒时间戳，缺时区直接拒。

## Tips

- `idempotency_key` 省略时没有幂等兜底，重试会建出两条一模一样的待办。
- 成功只回一个任务 GUID；提醒、评论、成员都要建完再逐个补。
