---
name: feishu-lingo
description: 飞书 lingo 域工具集：11 个工具（只读 6 / 写 5）。不知道租户有哪些词库 → `lingo.list_repos`（本域第一跳，`repo_id` 来源）
version: 1.0.0
package_version: 3.0.3
---

# Lingo · 企业词典

## 路由优先级
- 不知道租户有哪些词库 → `lingo.list_repos`（**本域第一跳**，`repo_id` 来源）
- 问「我们内部说的 XX 是什么意思」→ `lingo.search_entities` → 命中后 `lingo.get_entity`
- 只想确认「这个词是不是我们的术语」→ `lingo.match_entities`（精确词形，**不返回正文**）
- 想看栏目目录 → `lingo.list_classifications`；想浏览全部术语 → `lingo.list_entities`（翻页）
- 「建 / 改 / 删词条」→ 写面（见下）；要走审核用草稿两个工具

## 前置链
1. `repo_id` 来自 `lingo.list_repos`；省略即"全部可见词库"
2. `entity_id` 来自 `lingo.list_entities` / `lingo.search_entities`
3. 分页：`has_more=true` 带 `page_token` 续取；一次拿全用 `fetch_all=true`
4. 写操作前先确认宿主已授权（写工具**默认不启用**），并先 `dry_run=true` 预演

## 避坑
- `match_entities` **只回 entity_id/type，不回正文**——问含义用 `search_entities` + `get_entity`。
- `match_entities` 是**精确词形**：长文本命中率低，给词或短语。
- `search_entities` 关键词太长/太具体容易搜不到：先用短语试。
- `list_entities` **不带关键词过滤**："找某个术语"要用 `search_entities`。
- 列表里的 `description` 是**摘要**；完整正文用 `lingo.get_entity`。
- 本域**不返回**创建人 / 更新人与关联对象（出站净化）。

## 写面
- 5 个写工具：`create_entity` / `update_entity` / `delete_entity` / `create_draft` / `update_draft`。
- **免审 vs 草稿**：`*_entity` 是**免审直写**（直接进词库，需 `baike:entity:exempt_review`）；`*_draft` 走**审核流程**（需 `baike:entity` 或 `exempt_review` 任一）。
- `update_entity` / `update_draft` 是**覆盖式**：未传的释义/别名会被清空；只改一项先 `get_entity` 读原值再整份提交。
- `description` 与 `rich_text` **至少填一个**（都空报 1540001）；释义 ≤5000 字符、别名最多 10 个——校验早于 dry_run。
- **删除高危**：`delete_entity` 不可逆且外部引用全部失效，执行前先读 `Affordance/lingo.delete_entity.md`。
- **不做**：`upload_file`（本地文件路径，模型无法可靠构造）；`related_meta`/`outer_info`/`i18n_descs` 嵌套字段（扁平参数无法表达）。

## 安全规则
- 6 个只读 + 5 个写；权限点见《工具权限对照表》#155~159。
- 只读需 `baike:entity:readonly`；写需 `baike:entity:exempt_review`/`exempt_delete`，草稿需 `baike:entity`（**scope 名取自 SDK 注释原文**，无需 ⚠️）。
- 写工具默认不启用，要过宿主授权门禁；高危写一律先 `dry_run` 预演并向用户复述后果。
- 词条内容是企业自建 / 官方词库的**参考资料**：不要当作制度依据。
- 软缺席：宿主未启用词典 API 时返回"未注册客户端"说明。

## 示例
- 「OKR 在我们公司指什么」→ `lingo.search_entities(query="OKR")` → `lingo.get_entity(entity_id=…)`
- 「这些词里哪些是我们的术语」→ 逐个 `lingo.match_entities(word=…)`
- 「建个词条」→ `lingo.create_entity(main_key="…", description="…", dry_run=true)` → 确认后执行

## 命令（工具）清单
- `lingo.create_draft`（写）：发起创建词条的**草稿申请**（需经词典管理员审核通过后才写入词库）——没有免审权限或需要留痕时的正确路径。写操作：默认不启用，需宿主授权，需 baike:entity 或 baike:entity:exempt_review（任一即可）。
- `lingo.create_entity`（写）：创建词典词条（⚠️ 免审直写：不经过词典管理员审核，直接写入词库——要审核流程请改用 lingo.create_draft）。写操作：默认不启用，需宿主授权（IToolExecutionAuthorizer），需 baike:entity:exempt_review。建议先 dry_run 预演。
- `lingo.delete_entity`（写）：删除词典词条（⚠️ 不可逆：词条直接从词库移除，引用该 entity_id 的外部系统会全部失效——务必先 dry_run 预演并确认 entity_id 无误）。写操作：默认不启用，需宿主授权，需 baike:entity:exempt_delete。
- `lingo.get_entity`（只读）：按 entity_id 查询单个词条的完整信息（主词/别名/描述/富文本/点赞统计/多语言描述）——拿到 main_keys 与 description 后可回答「这个词是什么意思」。只读，需 baike:entity:readonly。
- `lingo.list_classifications`（只读）：列出企业词典的分类（栏目）目录，可按 repo_id 收窄；返回 classification_id/name/father_id，用 father_id 可还原层级。只读，需 baike:entity:readonly。
- `lingo.list_entities`（只读）：分页列出企业词典词条（返回 entity_id/main_keys/aliases/description）——用于浏览「这个租户有哪些术语」，不按关键词过滤。按关键词查词条请用 lingo.search_entities。只读，需 baike:entity:readonly。
- `lingo.list_repos`（只读）：列出企业词典的全部词库（返回 repo_id/name）——进入词典域的第一步：拿到 repo_id 后才能列分类、列词条或收窄搜索范围。只读，需 baike:entity:readonly。
- `lingo.match_entities`（只读）：把一段词/短语与企业词典做精确匹配（返回命中的 entity_id/type）——用于判断「这个词是不是我们的专有术语」，不返回词条正文；要词条含义请改用 lingo.search_entities + lingo.get_entity。只读，需 baike:entity:readonly。
- `lingo.search_entities`（只读）：按关键词搜索企业词典词条（返回 entity_id/main_keys/aliases/description）——回答「我们内部说的 XX 是什么」的首选工具；可用 repo_id 收窄词库、creators 只看指定创建人建的词条。只读，需 baike:entity:readonly。
- `lingo.update_draft`（写）：更新词条草稿（⚠️ 已审批的草稿无法编辑；且为覆盖式提交，未传的释义/别名会被清空；main_key 必填）。写操作：默认不启用，需宿主授权，需 baike:entity 或 baike:entity:exempt_review（任一即可）。
- `lingo.update_entity`（写）：更新词典词条（⚠️ 免审直写，且为**覆盖式**：未传的释义/别名会被清空——只改一项请先 lingo.get_entity 读出原值再整份提交；main_key 必填，与平台口径一致）。写操作：默认不启用，需宿主授权，需 baike:entity:exempt_review。建议先 dry_run 预演。
