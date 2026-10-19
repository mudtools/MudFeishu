## WHEN

- 某条规则的命中面或动作要调整，而它的 `rule_id` 与在序列中的位置要保留。
- 要让这条暂时不生效但以后还用：把 `is_enable` 写成 false。

## Avoid when

- 只想改先后——用 `mail.rule_reorder`，它不动内容。
- 这条彻底没用了——用 `mail.rule_delete`，本工具不删除。
- 还没有这条——用 `mail.rule_create`，别靠更新一个不存在的 ID。

## Prerequisites

- `rule_id` 来自 `mail.rule_list`，它同时回显该条当前的条件/动作简化式，照抄回填再改。
- `name`/`conditions`/`actions` 每次都要给全（整条替换）。

## Tips

- 省略 `match_all`/`is_enable`/`ignore_the_rest_of_rules` 会落回默认值，静默改掉这条原有的组合方式。
- 非法简化式在 `dry_run` 之前就编译报错；`dry_run` 回显的是编译后的规则结构，不是你的原文。
- 同一份内容重复提交结果一致，可安全重放。
