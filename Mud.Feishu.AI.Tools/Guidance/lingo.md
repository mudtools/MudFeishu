
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
