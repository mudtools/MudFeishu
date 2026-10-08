
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
