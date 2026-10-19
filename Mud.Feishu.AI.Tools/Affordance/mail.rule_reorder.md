## WHEN

- 规则内容一个字都不改，只要改变谁先被匹配（顺序即优先级）。

## Avoid when

- 条件或动作要变——用 `mail.rule_update`，它不动次序。
- 有一条不该再判——用 `mail.rule_delete`，删掉后剩余规则自动前移，不必重排。
- 要让刚建的那条抢在前面——先 `mail.rule_create` 再本工具，`rule_ids` 不能引入新规则。

## Prerequisites

- `rule_ids` 是该邮箱**全量**规则 ID 的完整序列，逐个来自 `mail.rule_list`，数组第一项最先匹配。

## Tips

- 整表覆盖：抄漏一个 ID，它就不在你给出的序列里，重排前后各调一次 `mail.rule_list` 对齐。
- 回执只有 reordered 与条数，不反映新顺序，别把它当校验结果。
- 重排不改条件与动作，排错了可以再排回来。
