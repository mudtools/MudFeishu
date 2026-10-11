---
name: feishu-hire
description: 飞书 hire 域工具集：5 个工具（只读 5 / 写 0）。岗位/职位（标题、编码、JD、要求）→ `hire.list_jobs` / `hire.get_job`（非 PII）。
version: 1.0.0
---

# 招聘（hire.*：职位 / 候选人）

## 路由优先级（先判断对象属于哪个域）
- **岗位/职位**（标题、编码、JD、要求）→ `hire.list_jobs` / `hire.get_job`（非 PII）。
- **招聘需求**（用人计划：需求人数、职级区间、部门、起止时间）→ `hire.list_job_requirements`（非 PII）。
  需求与职位是**两套对象**：「还缺几个人」「这职位挂在哪个需求下」问需求，「岗位要求是什么」问职位。
- **候选人**（人才档案、联系方式）→ `hire.list_talents` / `hire.get_talent`（**PII，默认不启用**）。
- **在职业员工**在 `contact.*`（已入职的人不是候选人）；**部门**在 `contact.*`；面试/Offer/投递未开放，
  不要臆造工具，可先用 `feishu.capability_lookup` 确认。

## 前置链
（不知职位 ID）`hire.list_jobs` 翻页 → 按 `title`/`code` 匹配取 `id`；要看 JD 正文再 `hire.get_job`。
（找候选人）`hire.list_talents`（可用 `keyword`）→ 取 `talent_id` → `hire.get_talent` 取手机/邮箱等联系字段。
（回答"我们有哪些在招岗位"）`hire.list_jobs()` → 汇总 `title`（列表不含正文，不要为了看正文逐条 get）。

## 降级策略
- `found=false`：详情 → 核对 ID 来源与列表返回一致；列表为空 → 换更短关键字，或省略过滤重翻一页。
- 报"未注册客户端（软缺席）"→ 宿主未启用对应招聘 API（岗位 / 候选人各自独立）；如实告知缺哪一类。
- 报"工具不在白名单"或不可见 → 该工具**默认不启用**（PII）：请宿主按需显式加入白名单，不要改用其它域近似作答。
- 结果 `truncated=true` → 正文较长：只取与问题相关的段落，不要要求"全部列出"。

## 避坑
- **列表不含正文/联系方式**：`hire.list_jobs` 无 JD，`hire.list_talents` 无手机邮箱——需要就用对应的 `get` 单条取。
- **候选人数据不得扩散**：`hire.get_talent` 只返回联系所需字段（姓名/手机/邮箱/工作年限/是否已入职）；
  身份证号、性别、年龄、籍贯、学历、工作经历、简历附件**本域不返回**，不要试图从其它字段拼凑。
- **岗位 vs 人**：职位表里没有候选人；不要回答"谁投了这个岗位"（投递数据未开放）。
- **需求里没有负责人与薪资**：`hire.list_job_requirements` 不返回招聘负责人/用人经理/创建人/薪资区间（与"定位需求"目的无关），
  不要试图从其它字段拼凑；这些信息请在招聘系统查看。
- **页大小不可调**：`list_*` 只接受 `page_token`（页大小由宿主固定）；`keyword` / `job_id` 是业务过滤，与页大小不是一类东西。
- **停用岗位仍在列表里**：`active_status` 表示启用状态；回答"在招岗位"时应按状态筛选。

## 安全规则
- 职位类工具为**只读、非 PII**；候选人工具为**只读但属 PII**（默认不启用，需宿主显式授权并确认合规口径）。
- 只使用本域真实工具名（`hire.list_jobs` / `hire.get_job` / `hire.list_talents` / `hire.get_talent`），不臆造工具或 API。
- 候选人信息仅在用户授权的会话内使用：不得转存、不得跨会话扩散、不得与无关人员共享。

## 示例
- 「我们有哪些在招岗位？」→ `hire.list_jobs()` → 汇总 `title`；`has_more=true` 时续页。
- 「这个岗位的任职要求是什么？」→ `hire.list_jobs`（拿 id）→ `hire.get_job(job_id=...)` → 取 `requirement`。
- 「帮我找一下张伟的联系方式」→ `hire.list_talents(keyword="张伟")` → `hire.get_talent(talent_id=...)` → 取 `mobile`/`email`。
- 用户问「张三投了哪个岗位」→ 本域**不支持**（投递数据未开放）：如实说明并建议走招聘系统的投递管理。

## 命令（工具）清单
- `hire.get_job`（只读）：按职位 ID 查询单个【招聘岗位】详情（标题、编码、岗位描述、任职要求、启用状态、创建/更新时间）——已从 hire.list_jobs 拿到职位 ID 时用它取完整正文（列表不含正文）。描述可能很长（按结果预算截断并标记 truncated）。查不到时返回 found=false。只读，需 hire:job:readonly。
- `hire.get_talent`（只读）：按人才 ID 查询单个【候选人】的联系与在职字段（姓名、手机号、邮箱、工作年限、是否已入职）——已从 hire.list_talents 拿到 talent_id 时用它取联系方式。⚠️ 该工具属 PII：结果是候选人个人信息，会进入【第三方模型上下文】；【默认不启用】，需宿主显式加入白名单并确认合规口径；身份证号/学历/工作经历/备注等一律不返回。查不到时返回 found=false。只读，需 hire:talent:readonly。
- `hire.list_job_requirements`（只读）：分页列出【招聘需求】（需求 ID、编码、名称、需求人数 head_count、优先级、进度、部门、职级区间、起止时间、审批状态、关联职位 ID 列表），可用 job_id 只看某职位挂的需求。用于回答「这个季度还缺几个人」「某职位对应哪条招聘需求」。⚠️ 为最小暴露：招聘负责人/用人经理/直属上级、创建人、薪资区间一律不返回（需这些信息请在招聘系统查看）。page_token 续页。只读，需 hire:job_requirement:readonly。
- `hire.list_jobs`（只读）：分页列出【招聘岗位】（职位 ID、标题、编码、启用状态、更新时间）——用于回答「我们有哪些在招岗位」「某岗位 ID 是什么」。列表不含岗位描述正文（较长），需要正文请用 hire.get_job 按 ID 单条获取。page_token 续页（页大小由宿主固定）。只读，需 hire:job:readonly。
- `hire.list_talents`（只读）：分页检索【候选人（人才）】列表（人才 ID、姓名、是否已入职），支持 keyword 按姓名/手机号等检索。用于「找人」拿到 talent_id 后再用 hire.get_talent 取联系人字段。⚠️ 该工具属 PII：结果是候选人个人信息，会进入【第三方模型上下文】；【默认不启用】，需宿主显式加入白名单并确认合规口径；身份证号/性别/年龄/籍贯/学历/经历等一律不返回。只读，需 hire:talent:readonly。
