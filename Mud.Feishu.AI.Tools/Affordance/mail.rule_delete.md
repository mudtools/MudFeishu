## WHEN

- 规则本身写错了（条件命中面过宽、动作是删信），确认不再需要这条规则。

## Avoid when

- 只想让它暂时不生效——本接口没有停用开关，删了无法复原，只能重新 `mail.rule_save`。
- 想调的是执行次序——用 `mail.rule_reorder`，不要删了重建（重建会掉回队尾）。
- 想改条件或动作——用 `mail.rule_save` 整体覆盖。

## Prerequisites

- `rule_id` 来自 `mail.rule_list`（返回带 name/is_enable/conditions/actions，先核对是哪一条）。
- 删除前确认该邮箱确实归本应用管理：`user_mailbox_id` 传错会删到别人邮箱的无效 ID。

## Tips

- 删规则不会回滚规则已经做过的动作：被自动归档或删掉的邮件不会因此回来。
- 规则有先后语义，删掉中间一条会让后面的条件判断整体前移。
