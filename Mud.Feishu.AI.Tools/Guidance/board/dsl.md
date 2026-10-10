
# 画板 DSL 用法（board.render_dsl）

## 该用哪种语法
| 想表达 | 语法 | 关键行 |
|---|---|---|
| 交互时序（谁调谁、先后顺序） | `mermaid` | `sequenceDiagram` |
| 流程 / 判断分支 | `mermaid` | `flowchart TD` |
| 类图 / 架构分层 | `mermaid` | `classDiagram` |
| 组件 / 部署（需要精确布局） | `plantuml` | `@startuml ... @enduml` |

## 前置链
`whiteboard_id` 不在本域产生：`docx.get_document_blocks` → 找 `block_type=43` 的块 → 其 `token` 即
`whiteboard_id`。拿到后再 `board.render_dsl`。已有节点结构时先 `board.list_nodes` 看清再改。

## 避坑
- 语法错误 → 画板返回错误而非静默忽略：按报错行号修正后重试，不要反复提交同一份源码。
- 中文标签：Mermaid 标签含空格或括号时用引号包住（`A["开始 处理"]`），否则解析失败。
- 画板未就绪（文档里还没有画板块）→ 用 `board.get_theme` 探测；仍失败则先在飞书文档中插入画板块。
- 源码里不要写 ``` 围栏；`dsl_type` 与源码语法必须一致（mermaid 源码配 mermaid）。
- 节点很多时先 `board.list_nodes` 看现有结构，避免重复叠加。

## 示例
「把这份部署流程画成时序图」→ 用户给文档链接 → `docx.get_document_blocks` 找画板块 →
`board.render_dsl`（`dsl_type=mermaid`，源码 `sequenceDiagram ...`）→ 回填渲染结果。
