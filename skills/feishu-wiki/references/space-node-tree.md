# 知识库空间与节点树

## 前置链
1. `wiki.list_nodes` / `wiki.get_node` 拿 **node_token** 与其指向的 **obj_token**
2. 取正文用 `obj_token` 进 `docx.*` / `sheets.*`（`node_token` 不能直接当文档 token 用）
3. 写：`wiki.create_node`（在父节点下建） / `wiki.move_node`（移动） / `wiki.move_docs_to_space`（迁入云文档）

## 避坑
- **两级 token 别混用**：node_token 是「知识库里的位置」，obj_token 是「底层文档/表格」。
  把 node_token 传给 `docx.get_raw_content` 是最常见的失败。
- `create_node` 需要 `space_id` 与 `parent_node_token`；只给名字会建到错误层级（或直接被拒）。
- **`move_node` 必须给出目标**：目标全空时平台会「成功但不移动」，工具已提前拒绝——按提示补参数。
- **`move_docs_to_space` 是异步任务**：返回 `applied` 字段说明是否立刻生效，不要假定已可读。
- 节点可见性受应用可见范围限制：`get_node` 报未找到时先确认是不是范围问题，不要反复换 token。

## 示例
- 「知识库里的产品文档讲了什么」→ `get_node` → 用 obj_token 调 `docx.get_raw_content`
- 「把这份云文档挪进知识库」→ `move_docs_to_space` → 检查 `applied`