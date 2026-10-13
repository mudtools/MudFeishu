
# Trustparty · 跨组织互通

## 路由优先级
- 「和哪些组织互通」→ `trustparty.list_collaboration_tenants`（互通面）
- 「目录关联了哪些组织」→ `trustparty.list_all_collaboration_tenants`（directory 面）
- 「这个外部组织是谁」→ `trustparty.get_collaboration_tenant`
- 「对端开放了哪些人 / 部门 / 群」→ `list_visible_organization`
- 「对端某部门是什么」→ `trustparty.get_collaboration_department`
- 「对端某联系人是谁」→ `trustparty.get_collaboration_user`
- 「哪些组织单元被共享出去」→ `trustparty.list_share_entities`
- 「互通规则 / 为何没生效」→ `trustparty.list_collaboration_rules`

## 前置链
1. `target_tenant_key` 有两个来源，**别混用**：互通面 `list_collaboration_tenants` 与 directory 面 `list_all_collaboration_tenants`（查规则要后者）
2. 外部部门 / 成员 ID 必须与 `*_id_type` 口径一致，不一致表现为「查不到」
3. 分页：`has_more=true` 带 `page_token` 续取；一次取全用 `fetch_all=true` + `max_items`

## 避坑
- **两个接口面不是一回事**：一边有数据、另一边为空属正常，别据此断言「没有互通」。
- `list_visible_organization` 是**统一三组键**：按 `entity_type` 读对应那组，其余为 null。
- `get_collaboration_department` 的负责人**只给标识**：要姓名再走 `get_collaboration_user`。
- `get_collaboration_user` **不返回**手机号 / 工号 / 自定义属性 / 头像 / 在职状态。
- `list_collaboration_rules` 的 `subject_is_valid` / `object_is_valid` 是「配了不生效」的解药（结果会给 `invalid_rule_count`）。
- `list_share_entities` 的 `is_select_subject` 区分主/客体侧：查不到先去掉它重试。

## 写面
- 三个写工具：`create_collaboration_rule` / `update_collaboration_rule` / `delete_collaboration_rule`，scope 同为 `trust_party:collaboration_rule:write`。
- **默认不启用**：要过宿主授权门禁（也可能被 `MaxToolRisk` 收回）——未授权时如实告知，勿反复重试。
- 写操作**先 `dry_run=true` 预演**（零调用下游），核对条数与 rule_id 后再执行。
- 主体 = 我方实体，客体 = 对端实体，两侧各至少一类；实体数之和须 &lt; 100。
- **`update` 是覆盖式**：未传的一侧会被清空，只想加成员要先读出原规则再整份提交。
- **`delete` 不可撤销**：对端立即失去互通能力，只能重建且换新 rule_id。

## 安全规则
- 8 只读 + 3 写；权限见《工具权限对照表》#132~142。
- 出站净化：标识、组织名、姓名、职务给；**手机号 · 工号 · 自定义属性 · 头像 · 在职状态不给**。
- 命中范围受对端授权限制：查不到 ≠ 不存在。
- 软缺席：宿主未启用互通 API 时返回「未注册客户端」说明，不要反复重试。

## 示例
- 「和哪些组织互通」→ `list_collaboration_tenants(fetch_all=true)`
- 「规则为何没生效」→ `trustparty.list_collaboration_rules(target_tenant_key="…")` → 看 `invalid_rule_count`
- 「给对端加对接人」→ 读出原规则 → `update_collaboration_rule`（合并后整份提交，先 dry_run）
