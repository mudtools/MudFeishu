## WHEN

- 已经拿到要挂的 OKR 周期 ID，需要在它下面立一个新目标。
- 目标正文就是一句纯文本，由工具层包装成块，不用自己拼富文本结构。

## Avoid when

- 要写的是目标下面的关键结果——那是 `okr.create_key_result`。
- 改已有目标的文字、备注或得分——用 `okr.update_objective`。
- 还不确定有哪些周期——先 `okr.list_cycles` 取 `cycle_id`，本工具不建周期。

## Prerequisites

- `cycle_id` 与 `content` 必填；前者只能来自 `okr.list_cycles`。
- `score` 与 `weight` 取 0~1 的字符串小数，越界或非数值在本地就被拒绝并回显实际取值。

## Tips

- 回执给的是新建目标的 ID，它是后续挂关键结果与改字段的入口，当场留下。
- 参数面没有幂等项：同一次创建重发一遍就再多出一个目标，落笔前用 `dry_run` 核对字段摘要。
- `category_id` 取自 `okr.list_categories`，与周期 ID 不是一回事，别互填。
