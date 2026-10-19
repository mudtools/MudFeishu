## WHEN

- 已有明确的节点结构（类型、父子关系），要批量精确摆放画板节点。
- 渲染结果不满足布局要求，需要按 parent_id 手动补建节点。

## Avoid when

- 只是把推理结果画成图（时序图/流程图）——用 `board.render_dsl`，不必手写节点结构。
- 还没看清画板现状——用 `board.list_nodes`，别在陌生结构上叠加创建。
- 要清理废弃节点——用 `board.delete_nodes`。

## Prerequisites

- whiteboard_id 来自文档画板块：`docx.get_document_blocks` 中 block_type=43 的 token。
- nodes_json 为非空 JSON 数组字符串，每个节点含 type/parent_id/props 等字段。

## Tips

- 空白或解析失败的 nodes_json 在下发前被本地拒绝。
- 传稳定的 idempotency_key 重试才不会重复建节点；新节点 ID 只回在 created_nodes 回执里。
