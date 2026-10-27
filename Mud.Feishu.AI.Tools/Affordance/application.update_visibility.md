## WHEN
- 把应用追加开放给新接入的用户、部门或用户组，让他们能用起来。
- 从可用范围里摘除指定对象，或让应用整体在全员可见与名单控制间切换。

## Avoid when
- 要看当前谁能看到——`application.get_app_visibility` 是只读检查。
- 要启停应用的管理面——那是 `application.update_management`。
- 要改应用名称或图标——工具面没有该能力，去开发者后台处理。

## Prerequisites
- `app_id` 来自 `application.list_apps`。
- 增与删各有独立参数，用户、部门、用户组三组别串位；口径由 `user_id_type` / `department_id_type` 声明。

## Tips
- 增量语义：未传的名单原样保留，不需要回填现状清单。
- 一个名单和 `is_visible_to_all` 都没给，会被本地按空更新拦下。
- `dry_run` 预演回报改动条数；条数与预期不符时逐项检查名单参数。
