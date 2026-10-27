## WHEN

- 要收紧或放开一个妙搭应用能被谁访问：限定人群、本租户、乃至全员外部。
- 需要顺带调整是否要求登录、申请开通方式这类访问门槛配置。

## Avoid when

- 改应用名称、描述、图标——那是元信息，用 `spark.patch_app`。
- 只想看当前范围——用 `spark.get_app_visibility`，别为查看发起写操作。
- 动应用里的表数据——走 `spark.list_tables` 与记录类工具，与此无关。

## Prerequisites

- 改之前先用 `spark.get_app_visibility` 读当前值，并与用户对齐目标范围。
- `visibility_json` 必须是 JSON 对象且 scope 必填（Public/Tenant/Range）；users、departments、chats 仅在 Range 下生效，白名单外字段本地即拒。

## Tips

- scope 置 Public 等于把应用亮给全员甚至租户外，放开前务必确认。
- `dry_run` 预演会给出 scope 与各主体列表的长度摘要，先看再改。
