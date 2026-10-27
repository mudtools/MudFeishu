## WHEN

- 事件驱动的联动流程已经下线，不再需要这条妙记通知渠道。
- 用户要求收回此前授予的事件推送授权，停止新妙记生成事件的送达。

## Avoid when

- 想开始接收妙记事件——那是 `minutes.subscribe`，别把方向弄反。
- 排查读不到妙记内容的问题——订阅与否不影响 `minutes.get` 等读取结果，退订解决不了它。

## Prerequisites

- 此前调用过 `minutes.subscribe`；`event_type` 闭集与订阅端一致，省略即用默认值。
- 宿主放行 user 身份且该写工具已获授权；需 minutes:minutes.basic:read。

## Tips

- 取消后仅停掉后续推送，已存在的妙记与链接不受影响。
- 已经下发到应用的历史事件不会因退订撤回，仍需消费端自行处理。
- 与订阅端共用 `dry_run` 预演，可先看将下发的 path 再执行。
