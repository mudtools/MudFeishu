## WHEN

- 用户要"以后这类邮件自动归档/移走"，需要持续生效的分拣规则。
- 条件还没想清楚，先用 `is_enable`=false 建一条搁着。

## Avoid when

- 只处理眼前这几封——用 `mail.trash_message` / `mail.mark_read`，规则不针对点名 ID。
- 已有那条要改条件或动作——用 `mail.rule_update`，别堆出两条重叠规则。
- 关心的是谁先命中——用 `mail.rule_reorder`，新建只能往序列尾部追加。

## Prerequisites

- `conditions`/`actions` 用竖线分隔的简化式书写，不写 JSON、不填平台代码。
- move_to_folder 动作必须两段并带目标文件夹 ID；`user_mailbox_id` 是收信邮箱。

## Tips

- 式子在 `dry_run` 之前编译：非法字段/操作符、超 30 项、平台暂不支持的动作都会报错并附合法写法。
- `match_all` 默认 true，多条件要同时满足，命中面比按关键词设想的更窄。
- 把 `ignore_the_rest_of_rules` 设为 true，命中之后的其余规则就不再参与判断。
