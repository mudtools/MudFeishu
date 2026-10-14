
# Aily · 智能伙伴数据知识

## 路由优先级
- 「这个应用接了哪些知识库」→ `aily.list_data_assets`（**本域第一跳**，`data_asset_id` 从这来）
- 「某个知识库为什么连不上」→ `aily.get_data_asset`（看 `connect_status` 与 `connect_failed_reason`）
- 「知识库都打了哪些标签」→ `aily.list_data_asset_tags`（按标签筛库前先用它拿 `data_asset_tag_id`）
- 「文档里写过什么 / 流程怎么走」→ `knowledge.search`（**内容问答归它**，与本域是清单 vs 内容的关系）
- 「这个词是什么意思」→ `lingo.*`（企业词典术语）；「文档在哪」→ `wiki.*` / `search.doc_wiki`

## 前置链
1. `app_id` 是 Aily 应用标识（形如 `app_xxx`）——本域**没有**"列出我的 Aily 应用"工具，需从智能伙伴管理后台拿
2. `data_asset_id` 来自 `aily.list_data_assets`
3. 分页：`has_more=true` 带 `page_token` 续取；一次拿全用 `fetch_all=true`（配 `max_items`）

## 避坑
- **本域是"配置清单面"，不是"内容面"**：知识库里的文档内容问答请用 `knowledge.search`，本域只回答"有哪些库、什么状态"。
- `label` / `description` 是多语言映射：投影按 `zh_cn → en_us → 第一个` 取**一个**值，别期待拿到全语言表。
- `connect_status` 表示知识库与数据源的连接状态；连不上时看 `connect_failed_reason`（常见：源文档被删、授权过期）。
- `item_count` 是平台返回的条目数快照；要条目明细需 `get_data_asset(with_data_asset_item=true)`。
- 列表为空 ≠ 出错：先去掉 `keyword` / `data_asset_tag_ids` 收窄条件重试，再判断是否真的没配知识库。

## 写面
- 本域**只读**（3 个工具）：不创建/不删除知识库、不改标签。
- 数据知识的上传（`upload_data_asset_file`）与创建（`create_data_asset`）入参含**本地文件路径**或深嵌套请求体，模型侧不可达——如实告知用户"当前不可用"，不要试图绕。
- 知识库的**删除**属高危（`delete_data_asset` 命中危险词），尚未纳入工具面。

## 安全规则
- 3 个工具全部**只读**，需 `aily:knowledge:readonly`（⚠️ SDK 注释未给 scope 字面量，控制台开通前请按名称核对）。
- 出站净化：只返回知识库标识、标题、描述、连接状态与标签名；**不返回**知识库内的文档内容与切片。
- 软缺席：宿主未启用 Aily API 时返回"未注册客户端"说明，不要反复重试。

## 示例
- 「这个应用接了哪些知识库」→ `aily.list_data_assets(app_id="app_xxx", fetch_all=true)`
- 「财务知识库为什么连不上」→ `aily.get_data_asset(app_id="…", data_asset_id="…")` → 看 `connect_failed_reason`
- 「按'内部制度'标签找库」→ `aily.list_data_asset_tags(app_id="…")` 拿 tag_id → `aily.list_data_assets(data_asset_tag_ids=[…])`
