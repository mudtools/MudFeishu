## WHEN

- 同一项目的多条待办需要一个共同的协作面来组织。

## Avoid when

- 只有一条零散待办——直接 `task.create_task`，为它建清单是多余动作。
- 清单已存在只是名字不对——那是 `task.tasklist_update`。
- 要往已有清单补人——那是 `task.tasklist_member_add`。

## Prerequisites

- `name` 必填，最长 100 个字符。
- 初始协作成员的 open_id 先经 `contact.search_user` 反查；他们拿到的是清单权限，不是某条任务的负责人身份。

## Tips

- 本工具没有幂等键入参：重试会建出第二个同名清单，只能事后 `task.tasklist_delete` 收拾。
- 返回的清单 GUID 是清单族后续所有工具的入口，务必留存。
