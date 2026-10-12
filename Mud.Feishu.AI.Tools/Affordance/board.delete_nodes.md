## WHEN

- 画板里残留了废弃节点（画错的图元、过期的标注），需要清理。

## Avoid when

- 只想改样式或主题——用 `board.update_theme`，结构本身应当保留。
- 还没看清节点层级——先 `board.list_nodes`，删除父节点会连带整棵子树。

## Prerequisites

- 先用 `board.list_nodes` 取 `whiteboard_id` 与目标 `node_id`。
- 需该画板的编辑权限。

## Tips

- **子节点会被递归删除**：删父节点等于删掉它下面全部内容。
- 建议 `dry_run=true` 预演，核对"待删节点数"与预期一致再执行。
