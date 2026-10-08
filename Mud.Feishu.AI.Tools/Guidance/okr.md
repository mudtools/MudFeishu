
# OKR（okr.*）

## 路由优先级（先判断对象属于哪个域）
- **目标 / 关键结果 / 进展** → `okr.*`；**任务待办** → `task.*`；**审批待办** → `approval.*`。
  三者都会被称为「目标」或「待办」，判据是对象类型不是措辞。
- 「某某人的 OKR」先 `okr.list_cycles(user_id)` 拿 cycle_id，再往下取 objective / key_result。

## 前置链
`list_cycles` → `list_objectives`（按 cycle_id）→ `list_key_results`（按 objective_id）→
`list_objective_progresses` / `list_key_result_progresses`。
写：`create_objective` / `update_objective` / `create_key_result` / `update_key_result`（删除类为 high-risk）。

## 避坑
- **层级顺序不能跳**：`list_objectives` 需要 cycle_id，`list_key_results` 需要 objective_id。
  缺前置时先补上一层，不要直接猜 ID。
- **得分 / 权重是 0~1 的比例**（字符串形式传入，如 `"0.5"`），不是百分数也不是 1~100。
  超出区间会被本地拒绝（平台会静默接受或报语焉不详的错）。
- **正文以纯文本入参**：工具层负责包装为飞书富文本块；不要自行拼 JSON 块结构。
- `list_periods` 是**周期模板**（租户级），与 `list_cycles` 的**用户周期**不是一回事：
  创建 Objective 需要的是 cycle_id。
- `delete_objective` 会**连带删除**其下关键结果，不可恢复。
- 周期 / 目标 / 关键结果语义与计分口径 → `feishu.guidance_read(okr, model-and-scoring)`。

## 安全规则
- 写工具首次调用一律 `dry_run=true` 预演；确认 method/path 与字段摘要无误后，以 `dry_run=false` 重放**同一参数**。
- high-risk-write 工具须宿主注册 `IToolExecutionAuthorizer` 才会放行，未注册时一律拒绝。
- 只使用本域真实工具名，不臆造不存在的工具或 API。

## 示例
「这个季度 OKR 进展」→ `list_cycles` → `list_objectives` → `list_objective_progresses`。
