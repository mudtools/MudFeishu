## WHEN

- 用户要"以后这类邮件自动归档/移走"，需要持续生效的分拣规则（不传 `rule_id` 即新建）。
- 某条规则的命中面或动作要调整，而它的 `rule_id` 与在序列中的位置要保留（传 `rule_id` 即整条替换）。
- 条件还没想清楚，先用 `is_enable`=false 建一条搁着。

## Avoid when

- 只处理眼前这几封——用 `mail.trash_message` / `mail.mark_read`，规则不针对点名 ID。
- 只想改先后——用 `mail.rule_reorder`，它不动内容。
- 这条彻底没用了——用 `mail.rule_delete`，本工具不删除。

## Prerequisites

- `conditions`/`actions` 用竖线分隔的简化式书写，不写 JSON、不填平台代码。
- move_to_folder 动作必须两段并带目标文件夹 ID；`user_mailbox_id` 是收信邮箱。
- 走替换路径时 `name`/`conditions`/`actions` 每次都要给全（整条替换）；`rule_id` 来自 `mail.rule_list`，它同时回显该条当前的简化式，照抄回填再改。

## Tips

- 式子在 `dry_run` 之前编译：非法字段/操作符、超 30 项、平台暂不支持的动作都会报错并附合法写法；`dry_run` 回显的是编译后的规则结构，不是你的原文。
- 省略 `match_all`/`is_enable`/`ignore_the_rest_of_rules` 会落回默认值：新建即按默认值建立，替换则会静默改掉这条原有的组合方式。
- `match_all` 默认 true，多条件要同时满足，命中面比按关键词设想的更窄。
- 把 `ignore_the_rest_of_rules` 设为 true，命中之后的其余规则就不再参与判断。
- 同一份内容重复提交结果一致，可安全重放。
