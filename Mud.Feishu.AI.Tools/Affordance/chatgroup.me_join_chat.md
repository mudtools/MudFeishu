## WHEN
- 机器人自己还不在目标群里，而接下来要在群内发言或做群操作，进群是前置一步。
- 用户要求应用进某个公开群值守，代为接收与响应群内消息。

## Avoid when
- 要拉其他成员进群——那是 `chatgroup.add_member`。
- 只需确认在不在群——`chatgroup.is_in_chat` 是只读检查，不必发起写操作。
- 要机器人建一个新群——用 `chatgroup.create_chat`。

## Prerequisites
- 只填一个 `chat_id`；进群的是当前应用身份，不能替某个用户入群。
- 私密群要先被群内有人邀请，邀请缺失时本工具调不通。

## Tips
- 缺的是邀请而不是重试：失败时如实告知用户需要先把应用拉进群。
- `dry_run` 预演只回报将要加入的群 ID，不下发。
- 执行后可用 `chatgroup.is_in_chat` 复核在群状态。
