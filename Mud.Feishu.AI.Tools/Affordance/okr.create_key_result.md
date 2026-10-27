## WHEN

- 目标已经在册，要为它补一条可度量的关键结果，而不是另起一个目标。
- 用户一次口述了好几条结果时逐条提交，本工具一次只产出一条。

## Avoid when

- 要新建的是目标本身——那是 `okr.create_objective`，它挂在周期下。
- 改已有关键结果的内容、得分或截止时间——用 `okr.update_key_result`。
- 这个目标下已有哪几条还没核对——先 `okr.list_key_results`，别凭印象补。

## Prerequisites

- `objective_id` 来自 `okr.list_objectives`，填成周期 ID 会直接失败。
- `content` 必填纯文本，由工具层包装成块；`score` 是 0~1 的字符串小数。

## Tips

- `deadline` 用毫秒时间戳字符串，与 `okr.update_key_result` 同一口径。
- 参数面没有幂等项：重发一次就多出一条结果，先用 `dry_run` 看字段摘要。
- 回执里的结果 ID 是后续改与删这条结果的入口，当场记下。
