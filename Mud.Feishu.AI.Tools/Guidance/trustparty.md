
# Trustparty · 跨组织互通

## 路由优先级
- 「我们和哪些组织互通」→ `trustparty.list_collaboration_tenants`（互通面；本域第一跳）
- 「目录里关联了哪些组织」→ `trustparty.list_all_collaboration_tenants`（directory 面）
- 「这个外部组织是谁」→ `trustparty.get_collaboration_tenant`
- 「对端开放了哪些人 / 部门 / 群」→ `trustparty.list_visible_organization`
- 「对端某部门是什么、谁负责」→ `trustparty.get_collaboration_department`
- 「对端某联系人是谁」→ `trustparty.get_collaboration_user`
- 「我们把哪些组织单元共享出去了」→ `trustparty.list_share_entities`
- 「互通规则 / 规则为何没生效」→ `trustparty.list_collaboration_rules`

## 前置链
1. `target_tenant_key` 有两个来源，**别混用**：互通面 `list_collaboration_tenants` 与 directory 面 `list_all_collaboration_tenants`（查规则要后者）
2. 外部部门 / 成员 ID 必须与 `*_id_type` 口径一致（`open_department_id` vs `department_id`、`open_id` vs `user_id`），不一致表现为「查不到」
3. 分页：`has_more=true` 带 `page_token` 续取；一次取全用 `fetch_all=true` + `max_items`

## 避坑
- **两个接口面不是一回事**：一边有数据、另一边为空属正常（目录能力未启用时如此），别据此断言「没有互通」。
- `list_visible_organization` 返回**统一三组键**（部门 / 用户 / 群）：按 `entity_type` 读对应那组，其余为 null 属正常。
- `get_collaboration_department` 的负责人**只给标识**：要姓名先拿 ID 再走 `get_collaboration_user`。
- `get_collaboration_user` **不返回**手机号 / 工号 / 自定义属性 / 头像 / 在职状态（出站净化）——别承诺「帮你查联系方式」。
- `list_collaboration_rules` 的 `subject_is_valid` / `object_is_valid` 是「配了不生效」的解药：实体离职 / 部门被删会让规则失效（结果会给 `invalid_rule_count`）。
- `list_share_entities` 的 `is_select_subject` 区分主体 / 客体侧：查不到先去掉它重试。
- `connect_time` 是平台时间戳（未标单位）：不要自行换算成日期对外表述。

## 安全规则
- 8 个工具全部**只读**；权限点见《工具权限对照表》#132~139。
- 出站净化：跨组织标识、组织名、姓名、职务给；**手机号 · 工号 · 自定义属性 · 头像 · 在职状态不给**。
- 命中范围受对端授权限制：查不到 ≠ 不存在（对端可能未开放该部门 / 成员）。
- **写面（建 / 改 / 删互通规则）尚未纳入**：如实告知「当前不可用」，不要用其它工具绕。
- 软缺席：宿主未启用互通 API 时返回明确的「未注册客户端」说明。

## 示例
- 「我们和哪些组织互通」→ `trustparty.list_collaboration_tenants(fetch_all=true)`
- 「对端开放了哪些人」→ `trustparty.list_visible_organization(target_tenant_key="…", fetch_all=true)`
- 「规则为何没生效」→ `trustparty.list_collaboration_rules(target_tenant_key="…")` → 看 `invalid_rule_count`
