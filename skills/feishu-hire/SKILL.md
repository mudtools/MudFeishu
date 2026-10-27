---
name: feishu-hire
description: 飞书 hire 域工具集：7 个工具（只读 7 / 写 0）。职位（对外岗位）→ `hire.list_jobs`、`hire.get_job`（非 PII）。
version: 1.0.0
package_version: 3.0.3
---

# 招聘（hire.*：职位 / 需求 / 候选人 / 面试 / Offer）

## 路由优先级（先判断对象属于哪个域）
- **职位**（对外岗位）→ `hire.list_jobs`、`hire.get_job`（非 PII）。
- **招聘需求**（内部用人计划：人数/职级区间/时间）→ `hire.list_job_requirements`（非 PII）。需求 ≠ 职位。
- **候选人**（联系方式）→ `hire.list_talents`、`hire.get_talent`（**PII，默认不启用**）。
- **面试安排**（何时/哪轮/什么状态）→ `hire.list_interviews`（**PII，默认不启用**）；评价正文不返回。
- **Offer 状态**（审批中/已发送/已接受/被拒）→ `hire.list_offers`（**PII，默认不启用**）；薪资条件不返回。
- **已入职员工/部门** → `contact.*`；更深的投递进展先 `feishu.capability_lookup` 确认。

## 前置链
- 职位：`hire.list_jobs` 翻页 →（看正文）`hire.get_job`；需求：`hire.list_job_requirements`（可带 `job_id`）。
- 候选人：`hire.list_talents`（可用 `keyword`）→ `hire.get_talent(talent_id=...)` 取手机/邮箱。
- 面试：`hire.list_interviews`（可带 `application_id`；`start_time`/`end_time` 毫秒时间戳限定时间窗）。
- Offer：`hire.list_offers(talent_id=...)` → 看 `offer_status`：2 审批中 / 4 审批通过 / 5 不通过 /
  6 已发送 / 7 被接受 / 8 被拒 / 9 已失效 / 10 已创建。

## 降级策略
- `found=false`：核对 ID 来源；列表为空 → 放宽过滤（去掉 job_id/keyword/时间窗）重试一次。
- 报「未注册客户端（软缺席）」→ 宿主未启用对应招聘 API（五类各自独立），如实告知缺哪类。
- 报「工具不在白名单」→ 该工具属 PII、**默认不启用**，请宿主加白名单。
- `truncated=true` → 只取相关片段，不要要求"全部列出"。

## 避坑
- **列表不含正文/联系方式/评价/金额**：职位列表无 JD、候选人列表无手机邮箱、面试列表无评价、Offer 列表无薪资条件；
  需要正文用对应 `get`（面试评价与 Offer 条件本域不提供）。
- **候选人/面试/Offer 数据不得扩散**：只给上述最小字段——身份证号/学历/经历/评价正文/薪资条件一律不返回，不得从其它字段拼凑。
- **岗位 ≠ 人**：职位表里没有候选人；不要回答"谁投了这个岗位"。
- **页大小不可调**：`list_*` 只接受 `page_token`；`job_id`/`keyword`/`application_id`/`talent_id`/时间窗是业务过滤。
- **停用岗位仍在列表里**：`active_status` 表示启用状态，回答"在招岗位"时按状态筛选。

## 安全规则
- **非 PII**：职位、招聘需求。**PII（默认不启用）**：候选人、面试、Offer。
- 只用真实工具名：`hire.list_jobs` / `hire.get_job` / `hire.list_job_requirements` / `hire.list_talents` /
  `hire.get_talent` / `hire.list_interviews` / `hire.list_offers`；不臆造工具或 API。
- PII 信息仅在授权会话内使用：不转存、不跨会话扩散、不与无关人员共享。

## 示例
- 「有哪些在招岗位 / 还缺几个人？」→ `hire.list_jobs()` / `hire.list_job_requirements()`。
- 「帮我找张伟的联系方式」→ `hire.list_talents(keyword="张伟")` → `hire.get_talent(talent_id=...)`。
- 「张三面到哪一轮 / Offer 到哪一步？」→ `hire.list_interviews(application_id=...)` / `hire.list_offers(talent_id=...)`。

## 命令（工具）清单
- `hire.get_job`（只读）：按职位 ID 查询单个【招聘岗位】详情（标题、编码、岗位描述、任职要求、启用状态、创建/更新时间）——已从 hire.list_jobs 拿到职位 ID 时用它取完整正文（列表不含正文）。描述可能很长（按结果预算截断并标记 truncated）。查不到时返回 found=false。只读，需 hire:job:readonly。
- `hire.get_talent`（只读）：按人才 ID 查询单个【候选人】的联系与在职字段（姓名、手机号、邮箱、工作年限、是否已入职）——已从 hire.list_talents 拿到 talent_id 时用它取联系方式。⚠️ 该工具属 PII：结果是候选人个人信息，会进入【第三方模型上下文】；【默认不启用】，需宿主显式加入白名单并确认合规口径；身份证号/学历/工作经历/备注等一律不返回。查不到时返回 found=false。只读，需 hire:talent:readonly。
- `hire.list_interviews`（只读）：分页查询【面试安排】（面试 ID、投递 ID、轮次 round 与 round_type 名称、阶段 stage、开始/结束时间（毫秒时间戳）、安排状态）——用于回答「今天/本周有哪些面试」「某投递面到哪一轮」。可用 application_id 只查某投递，用 start_time/end_time（毫秒时间戳字符串）限定时间窗。⚠️ 该工具属 PII：面试安排即「某人正在应聘」的事实，会进入【第三方模型上下文】… 〔需 hire:interview:readonly〕
- `hire.list_job_requirements`（只读）：分页列出【招聘需求】（需求 ID、编码、名称、需求人数 head_count、优先级、进度、部门、职级区间、起止时间、审批状态、关联职位 ID 列表），可用 job_id 只看某职位挂的需求。用于回答「这个季度还缺几个人」「某职位对应哪条招聘需求」。⚠️ 为最小暴露：招聘负责人/用人经理/直属上级、创建人、薪资区间一律不返回（需这些信息请在招聘系统查看）。page_token 续页。只读，需 hire:job_requirement:readonly。
- `hire.list_jobs`（只读）：分页列出【招聘岗位】（职位 ID、标题、编码、启用状态、更新时间）——用于回答「我们有哪些在招岗位」「某岗位 ID 是什么」。列表不含岗位描述正文（较长），需要正文请用 hire.get_job 按 ID 单条获取。page_token 续页（页大小由宿主固定）。只读，需 hire:job:readonly。
- `hire.list_offers`（只读）：按人才 ID 查询其【Offer 列表】（Offer ID、投递 ID、岗位 ID/名称、Offer 状态、Offer 类型、创建时间）。offer_status 含义：2=审批中 / 3=审批已撤回 / 4=审批通过 / 5=审批不通过 / 6=已发送 / 7=被接受 / 8=被拒绝 / 9=已失效 / 10=已创建。⚠️ 该工具属 PII：某人是否获得 Offer 属个人信息，会进入【第三方模型上下文】；【默认不启用】… 〔需 hire:offer:readonly〕
- `hire.list_talents`（只读）：分页检索【候选人（人才）】列表（人才 ID、姓名、是否已入职），支持 keyword 按姓名/手机号等检索。用于「找人」拿到 talent_id 后再用 hire.get_talent 取联系人字段。⚠️ 该工具属 PII：结果是候选人个人信息，会进入【第三方模型上下文】；【默认不启用】，需宿主显式加入白名单并确认合规口径；身份证号/性别/年龄/籍贯/学历/经历等一律不返回。只读，需 hire:talent:readonly。
