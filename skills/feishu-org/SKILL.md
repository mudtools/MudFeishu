---
name: feishu-org
description: 飞书 org 域工具集：8 个工具（只读 8 / 写 0）。拿到的是组织元数据 ID → 本域：`job_title` → 职务；`job_level_id` → 职级；`job_family_id` → 职务族；`city` → 工作城市。
version: 1.0.0
---

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

## 命令（工具）清单
- `org.get_job_family`（只读）：按职务族 ID 查询单个【职务族】详情（名称、说明、父职务族 ID、启用状态）——已从通讯录拿到 job_family_id 时用它精确取值。查不到时返回 found=false。只读，需 contact:user.base:readonly。
- `org.get_job_level`（只读）：按职级 ID 查询单个【职级】详情（名称、说明、排序序号、启用状态）——已从通讯录拿到 job_level_id 时用它精确取值。查不到时返回 found=false。只读，需 contact:user.base:readonly。
- `org.get_job_title`（只读）：按职务 ID 查询单个【职务】详情（名称、多语言名称、启用状态）——已从通讯录拿到 job_title ID 时用它精确取值，不必翻页。ID 形如 od-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx（也可能是数字串）；查不到时返回 found=false。只读，需 contact:user.base:readonly。
- `org.get_work_city`（只读）：按城市 ID 查询单个【工作城市】详情（名称、多语言名称、启用状态）——已从通讯录拿到 city 城市 ID 时用它精确取值。查不到时返回 found=false。只读，需 contact:user.base:readonly。
- `org.list_job_families`（只读）：分页列出/按名称模糊匹配【职务族】目录（职务族 ID、名称、说明、父职务族 ID、启用状态）——用于把通讯录返回的 job_family_id 换成可读名称。职务族有层级：parent_job_family_id 为空者即顶层。name 为可选模糊过滤；page_token 续页。只读，需 contact:user.base:readonly。
- `org.list_job_levels`（只读）：分页列出/按名称模糊匹配【职级】目录（职级 ID、名称、说明、排序序号 order、启用状态）——用于把通讯录返回的 job_level_id 换成可读名称，或确认组织职级序列。name 为可选模糊过滤；page_token 续页（页大小由宿主固定）。只读，需 contact:user.base:readonly。
- `org.list_job_titles`（只读）：分页列出当前租户的【职务】目录（职务 ID、名称、多语言名称、启用状态）——用于把通讯录返回的 job_title 职务 ID 换成可读名称，或确认组织内有哪些职务。page_token 续页（页大小由宿主固定，模型不可调）；结果为空说明租户未配置职务。只读，需 contact:user.base:readonly。
- `org.list_work_cities`（只读）：分页列出【工作城市】目录（城市 ID、名称、多语言名称、启用状态）——用于把通讯录返回的 city 城市 ID 换成可读城市名。注意与 mdm.get_countries（国家/地区参考表）不同：本工具是员工工作城市。page_token 续页（页大小由宿主固定）。只读，需 contact:user.base:readonly。
