## WHEN

- 要把流程、时序、类图之类的推理结果画成图并放进画板，你能直接产出 Mermaid 或 PlantUML 源码。
- 你不了解画板节点结构也能出图：节点解析由平台完成。

## Avoid when

- 已有精确坐标/父子节点数据——用 `board.create_nodes`，别让 DSL 解析器猜布局。
- 要查看或清理现有节点——用 `board.list_nodes` / `board.delete_nodes`。
- 只换全局配色——用 `board.update_theme`。

## Prerequisites

- whiteboard_id 取自文档画板块（`docx.get_document_blocks` 里 block_type=43 的 token）。
- dsl_type 仅接受 plantuml 或 mermaid，且必须与 content 的语法一致，否则下发前即被拒。

## Tips

- content 放源码本体，不要包 markdown 围栏。
- 语法错误会报错而非静默忽略：按报错修正再提交，别原样重试；画板已有内容多时先 `board.list_nodes` 防止重复叠加。
