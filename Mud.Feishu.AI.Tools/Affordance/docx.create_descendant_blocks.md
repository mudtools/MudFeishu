## WHEN

- 一次落下一棵**带父子层级**的结构（一个容器块加它下面的若干子块），而不是拆成多次插入。
- 顶层子块的先后顺序有讲究，需要显式给出顺序。

## Avoid when

- 只插一个块——用 `docx.append_blocks`（`parent_block_id` + `block_type`/`text` 单块简写），不必搭临时 ID 体系。
- 往文档尾部平铺追加——用 `docx.append_blocks`。
- 只改文案不动结构——用 `docx.update_blocks`。

## Prerequisites

- `block_id` 是承接这棵子树的父块真实 ID，与 `document_id` 同篇（先 `docx.get_document_blocks` 定位）。
- `descendants` 是扁平数组，每块自带一个临时 ID；`children_id` 只列顶层那批，且每个都要能在 `descendants` 里对上。

## Tips

- 临时 ID 引用对不上会被当场拒掉，`dry_run` 也跑这道校验——可以拿它当结构自检。
- 返回给的是临时 ID 到真实块 ID 的对应关系；下一跳编辑要用真实 ID，别复用临时 ID。
- 块型面仍是文本类闭集，这棵树里的表格建不出来。
