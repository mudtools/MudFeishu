---
name: feishu-search
description: 飞书 search 域工具集：1 个工具（只读 1 / 写 0）。不知道东西在哪、先定位 → `search.doc_wiki`；已知位置、要取数 → docx / sheets / bitable。
version: 1.0.0
---

# 搜索（search.doc_wiki）

## 路由优先级（先判断对象属于哪个域）
- **不知道东西在哪、先定位** → `search.doc_wiki`；**已知位置、要取数** → docx / sheets / bitable。
- 「问答式找知识」用 `knowledge.search`；「按标题/关键字找文档」用本工具——两者回答的问题不同。

## 前置链
`search.doc_wiki` 取候选 token → 再用 `docx.get_raw_content` / `sheets.get_range_values` 取正文。

## 避坑
- **结果是候选不是内容** —— 不要把标题当答案。
- 范围受应用可见性限制：搜不到不等于不存在，先确认可见范围再下结论。
- 候选可能同时含文档与表格：按 token 类型选对取文工具。
- 中文分词影响召回：换更短的词组或同义词重试一次即可，不要反复长句检索。

## 安全规则
- 本域全为只读工具（无写操作、无 `dry_run` 语义）；调用受 scope 审计约束。
- 只使用本域真实工具名，不臆造不存在的工具或 API。

## 示例
报销制度在哪 → 检索 → 取候选 token → `get_raw_content`。

## 命令（工具）清单
- `search.doc_wiki`（只读）：云文档与知识库全文搜索，返回标题/摘要/URL；结果的 token 可传给 docx.get_raw_content、url 对应节点可传给 wiki.get_node。query 上限 30 字符。只读，需 search:docs:readonly。
