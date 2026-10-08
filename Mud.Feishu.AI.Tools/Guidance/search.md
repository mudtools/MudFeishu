# 搜索（search.doc_wiki）

## 前置链
`search.doc_wiki` 取候选 token → 再用 docx / sheets 取正文。

## 避坑
- **结果是候选不是内容** —— 不要把标题当答案。
- 范围受应用可见性限制。

## 示例
报销制度在哪 → 检索 → 取候选 token → `get_raw_content`。