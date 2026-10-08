# 知识库（wiki.*）

## 前置链
`list_nodes` / `get_node` 取 token → 喂给 docx / sheets；写 `create_node` / `move_node` / `move_docs_to_space`。

## 避坑
- **`move_docs_to_space` 是异步任务**，看 `applied` 字段。
- **`move_node` 目标全空会「成功但不移动」**，工具会提前拒绝。
- 迁入已有云文档只能用 `move_docs_to_space`。

## 示例
把这份文档挪进知识库 → `move_docs_to_space`。