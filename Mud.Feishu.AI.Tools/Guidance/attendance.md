
# 考勤（attendance.*）

## 路由优先级（先判断对象属于哪个域）
- **查打卡记录**（"我昨天几点打的卡"）→ `attendance.query_my_flow`（user 身份，只能查本人）；
  查**他人** → `attendance.query_user_flow`（默认不启用，见下）。
- **查排班**（"我今天几点上班"）→ `attendance.query_daily_shift`；再要班次时间规则 → `attendance.get_shift`。
- **查统计报表**（出勤率 / 迟到次数）→ `attendance.query_stats_data`。
- **补卡**（"忘了打卡，帮我补"）→ 先 `attendance.query_my_remedys` 看已有申请，再 `attendance.submit_remedy`。

## 前置链
`query_daily_shift` 给出 `shift_id` → `get_shift` 给班次上下班时间；补卡链路：
`query_my_remedys`（确认是否已提交）→ `submit_remedy`（发起审批）→ 结果里带 `approval_id` 供用户跟进。

## 日期与时间口径（最容易错的一环）
- `check_date_from` / `check_date_to` / `start_date` / `end_date` / `remedy_date` 是 **yyyyMMdd 整数**（如 20240101），**不是** Unix 时间戳。
- `check_time_from` / `check_time_to` 是**秒级时间戳字符串**（如 `"1705312800"`）。
- `remedy_time` 是 **HH:mm** 字符串（如 `"09:00"`）。
口径传错时飞书返回"无数据"而非报错——看起来像"没有记录"，实际是参数形态错。核对后再解释结果。

## 避坑
- **`submit_remedy` 非幂等**：重复提交会产生重复申请。提交前先用 `query_my_remedys` 确认，
  提交后再查一次确认写入，**不要**因为"没看到返回"就重试。
- **查他人考勤是高敏操作**：`attendance.query_user_flow` / `attendance.get_flow` 默认不启用，
  调用前确认用户已获授权；拿不到结果时如实说明"该工具未启用"（用 `feishu.tool_search` 可确认是否存在）。
- **user 身份工具**（`attendance.query_my_flow`）要求宿主放行 `user` 身份并提供当前用户；
  上下文缺用户时工具会明确报错，此时应向用户索取 ID 或改由管理员用 tenant 工具查询。

## 安全规则
- 本域 7 个只读 + 1 个写（`attendance.submit_remedy`，需 `WriteAllowList` + 授权门禁，可用 `dry_run=true` 预演）。
- 考勤数据属员工个人信息：结果会进入模型上下文——用 `feishu.guidance_read` 读考勤域的 PII 主题
  （键 `attendance/pii`）可获取数据边界与宿主侧二次脱敏建议。

## 示例
「我今天几点上班」→ `attendance.query_daily_shift`（今天 yyyyMMdd）→ 有 `shift_id` 则 `attendance.get_shift`。
