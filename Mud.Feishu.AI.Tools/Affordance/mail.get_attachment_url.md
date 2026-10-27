## WHEN

- 用户要的是附件文件本体，先拿到可下载链接再交给宿主处理。
- 已经确定是哪封邮件、且知道要取哪个附件。

## Avoid when

- 只想读邮件写了什么——用 `mail.get_message`，本工具不读正文。
- 期望结果里就是文件——本工具只回链接，不落盘；云空间文件才走 `drive.download_file`。
- 还不知道是哪封邮件——先 `mail.search` 或 `mail.list_messages` 定位。

## Prerequisites

- `user_mailbox_id`、`message_id`、`attachment_ids` 都必填，一次最多 100 个附件 ID。
- 需 mail:mailbox:readonly，以应用（tenant）身份调用。
- 宿主须启用 AddMailApi 的消息侧客户端，缺席时调用即报错。

## Tips

- 回执分两组：`downloads`（每项 `attachment_id` 加 `download_url`）与 `failed_ids`，个别失败不整批报错。
- 链接有时效，取到就用，不要留到后续步骤再回来复用。
- 附件 ID 在本域只读投影里没有出口——读信工具不回附件字段，需由宿主或事件上下文提供。
