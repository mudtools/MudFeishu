# OKR 模型与计分口径

## 前置链
1. `okr.list_cycles(user_id)` → `cycle_id`（用户周期，含状态与总分）
2. `okr.list_objectives(cycle_id)` → `objective_id`（含 position / score / weight / category_id）
3. `okr.list_key_results(objective_id)` → `key_result_id`
4. 进展：`okr.list_objective_progresses(objective_id)` / `okr.list_key_result_progresses(key_result_id)`

## 避坑
- **两级计分**：Key Result 的 `score` 是自身完成度；Objective 的 `score` 由 KR 得分按 `weight` 加权而来。
  想解释「为什么这个目标是 0.6」，要先取 KR 的 score 与 weight 再算，平台不返回推导过程。
- `weight` 是 **0~1 的比例且保留三位小数**；所有权重之和通常为 1，但工具不做强制校验——
  写入时按用户给的数字如实提交，不要自行归一化。
- `position` 是**展示序号（从 1 开始）**，不是排序操作：调整顺序要用位置 / 权重接口（本批未策展，
  需要时先 `feishu.capability_lookup` 确认能力是否存在）。
- **v1 与 v2 不是同一套模型**：v1 的 `progress_records` 与 v2 的 `progresses` 是两套富文本契约。
  本宿主只策展了 v2 主干 + v1 的周期元数据（`list_periods`）；不要用 v1 的参数形状调 v2 工具。
- `cycle_status`：0 默认 / 1 正常 / 2 失效 / 3 隐藏——「失效 / 隐藏」的周期仍可读，
  但向其中写入通常没有业务意义，遇到时先与用户确认。
- `deadline` 是**毫秒时间戳字符串**（不是 RFC3339），与日历域的时间口径不同。

## 示例
- 「这个目标为什么没达标」→ `list_key_results` 取各 KR 的 score/weight → 说明加权结果
- 「把目标内容改成…」→ `update_objective(content=…)`，先用 `dry_run=true` 确认