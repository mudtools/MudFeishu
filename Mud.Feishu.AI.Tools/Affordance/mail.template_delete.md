## WHEN

- 模板已废弃（签名变了、话术不再用），且确认没人依赖它起草。

## Avoid when

- 只是内容过时——用 `mail.template_update` 整体覆盖，保留 template_id。
- 想换一份措辞但不想丢掉原件——删除后不可恢复，只能重新 `mail.template_create`。

## Prerequisites

- `template_id` 来自 `mail.template_list`；删前用 `mail.template_get` 核对 subject 与正文，确认命中的是目标模板。
- `user_mailbox_id` 是模板所属邮箱地址，不是操作人 ID。

## Tips

- 模板是邮箱维度资源：同一应用下换邮箱传参，删不到你想删的那份。
- 删除只影响模板本身，不影响此前由它生成的草稿或已发送邮件。
