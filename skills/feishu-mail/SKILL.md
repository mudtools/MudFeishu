---
name: feishu-mail
description: 飞书 mail 域工具集：21 个工具（只读 10 / 写 11）。邮件（mailbox / thread）→ `mail.*`；即时消息 → `im.*`。同一收件人不代表同一渠道。
version: 1.0.0
package_version: 3.0.3
---

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

## 命令（工具）清单
- `mail.create_draft`（写）：在用户邮箱中创建一封草稿（不发送，可先检查再经 mail.send 传 draft_id 发出）。to/cc/bcc 为邮箱地址数组，subject 为主题，body 为纯文本正文。写操作：需 mail:mailbox。用户身份工具——宿主须在 AllowedIdentities 放行 user。
- `mail.delete_draft`（写）：删除一封草稿。高危写（high-risk-write）：需 mail:mailbox。建议先 dry_run 预演确认。draft_id 来自 mail.create_draft 或邮件列表。用户身份工具——宿主须在 AllowedIdentities 放行 user。
- `mail.get_attachment_url`（只读）：获取邮件内附件的下载链接（有下载需求时先取链接）。message_id 来自 mail.list_messages；attachment_ids 来自邮件详情。只读，需 mail:mailbox:readonly。
- `mail.get_message`（只读）：获取邮件详情（主题/收发件人/正文预览）。message_id 来自 mail.list_messages。只读，需 mail:mailbox:readonly。
- `mail.get_thread`（只读）：取一封邮件的会话线程（同一主题下的往来邮件）——回复/追问类问题的完整上下文来源。只读，需 mail:mailbox:readonly。
- `mail.list_labels`（只读）：列出邮箱的邮件标签（label）——用于查清可用的 label_id，再传给 mail.list_messages 做标签过滤。只读，需 mail:mailbox:readonly。
- `mail.list_messages`（只读）：列出用户邮箱中的邮件（返回 message_id 列表，可传给 mail.get_message 获取详情）。user_mailbox_id 为用户邮箱地址。只读，需 mail:mailbox:readonly。
- `mail.mark_read`（写）：把邮件标记为已读（或未读）——Agent 读完邮件后应回写，避免用户邮箱堆积未读。写操作：需 mail:mailbox。
- `mail.rule_delete`（写）：删除一条收件规则。高危写（high-risk-write）：需 mail:mailbox。建议先用 mail.rule_list 确认对象并 dry_run 预演。rule_id 来自 mail.rule_list。
- `mail.rule_list`（只读）：列出邮箱的收件规则（含规则 id、名称、启用状态与条件/动作的简化式描述）——用于查清 rule_id 再执行 rule_update/rule_delete/rule_reorder。只读，需 mail:mailbox:readonly。
- `mail.rule_reorder`（写）：调整收件规则的匹配顺序（规则自上而下匹配，**顺序即优先级**；须给出全量规则 id 序列，按目标优先级排列）。rule_ids 来自 mail.rule_list。写操作：需 mail:mailbox。
- `mail.rule_save`（写）：新建或整条替换一条收件规则（自动分拣邮件）。不传 rule_id 即新建；传 rule_id 即整条替换该规则（建议先用 mail.rule_list 回填原内容）。⚠️ 条件与动作用管道分隔的简化式，**不要写 JSON**：condition 形如… 〔需 mail:mailbox〕
- `mail.search`（只读）：按关键字搜索邮箱邮件（如'找张三发的关于合同的邮件'）。user 身份接口，支持分页。只读，需 mail:mailbox:readonly。
- `mail.send`（写）：以用户身份发送邮件。两种用法：① 传 draft_id 发送一封已存草稿（可定时：send_time 为 RFC3339，须至少晚于当前时间 5 分钟）；② 不传 draft_id 则现写现发（to/subject/body 必填，内部先建草稿再发送，模型无需感知中间态）。两种用法不要混用：有 draft_id 时不再接受 to/subject/body/cc/bcc。写操作：需 mail:mailbox… 〔user 身份；需 mail:mailbox〕
- `mail.signature_list`（只读）：列出用户邮箱的邮件签名（signature_id/名称/摘要内容）与各邮箱地址的签名使用配置——写信时按签名落款前先查清可用签名。只读，需 mail:mailbox:readonly。用户身份工具——宿主须在 AllowedIdentities 放行 user。
- `mail.template_delete`（写）：删除一份邮件模板（**删除后不可恢复**，模板将不再出现在列表中）。高危写（high-risk-write）：需 mail:mailbox。建议先用 mail.template_list 确认对象并 dry_run 预演。
- `mail.template_get`（只读）：获取邮件模板的完整内容（名称/主题/正文/收件地址）——用于按模板起草邮件前读取模板正文做二次填充。template_id 来自 mail.template_list。只读，需 mail:mailbox:readonly。
- `mail.template_list`（只读）：列出邮箱的个人邮件模板（template_id/名称/创建时间）——用于查清 template_id 再取详情或更新。只读，需 mail:mailbox:readonly。
- `mail.template_save`（写）：新建或整段替换一份可复用的个人邮件模板。不传 template_id 即新建；传 template_id 即整段替换该模板（建议先用 mail.template_get 回填原内容，再改需要变的字段）。template_content 为模板正文（可含 HTML）；tos/ccs/bccs 为默认收件地址数组。写操作：需 mail:mailbox。
- `mail.trash_message`（写）：批量把邮件移到已删除文件夹（**回收站语义，可在邮箱里找回**，不是永久删除）。message_ids 来自 mail.list_messages。写操作：需 mail:mailbox。
- `mail.update_draft`（写）：更新一封已有草稿的内容（**整段替换**：直接给全 to/subject/body 等完整内容，平台会用新内容整体替换旧事实）。draft_id 来自 mail.create_draft 或邮件列表。写操作：需 mail:mailbox。用户身份工具——宿主须在 AllowedIdentities 放行 user。

## 参考文档（细则）
- `references/reply-forward.md`：`mail/reply-forward`
- 正文里形如 `feishu.guidance_read(mail, 主题)` 的指针，在文件形态下读本包的 `references/主题.md`（本包细则就是那份资产，无需进程内工具）
