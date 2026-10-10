---
name: feishu-security
description: 飞书 security 域工具集：1 个工具（只读 1 / 写 0）。"谁动了这份文档 / 谁的权限被改了 / 谁删了什么 / 某人的异常操作" → `security.*`；
version: 1.0.0
---

# 安全与合规 · 行为审计日志（security.*）

## 路由优先级（先判断对象属于哪个域）
- **"谁动了这份文档 / 谁的权限被改了 / 谁删了什么 / 某人的异常操作"** → `security.*`；
  **文档内容本身**用 `docx.*`，**文件与协作者**用 `drive.*`，**登录设备与门禁**属其它域（未策展时先用
  `feishu.capability_lookup` 确认「是没有还是没启用」，不要臆造调用）。
- 需要**通讯录详情 / 把 open_id 变成姓名** → 用 `contact.*` 补全，不要在审计结果里反推身份。

## 前置链
（无条件）`security.query_audit_logs` 直接查（默认最近 30 天；建议把 `user_id_type` 传 `open_id`，
便于与 `contact.*` / `im.*` 的 ID 口径衔接）→ 命中后用 `event_name` 收敛继续翻页。
（知道是谁）先 `contact.search_user` 拿 `open_id` → `operator_type=user` + `operator_value=<open_id>` 精确查。
（只知道"哪个文档"）先用 `drive.*` / `docx.*` 拿到对象标识，再用 `object_type`/`object_value` 收敛。

## PII 与合规边界（本域是 PII 出口）
- 本工具返回**他人**的成员 ID、IP、城市与设备信息，属 **PII**：**默认不启用**，必须由宿主显式加入
  `FeishuAgent:Tools` 白名单——用户问起时如实说明"该能力需管理员开通"，不要改用其它域工具拼凑答案。
- 结果会进入**第三方模型上下文**：只回答与排查目标直接相关的事实，**不要**逐条复述无关人员的 IP/设备明细。
- 只读查询用于合规排查；**不得**用于对个人的绩效评价、监控或画像。

## 避坑
- **时间窗 ≤ 30 天**：起止相差超过 30 天会被本地拒绝（不消耗一次下游调用）——请按 30 天分段查。
- **时间戳是秒级整数**：不是 ISO 字符串；不确定"现在是多少秒"时省略 `latest`（默认当前时刻）。
- **`operator_value` 必须与 `operator_type` 成对**：只给值不给类型会被本地拒绝。
- **大概率查不到不是故障**：默认只包含平台支持的审计事件；先放宽筛选（去掉 event_name/operator）确认
  该时间窗是否有数据，再逐步收敛条件。
- **翻页**：`has_more=true` 时用返回的 `page_token` 续查；不要把 page_size 调到 200 只为"少翻几页"——
  结果越大会越早触发截断。

## 安全规则
- 本域为**只读**（无写操作）；需 `admin:audit_info:readonly`。
- 审计日志是**合规证据**：只做查询与陈述，**不得**基于结果做出处罚性结论或对外披露个人信息。
- 只使用本域真实工具名（`security.query_audit_logs`），不臆造不存在的工具或 API。

## 示例
- 「上周三谁改了这个文档的权限？」→ `security.query_audit_logs(event_name="permission_change", oldest=<上周三0点秒>, latest=<上周四0点秒>, user_id_type="open_id")`
  → 从 `objects[].object_name` 里挑出该文档 → 如需姓名，用 `contact.batch_get` 补全。
- 「张三最近有没有导出过数据？」→ `contact.search_user` 拿 `open_id` → `security.query_audit_logs(operator_type="user", operator_value=<open_id>, oldest=<7天前秒>)`
  → 按 `event_name` 汇总。
- 「这个月有人删过群吗？」→ 时间窗 30 天分段 + `event_module` 收敛 → 结果里按 `event_name` 归类回答。

## 命令（工具）清单
- `security.query_audit_logs`（只读）：查询企业成员的【行为审计日志】——谁在什么时间、从哪个 IP/城市、对哪个对象（文档/会话/权限/应用）做了什么（事件名如 space_edit_doc、permission_change）。用于合规排查与异常操作复盘。⚠️ 该工具属 PII：结果含成员 user_id、IP 与设备/地理位置，会进入【第三方模型上下文】；【默认不启用】，需宿主显式加入白名单并确认合规口径。时间范围起止相差不能超过 30 天；建议 user_id_type 传 open_id（否则需字段权限 …
