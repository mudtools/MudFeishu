## WHEN

- 指标当前的读数旧了，要把它推到新值。
- 需要改指标本身的口径：计算方式、状态或单位。
- 要把状态从自动切回手动，随后手工给状态赋值。

## Avoid when

- 改的是目标或关键结果的正文与得分——那是 `okr.update_objective` / `okr.update_key_result`。
- 只想看指标现在的配置——用 `okr.get_objective_indicator` 或 `okr.get_key_result_indicator`。

## Prerequisites

- `indicator_id` 只能来自那两个读取工具，不能拿目标或关键结果的 ID 顶替。
- 至少传一个要更新的字段；数值必须落在 -99999999999 ~ 99999999999 之间。

## Tips

- 挂在 Objective 上的指标改不动 `start_value` / `target_value` 与单位。
- `current_value` 处于自动计算方式时改不动；`indicator_status` 只在状态计算方式为手动时可改。
- `unit_type` 与 `unit_value` 必须成对提供，单给一个不构成有效改动。
