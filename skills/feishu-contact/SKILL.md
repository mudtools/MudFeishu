---
name: feishu-contact
description: 飞书 contact 域工具集：6 个工具（只读 6 / 写 0）。把「人」换成 open_id / 查部门 → `contact.*`；拿到 open_id 之后的动作（发消息、建任务、发邮件）
version: 1.0.0
---

# 通讯录（contact.*）

## 路由优先级（先判断对象属于哪个域）
- **把「人」换成 open_id / 查部门** → `contact.*`；拿到 open_id 之后的动作（发消息、建任务、发邮件）
  属各业务域。通讯录域只解决「对象是谁」，不承载业务动作。
- 「按名字找」用 `contact.search_user`；「按已知 ID 取详情」用 `contact.get_user` / `batch_get`。

## 前置链
`resolve_user` / `search_user` → open_id → im / mail / task 等写工具；部门轴 `list_departments` → `list_department_members`。

## 避坑
- **发给张三不是一步**，必须先解析 open_id。
- 解析不到先确认**可见范围**（通讯录权限与部门范围），别反复换关键字重试。
- **同名 / 重名**时列多个候选让用户确认，不要自己挑一个。
- `batch_get` 的 ID 列表有上限；超量要分批。
- 邮箱 / 手机号属敏感字段，只有应用开通对应字段权限时才返回。

## 安全规则
- 本域全为只读工具（无写操作、无 `dry_run` 语义）；调用受 scope 审计约束。
- 只使用本域真实工具名，不臆造不存在的工具或 API。
- 不把通讯录返回的个人信息扩散到无关输出里（按需取字段）。

## 示例
发给张三 → `search_user` → `im.send_message(receive_id_type="open_id")`。

## 命令（工具）清单
- `contact.batch_get`（只读）：按 ID 批量读取用户通讯录详情（最多 50 个）——contact.resolve_user 拿到 ID 后的详情补全链。只读，需 contact:user.base:readonly。
- `contact.get_user`（只读）：按 user_id 或 open_id 读取单个用户的通讯录详情（姓名/邮箱/手机号/部门/上级/状态）。只读，需 contact:user.base:readonly。
- `contact.list_department_members`（只读）：按部门 ID 列出该部门下的员工（返回 open_id/name/employee_id）。department_id 来自 contact.list_departments。只读，需 contact:user.base:readonly。
- `contact.list_departments`（只读）：列出指定部门下的子部门（返回 department_id/name/parent_department_id）。department_id 为 0 时列出根部门。只读，需 contact:department.base:readonly。
- `contact.resolve_user`（只读）：把邮箱或手机号批量换成用户 ID（open_id/user_id/union_id）——发送消息、加群、指派任务前的第一步。emails 与 mobiles 至少填一个，合计不超过 50 项。已知姓名/昵称而非邮箱手机号时请改用 contact.search_user。只读，需 contact:user.base:readonly。
- `contact.search_user`（只读）：按姓名/关键字搜索用户，返回 open_id/user_id/姓名/所属部门——用户说"发给张三"时的第一步（拿到 open_id 后交给 im.send_message，receive_id_type 传 open_id）。只读，需 contact:user.base:readonly。
