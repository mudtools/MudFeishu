
# 组织元数据 · 职务目录（org.*）

## 路由优先级（先判断对象属于哪个域）
- **拿到 `job_title` 职务 ID（不是名称）** → `org.*`；**用户本身的姓名/部门/邮箱/手机**在 `contact.*`；
  **部门树**在 `contact.list_departments`；**地区编码**在 `mdm.get_countries`。没有对应域时先用
  `feishu.capability_lookup` 确认「是不存在还是没启用」，不要臆造调用。
- 「**谁**是什么职务」属于个人信息，本域**不提供**——先 `contact.search_user` 拿用户对象，
  再对其中 `job_title` 字段用 `org.get_job_title` 换名称。

## 前置链
（已知职务 ID）`org.get_job_title` 精确取值 → 用返回的 `name`（或 `i18n_name` 里对应语言）回答。
（不知职务 ID）`org.list_job_titles` 翻页取目录 → 按 `name` 匹配；`has_more=true` 时用 `page_token` 续页。
（要回答"某人是什么职务"）`contact.search_user` → 取 `job_title` → `org.get_job_title` → 用职务名称回答。

## 降级策略
- `found=false`：① 详情查不到 → 核对 ID 来源（应取自通讯录 `job_title` 字段），或先列目录核对；
  ② 列表为空 → 租户可能未配置职务，如实说明，不要反复换 token 重试。
- 工具报"未注册客户端（软缺席）"→ 该能力需管理员开通通讯录职务 API；如实告知，不要改用其它域近似作答。

## 避坑
- **`job_title` 是 ID 不是名称**：直接把它当"职务名称"念给用户是错的——必须先经本域换取名称。
- **页大小不可调**：本域只接受 `page_token`（页大小由宿主固定）；要"一次拿全"就按 `has_more` 续页，
  不要试图用参数放大页容量。
- **职务是组织定义、不是人**：列表里没有员工信息；不要从职务目录推断任何人的任职情况。
- **停用职务仍在目录里**：`status=false` 表示已停用，历史数据里仍可能出现它的 ID，不要因为停用就否认该 ID 有效。

## 安全规则
- 本域为**只读**（无写操作）、属**非 PII** 参考数据；需通讯录用户基础信息只读权限（与 `contact.*` 同一授权面，宿主一次授权即可用齐）。
- 只使用本域真实工具名（`org.list_job_titles` / `org.get_job_title`），不臆造不存在的工具或 API。
- 不得把职务名称与具体个人绑定后对外披露（"某人的职务"属个人信息，仅在用户授权的会话内使用）。

## 示例
- 「他是做什么的？」→ `contact.search_user`（拿用户对象）→ `org.get_job_title(job_title_id=<用户对象的 job_title>)` → 用 `name` 回答。
- 「我们公司有哪些职务？」→ `org.list_job_titles()` → 汇总 `name`；`has_more=true` 时用 `page_token` 续页。
- 用户给了 `od-xxxx` 形式的字符串 → 先判断它来自哪个字段；若是 `job_title` 就用本域，若是部门 ID 用 `contact.list_departments`。
