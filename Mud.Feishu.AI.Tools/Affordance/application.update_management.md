## WHEN
- 重新开放此前被停用的应用管理面，恢复管理员的后台操作权限。
- 与负责人明确确认后，临时冻结某个应用的后台管理入口，防止误配置。

## Avoid when
- 要改谁能看到这个应用——那是 `application.update_visibility`。
- 只想了解应用现状——`application.get_app` 是只读，别动开关。
- 要增删后台协作者——工具面未暴露该能力，只能用 `application.list_collaborators` 查看现状。

## Prerequisites
- `app_id`（形如 cli_xxx）经 `application.list_apps` 取到并核对。
- `enable` 必须显式给出，本工具不接受"省略即维持原样"。

## Tips
- 停用后管理员立即无法在后台管理该应用，直到再次以 enable=true 恢复。
- 漏 `enable` 的调用会被提前拒绝，`dry_run` 也救不回这次失败。
- 先 `dry_run` 核对开关值与 app_id，再向用户复述停用的后果并执行。
