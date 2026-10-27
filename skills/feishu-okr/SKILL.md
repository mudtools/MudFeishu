---
name: feishu-okr
description: 飞书 okr 域工具集：18 个工具（只读 11 / 写 7）。目标 / 关键结果 / 进展 → `okr.*`；任务待办 → `task.*`；审批待办 → `approval.*`。
version: 1.0.0
package_version: 3.1.1
---

# OKR（okr.*）

## 路由优先级（先判断对象属于哪个域）
- **目标 / 关键结果 / 进展** → `okr.*`；**任务待办** → `task.*`；**审批待办** → `approval.*`。
  三者都会被称为「目标」或「待办」，判据是对象类型不是措辞。
- 「某某人的 OKR」先 `okr.list_cycles(user_id)` 拿 cycle_id，再往下取 objective / key_result。

## 前置链
`list_cycles` → `list_objectives`（按 cycle_id）→ `list_key_results`（按 objective_id）→
`list_objective_progresses` / `list_key_result_progresses`。
写：`create_objective` / `update_objective` / `create_key_result` / `update_key_result`（删除类为 high-risk）。
指标：`get_objective_indicator` / `get_key_result_indicator` 取 `indicator_id` → `update_indicator`（平台没有按 id 反查的接口）。

## 避坑
- **层级顺序不能跳**：`list_objectives` 需要 cycle_id，`list_key_results` 需要 objective_id。
  缺前置时先补上一层，不要直接猜 ID。
- **得分 / 权重是 0~1 的比例**（字符串形式传入，如 `"0.5"`），不是百分数也不是 1~100。
  超出区间会被本地拒绝（平台会静默接受或报语焉不详的错）。
- **正文以纯文本入参**：工具层负责包装为飞书富文本块；不要自行拼 JSON 块结构。
- `list_periods` 是**周期模板**（租户级），与 `list_cycles` 的**用户周期**不是一回事：
  创建 Objective 需要的是 cycle_id。
- `delete_objective` 会**连带删除**其下关键结果，不可恢复。
- **指标与进展是两套面**：`update_indicator` 只动指标（值 / 计算方式 / 单位），文字进展走进展面；
  哪些字段改得动取决于实体类型（Objective 不改起始值、目标值与单位），故必须先读指标再写。
- 周期 / 目标 / 关键结果语义与计分口径 → `feishu.guidance_read(okr, model-and-scoring)`。

## 安全规则
- 写工具首次调用一律 `dry_run=true` 预演；确认 method/path 与字段摘要无误后，以 `dry_run=false` 重放**同一参数**。
- high-risk-write 工具须宿主注册 `IToolExecutionAuthorizer` 才会放行，未注册时一律拒绝。
- 只使用本域真实工具名，不臆造不存在的工具或 API。

## 示例
「这个季度 OKR 进展」→ `list_cycles` → `list_objectives` → `list_objective_progresses`。

## 命令（工具）清单
- `okr.create_key_result`（写）：在指定目标（Objective）下创建一个关键结果（Key Result）（content 为纯文本，工具层负责包装为飞书富文本块）。写操作：需 okr:okr.content:writeonly。
- `okr.create_objective`（写）：在指定 OKR 周期（Cycle）下创建一个目标（Objective）（content 为纯文本，工具层负责包装为飞书富文本块）。写操作：需 okr:okr.content:writeonly。
- `okr.delete_key_result`（写）：删除指定关键结果（Key Result）——不可恢复。高危写（high-risk-write）：需 okr:okr.content:writeonly。建议先 dry_run 预演确认。
- `okr.delete_objective`（写）：删除指定目标（Objective），并连带删除其下的关键结果——不可恢复。高危写（high-risk-write）：需 okr:okr.content:writeonly。建议先 dry_run 预演确认。
- `okr.get_key_result`（只读）：获取单个关键结果（Key Result）的详情（内容、归属者、序号、得分、权重、截止时间）。只读，需 okr:okr.content:readonly。
- `okr.get_key_result_indicator`（只读）：获取某个关键结果（Key Result）的指标信息（indicator_id、状态与状态计算方式、起始值/目标值/当前值及其计算方式、单位）。只读，需 okr:okr.content:readonly。返回的 indicator_id 是 okr.update_indicator 的唯一入口。
- `okr.get_objective`（只读）：获取单个目标（Objective）的详情（富文本内容、备注、归属者、得分、权重、截止时间、分类）。只读，需 okr:okr.content:readonly。
- `okr.get_objective_indicator`（只读）：获取某个目标（Objective）的指标信息（indicator_id、状态与状态计算方式、起始值/目标值/当前值及其计算方式、单位）。只读，需 okr:okr.content:readonly。返回的 indicator_id 是 okr.update_indicator 的唯一入口（平台没有『按 id 反查指标』的接口，改之前必须先在这里定位）。
- `okr.list_categories`（只读）：列出系统中的全部 OKR 分类（id、多语言名称、颜色、类型、启用状态），可翻页。只读，需 okr:okr.setting:read。category_id 是 okr.create_objective / okr.update_objective 的可选输入。
- `okr.list_cycles`（只读）：列出指定用户的 OKR 周期（Cycle）列表（含 id、周期状态、起止时间、得分），可翻页。只读，需 okr:okr.period:readonly。返回的 cycle_id 是 okr.list_objectives 的前置输入。
- `okr.list_key_result_progresses`（只读）：列出某个关键结果（Key Result）下的进展记录列表（含进展内容、完成度、归属者、创建/更新时间），可翻页。只读，需 okr:okr.progress:readonly。
- `okr.list_key_results`（只读）：列出某个目标（Objective）下的关键结果（Key Result）列表（含 id、内容、序号、得分、权重、截止时间），可翻页。只读，需 okr:okr.content:readonly。
- `okr.list_objective_progresses`（只读）：列出某个目标（Objective）下的进展记录列表（含进展内容、完成度、归属者、创建/更新时间），可翻页。只读，需 okr:okr.progress:readonly。
- `okr.list_objectives`（只读）：列出某个 OKR 周期（Cycle）下的目标（Objective）列表（含 id、富文本内容、归属者、序号、得分、权重、截止时间），可翻页。只读，需 okr:okr.content:readonly。cycle_id 来自 okr.list_cycles。
- `okr.list_periods`（只读）：列出当前租户下的 OKR 周期（Period）定义（id、中英文名称、启用状态、起止时间），可翻页。只读，需 okr:okr:readonly。period 是周期模板元数据，与用户 Cycle 不同（create 时需要的是 cycle_id）。
- `okr.update_indicator`（写）：更新一个 OKR 指标（Indicator）的取值与计算方式（至少传一个要更新的字段）。写操作：需 okr:okr.content:writeonly。⚠️ 先用 okr.get_objective_indicator / okr.get_key_result_indicator 取 indicator_id 并读当前配置：Objective 不支持改起始值、目标值与单位；处于自动计算方式的当前值改不动… 〔需 okr:okr.content:writeonly〕
- `okr.update_key_result`（写）：修改指定关键结果（Key Result）的内容、得分或截止时间（至少传一个要更新的字段）。写操作：需 okr:okr.content:writeonly。
- `okr.update_objective`（写）：修改指定目标（Objective）的字段（content/notes/deadline/score/category_id，至少传一个要更新的字段）。写操作：需 okr:okr.content:writeonly。

## 参考文档（细则）
- `references/model-and-scoring.md`：`okr/model-and-scoring`
- 正文里形如 `feishu.guidance_read(okr, 主题)` 的指针，在文件形态下读本包的 `references/主题.md`（本包细则就是那份资产，无需进程内工具）
