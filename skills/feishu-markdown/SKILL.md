---
name: feishu-markdown
description: 飞书 markdown 域工具集：4 个工具（只读 2 / 写 2）。新建一份在线文档 → `markdown.create`（产物是 docx，可继续被本域改写）。
version: 1.0.0
package_version: 3.1.1
---

# Markdown 生命周期（markdown.*）

## 路由优先级
- **新建**一份在线文档 → `markdown.create`（产物是 docx，可继续被本域改写）。
- **读正文**：新版文档 `token_type=docx`；云空间 `.md` 资源文件 `token_type=file`（只读）。
- **改一段** → `markdown.patch`（精确文本命中，能防误改）；**整体重写** → `docx.replace_document`。
- **写入前预审** → `markdown.diff`（远端 vs 草稿），确认只改预期的行。

## 前置链
- 新建：`markdown.create` → 取 `document_token` → 后续 patch / `docx.replace_document`。
- 改写：`markdown.fetch`（或 `markdown.diff`）确认现状 → `markdown.patch`（先 `dry_run=true`）→ 再核对。
- 定位文件：不知道 token 时用 `drive.list_folder_files` 或 `search.doc_wiki`。

## 避坑
- **云空间 `.md` 资源文件没有原位更新**：覆盖只能删旧建新（token 变化）。要「可改写的 Markdown」就用本域建 docx。
- `find` 必须与正文逐字一致（含空白）；命中多处须显式 `replace_all=true`，否则拒绝。
- 正文窗口化读取：`has_more=true` 用 `next_offset` 续读，别把一窗当全文。
- 整篇重建是「先追加后删除」：失败不丢内容，但删除失败返回 `partial_failure`，按指引修复而非盲目重跑。
- 源文件名由 `title` 生成，导入扩展名固定 `md`；调用方不能改后缀（平台会拒）。

## 安全规则
- 2 枚写工具默认不启用（`FeishuAgent:WriteAllowList` + 宿主授权），首次一律 `dry_run=true`。
- `idempotency_key` 必填且稳定：重试复用同一值，避免重复插入。
- Markdown 正文是用户数据，按原样回填；不要拼接未校验的外部内容。

## 示例
- 「把这篇 Markdown 建成飞书文档」→ `markdown.create`。
- 「文档第三段里的旧日期改成新日期」→ `markdown.patch`（find 带上下文保证唯一）。
- 「对照草稿看看远端改了什么」→ `markdown.diff`（right_text=草稿）。

## 命令（工具）清单
- `markdown.create`（写）：把 Markdown 内容创建为飞书在线文档（异步导入为 docx），返回 document_token/url。导入任务终态后自动清理源 .md 临时文件。写操作：需 drive:drive。
- `markdown.diff`（只读）：比较两份 Markdown 的行级差异（左侧 token；右侧二选一：token 或直接文本），返回 unified diff 与增删行数。用于写入前预审、或核对远端是否被别人改过。只读，需 drive:drive:readonly。
- `markdown.fetch`（只读）：读取 Markdown 正文（token_type=docx 读新版文档正文；token_type=file 读云空间 .md 资源文件）。按 offset/limit 分窗口返回，has_more=true 时用 next_offset 续读。只读，需 drive:drive:readonly。
- `markdown.patch`（写）：按精确文本替换文档正文的一段（find 必须唯一命中；多处命中须显式 replace_all=true）。写操作：需 docx:document + drive:drive:readonly。
