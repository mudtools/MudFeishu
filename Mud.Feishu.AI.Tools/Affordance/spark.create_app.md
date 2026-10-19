## WHEN

- 需要从零起一个新的妙搭应用时才用本工具——它是整条搭建链的起点，后续配置与写数据都等这个返回的 app_id。
- 用户点名"建一个叫 X 的应用"，且 `spark.list_apps` 里没有同名可复用项。

## Avoid when

- 应用已存在，只是改名或换描述、图标——用 `spark.patch_app`。
- 要调谁能访问这个应用——用 `spark.update_app_visibility`，与创建无关。
- 不确定是否已经建过——先用 `spark.list_apps` 核对，避免建重复的。

## Prerequisites

- 走 user 身份且宿主已放行 user；需 spark:app 权限点。
- `name` 必填且不能为空白；`app_type`、`desc`、`icon_url` 都可选。

## Tips

- 创建不幂等：重复调用会产生多个同名应用，失败后别盲目重试。
- 先用 `dry_run` 预演再真正下发；拿到的 app_id 记牢，它是本域后续工具的输入。
