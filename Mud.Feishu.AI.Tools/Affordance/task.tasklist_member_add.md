## WHEN

- 某人要能看到并参与一个清单里的全部任务。

## Avoid when

- 只让他负责某一条任务——那是 `task.add_members`，清单成员与任务成员是两层。
- 要撤掉这个人的清单权限——那是 `task.tasklist_member_remove`。
- 想给他指定角色——本工具没有角色入参，别指望在这里调权。

## Prerequisites

- 清单 GUID 来自 `task.tasklist_page_list`，它和任务的 GUID 是两套 ID，不能互填。
- `member_ids` 只收 open_id；成员类型由工具层固定，不用自己拼。

## Tips

- 新加的成员不带角色，落到平台默认的最小权限，能看到不等于能改。
- 没有幂等键，同一批人重复提交就是重复添加；先 `dry_run` 核对名单再落地。
