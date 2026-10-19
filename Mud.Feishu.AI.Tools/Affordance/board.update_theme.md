## WHEN

- 要把整个画板的配色统一切换成另一套主题，节点内容与结构都保持不动。
- 这是全局样式动作：一次调用改变整块画板呈现的配色方案。

## Avoid when

- 要增删或摆放节点——用 `board.create_nodes` / `board.delete_nodes`；主题操作不碰结构。
- 只是想知道当前用的什么主题——用 `board.get_theme`，那是只读。

## Prerequisites

- whiteboard_id 来自文档画板块（`docx.get_document_blocks` 中 block_type=43 的 token）。
- theme_id 是目标主题 ID；不确定取值时先用 `board.get_theme` 读出画板现行主题作参照。

## Tips

- 幂等操作：重复提交同一主题结果一致，重试安全。
- 回执只回显 whiteboard_id 与 theme，不含配色差异；要核实效果可再用 `board.get_theme` 复读。
