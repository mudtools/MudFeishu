
# 邮件（mail.*）

## 路由优先级（先判断对象属于哪个域）
- **邮件**（mailbox / thread）→ `mail.*`；**即时消息** → `im.*`。同一收件人不代表同一渠道。
- 找「谁发来的合同」优先 `mail.search`（关键字），不要先全量 `list_messages` 再翻页。
- 「先写好再审 / 分几次补内容」→ 草稿族 `create_draft` → `update_draft` → `send_draft`；一次性发出去直接 `send_message`。
- 「以后这种邮件自动归档 / 自动标已读」→ 收件规则族 `rule_*`；「常用邮件存下来复用」→ 模板族 `template_*`；「落款用哪个签名」→ `signature_list`。

## 前置链
读 `list_messages` / `get_message` / `get_thread` / `list_labels`；下载附件先 `get_attachment_url`（`attachment_ids` 来自 `get_message` 的邮件详情）。
草稿族：`create_draft` 拿 draft_id → `update_draft`（整份替换）→ `send_draft` 或 `delete_draft`。
规则族：`rule_list` 拿 rule_id → `rule_create` / `rule_update`（**整条替换**）/ `rule_delete` / `rule_reorder`（顺序即优先级，须给全量 id 序列）。
模板族：`template_list` 拿 template_id → `template_get` 回填 → `template_create` / `template_update` / `template_delete`。
写 `send_message` / `mark_read` / `trash_message`；`get_message` 的 mail_id 先由 `list_messages` 或 `search` 获得。

## 避坑
- **mail 写面需用户身份**，宿主须放行 `user`。
- `send_message` 两步合一（建草稿→发送），模型无需感知草稿态。
- Agent 读完应 `mark_read` 回写，否则用户邮箱堆积未读；恢复未读用同一工具传 `read=false`（无独立 `mark_unread`）。
- **规则/模板的条件与动作用管道分隔的简化式，不要写 JSON**：`field|operator|value`（如 `subject|contains|发票`），旗标型字段可省 operator/value（如 `has_attachment`）。
- `rule_update` / `template_update` 是整体覆盖语义——先 `rule_list` / `template_get` 回填原值再改，否则省略的字段会被清空。
- 删除前确认对象：`trash_message`（进回收站）与 `delete_draft` / `rule_delete` / `template_delete` 语义不同，别混用。
- 回复 / 转发 vs 新建的差别 → `feishu.guidance_read(mail, reply-forward)`。

## 安全规则
- 写工具首次调用一律 `dry_run=true` 预演；确认 method/path 与字段摘要无误后，以 `dry_run=false` 重放**同一参数**。
- high-risk-write 工具须宿主注册 `IToolExecutionAuthorizer` 才会放行，未注册时一律拒绝。
- 只使用本域真实工具名，不臆造不存在的工具或 API。

## 示例
找张三发的合同邮件 → `mail.search` → 需要正文再 `mail.get_message`。
