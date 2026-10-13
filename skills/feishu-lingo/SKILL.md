---
name: feishu-lingo
description: 飞书 lingo 域工具集：6 个工具（只读 6 / 写 0）。不知道租户有哪些词库 → `lingo.list_repos`（进入本域的第一跳，`repo_id` 从这来）
version: 1.0.0
package_version: 3.0.2
---

# Lingo · 企业词典

## 路由优先级
- 不知道租户有哪些词库 → `lingo.list_repos`（**进入本域的第一跳**，`repo_id` 从这来）
- 问「我们内部说的 XX 是什么意思」→ `lingo.search_entities`（关键词）→ 命中后 `lingo.get_entity` 取全文
- 只想确认「这个词是不是我们的术语」→ `lingo.match_entities`（精确词形，**不返回正文**）
- 想看栏目目录 → `lingo.list_classifications`；想浏览全部术语 → `lingo.list_entities`（无关键字，翻页）

## 前置链
1. `repo_id` 来自 `lingo.list_repos`；省略即"全部可见词库"
2. `entity_id` 来自 `lingo.list_entities` / `lingo.search_entities`
3. 列表接口分页：`has_more=true` 时带返回的 `page_token` 续取；要一次拿全用 `fetch_all=true`（配 `max_items`）

## 避坑
- `match_entities` **只回命中的 entity_id/type，不回词条正文**——问含义时别用它，用 `search_entities` + `get_entity`。
- `match_entities` 是**精确词形**匹配：整句长文本命中率低，给词或短语。
- `search_entities` 关键词太长/太具体容易搜不到：先用短语试，再考虑去掉 `repo_id` 扩大范围。
- `list_entities` **不带关键词过滤**："找某个术语"要用 `search_entities`，否则会白翻很多页。
- 列表里的 `description` 是**摘要**（已截断）；要完整正文用 `lingo.get_entity`。
- 本域**不返回**词条的创建人 / 更新人与关联对象（出站净化）——需要"谁建的"请走其它域工具。

## 安全规则
- 6 个工具全部**只读**，需 `baike:entity:readonly`；本域无写操作，不会改动任何词条。
- 词条内容是企业自建 / 官方词库的**参考资料**：可用于对齐术语口径，但不要当作制度或合规依据。
- 软缺席：宿主未启用词典 API 时这些工具会返回明确的"未注册客户端"说明——此时不要反复重试。

## 示例
- 「OKR 在我们公司指什么」→ `lingo.search_entities(query="OKR")` → `lingo.get_entity(entity_id=…)`
- 「这些词里哪些是我们的术语」→ 逐个 `lingo.match_entities(word=…)`
- 「把词典全量导出来」→ `lingo.list_repos` → `lingo.list_entities(repo_id=…, fetch_all=true)`

## 命令（工具）清单
- `lingo.get_entity`（只读）：按 entity_id 查询单个词条的完整信息（主词/别名/描述/富文本/点赞统计/多语言描述）——拿到 main_keys 与 description 后可回答「这个词是什么意思」。只读，需 baike:entity:readonly。
- `lingo.list_classifications`（只读）：列出企业词典的分类（栏目）目录，可按 repo_id 收窄；返回 classification_id/name/father_id，用 father_id 可还原层级。只读，需 baike:entity:readonly。
- `lingo.list_entities`（只读）：分页列出企业词典词条（返回 entity_id/main_keys/aliases/description）——用于浏览「这个租户有哪些术语」，不按关键词过滤。按关键词查词条请用 lingo.search_entities。只读，需 baike:entity:readonly。
- `lingo.list_repos`（只读）：列出企业词典的全部词库（返回 repo_id/name）——进入词典域的第一步：拿到 repo_id 后才能列分类、列词条或收窄搜索范围。只读，需 baike:entity:readonly。
- `lingo.match_entities`（只读）：把一段词/短语与企业词典做精确匹配（返回命中的 entity_id/type）——用于判断「这个词是不是我们的专有术语」，不返回词条正文；要词条含义请改用 lingo.search_entities + lingo.get_entity。只读，需 baike:entity:readonly。
- `lingo.search_entities`（只读）：按关键词搜索企业词典词条（返回 entity_id/main_keys/aliases/description）——回答「我们内部说的 XX 是什么」的首选工具；可用 repo_id 收窄词库、creators 只看指定创建人建的词条。只读，需 baike:entity:readonly。
