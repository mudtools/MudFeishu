## WHEN

- 库内的页面放错了层，需要换父节点或换空间，内容与标识都保持不变。
- 做归档与目录整理：把已完结的页面挪进归档位置。

## Avoid when

- 对象还是云盘里的文档，没进过库——用 `wiki.move_docs_to_space`。
- 原位还要留一份，别处再多一份——用 `wiki.node_copy`，本工具不复制。
- 只是想确认它现在挂在哪、指向什么——用 `wiki.get_node`，无需写入。

## Prerequisites

- `space_id` 是节点当前所在空间，`node_token` 取自 `wiki.list_nodes`。
- `target_parent_token` 与 `target_space_id` 至少要给一个，两者都空会被本地直接拒绝。
- 跨空间时先 `wiki.space_list` 确认目标空间对调用身份可见。

## Tips

- 目标全空时平台会"返回成功但原地不动"，工具提前挡下这一手，按报错补齐参数即可。
- 移动只改变树上的位置，节点标识与其底层文档标识都不变，外部引用无需重写。
- 回执只说未返回节点信息时，先用 `wiki.list_nodes` 复核实际位置，别急着重发一次移动。
