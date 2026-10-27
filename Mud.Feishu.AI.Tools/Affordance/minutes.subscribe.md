## WHEN

- 用户希望应用此后能自动收到其新妙记生成的通知，用于会后自动摘要之类的联动流程。
- 机器人要在妙记一生成就立刻做后续任务，需要事件推送作为触发源。

## Avoid when

- 只想现在读某一场已完成的妙记——直接 `minutes.get_artifacts`，无需订阅。
- 要收回事件授权、停止推送——那是 `minutes.unsubscribe`。
- 想按关键词翻历史妙记——用 `minutes.search`，订阅不承担检索职能。

## Prerequisites

- 宿主须放行 user 身份，且 WebSocket 或 Webhook 事件通道已接入，否则回调无处送达。
- 需 minutes:minutes.basic:read，并经宿主授权启用该写工具。

## Tips

- `event_type` 是闭集，当前只有妙记生成这一类事件，省略即用默认值。
- 订阅只改后续事件的流向，不改动任何已有妙记内容。
- 重复订阅不会叠加出第二条订阅关系，失败后可放心重试。
