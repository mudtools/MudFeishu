
# 组织元数据（org.*：职务 / 职级 / 职务族 / 工作城市）

## 路由优先级（先判断对象属于哪个域）
- 拿到的是**组织元数据 ID** → 本域：`job_title` → 职务；`job_level_id` → 职级；`job_family_id` → 职务族；`city` → 工作城市。
- `contact.*` 管人（姓名/部门/邮箱/手机）；部门树用 `contact.list_departments`；**国家/地区编码**（CN、区号）用 `mdm.get_countries`，不要拿工作城市替代。
- 「**谁**是什么职务/职级」属个人信息：本域不提供，先 `contact.search_user` 拿用户对象再换名称。
- 没有对应域时先用 `feishu.capability_lookup` 确认「不存在还是没启用」，不要臆造调用。

## 前置链
- 已知 ID → 对应 `get_*` 精确取值；不知 ID → 对应 `list_*` 翻页（`has_more=true` 用 `page_token` 续页）。
- 职级 / 职务族支持 `name` 模糊过滤，比翻页更快命中。
- 「某人是什么职级」：`contact.search_user` → 取 `job_level_id` → `org.get_job_level` → 用 `name` 回答。

## 降级策略
- `found=false`：详情查不到 → 核对 ID 来源（应取自用户对象对应字段）或先列目录核对；列表为空 → 可能未配置，或 `name` 过滤过窄（去掉 name 重试一次）。
- 报「未注册客户端（软缺席）」→ 说明该客户端对应的元数据未启用（四类各自独立）；如实告知缺哪一类，不要改用其它域近似作答。

## 避坑
- 这些字段都是 **ID 不是名称**：直接念给用户是错的，必须先换名称。
- 职务族有层级（`parent_job_family_id`，为空即顶层），不得臆造层级。
- 职级 `order` 只是组织内排序序号，不得解读为薪酬等级等未声明含义。
- 工作城市 ≠ 国家/地区（见路由）。
- `list_*` 只接受 `page_token`（页大小由宿主固定）；`name` 是业务过滤，与页大小不是一类东西；要拿全就按 `has_more` 续页。
- `status=false` 表示已停用，历史数据里该 ID 仍有效，不要否认它。
- 本域是组织定义、不含员工信息：不得据此推断任何人的任职情况。

## 安全规则
- 只读、非 PII；需通讯录用户基础信息只读权限（与 `contact.*` 同一授权面）。
- 只用真实工具名：`org.list_job_titles` / `org.get_job_title` / `org.list_job_levels` / `org.get_job_level` / `org.list_job_families` / `org.get_job_family` / `org.list_work_cities` / `org.get_work_city`；不臆造工具或 API。
- 不得把职务/职级名称与具体个人绑定后对外披露。

## 示例
- 「他是做什么的？」→ `contact.search_user` → `org.get_job_title`（用用户对象的 `job_title`）→ 答 `name`。
- 「我们有哪些职级？」→ `org.list_job_levels()` → 按 `order` 汇总；找特定职级加 `name`。
- 「他在哪个城市办公？」→ `contact.search_user` → `org.get_work_city`（用 `city`）。
