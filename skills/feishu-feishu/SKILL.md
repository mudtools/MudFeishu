---
name: feishu-feishu
description: 飞书 feishu 域工具集：5 个工具（只读 4 / 写 1）。「这个能力有没有」→ `feishu.capability_lookup`（分组级存在性）；
version: 1.0.0
---

# 能力出处与元工具（feishu.*）

## 路由优先级（先判断对象属于哪个域）
- 「这个能力有没有」→ `feishu.capability_lookup`（**分组级**存在性）；
  「这个方法怎么调」→ `feishu.schema_read`（**方法级**签名：HTTP/路由/参数/令牌/风险）；
  「某个域怎么用、有哪些坑」→ `feishu.guidance_read`。
- 「要调一个没被策展为工具的方法」→ 先 `feishu.schema_read` 拿签名，再 `feishu.api_call` 兜底调用。
- 三者都是**元工具**：不执行业务动作，只回答「有没有 / 怎么调 / 怎么用」。

## 前置链
能力不在工具集 → `feishu.capability_lookup`；要方法签名 → `feishu.schema_read`（三种判据：`method` 精确名 / `keyword` 关键字 / `module` 模块，**至少给一个**）；
要深层避坑 → `feishu.guidance_read(domain, topic)`；兜底调用 → `feishu.api_call(method, path_params, query_params, body)`。

## 避坑
- `capability_lookup` 只回元数据，**不代表能调用**；报「能力存在但未策展」时如实告知，禁止臆造 API。
- `guidance_read` 的键是 `{域}/{主题}`；键不存在时会**列出全部候选键**，从清单里选，不要自行拼造。
- `schema_read` 的 `module` 取**接口名里的资源段**（`IFeishuTenantV1OkrPeriod` → `OkrPeriod`），不是域前缀。
- **`api_call` 的四条边界**（不是参数问题，改了参数也不行）：① `token_kind=user` 的方法**拒绝**（兜底只有租户身份，硬调会造成静默身份错配）；② `risk=high-risk-write` 的方法**拒绝**（高危必须走策展工具）；③ `path_params` 必须**恰好**覆盖路由里的 `{x}` 占位符；④ **默认 `dry_run=true`**，以 `dry_run=false` 重放同一参数才真正发请求。
- `api_call` 以**已策展方法**为目标时，预览里会给出 `curated_tool`——有专用工具就用专用工具（它带投影、截断与更精确的风险分级）。
- 三个工具都只读编译期常量，**不发起任何飞书请求**（`api_call` 的 `dry_run=true` 亦然）。

## 安全规则
- `feishu.api_call` 是**写类工具**：必须在 `FeishuAgent:WriteAllowList` 里显式键控，并注册 `IToolExecutionAuthorizer` 才会放行。
- 首次调用一律 `dry_run=true` 预演；确认 method/url/参数摘要无误后，以 `dry_run=false` 重放**同一参数**。
- 不得把「能力存在」当成「已启用」向用户承诺；两者差一个白名单。

## 示例
- 能发卡片吗 → `capability_lookup` → 如实告知结果
- 「有没有导出会议报告的方法」→ `schema_read(keyword="export")` → 拿到签名
- 「调它」→ `api_call(method, …, dry_run=true)` 预演 → 确认后 `dry_run=false`

## 命令（工具）清单
- `feishu.api_call`（写）：通过方法名 + 参数字典调用任意飞书 SDK 方法（万能兜底）。使用前先用 feishu.schema_read 查方法签名。默认 dry_run=true 只预览不调用；dry_run=false 时实际发起 HTTP 请求。写操作须宿主授权。
- `feishu.capability_lookup`（只读）：查询飞书开放平台能力在 SDK 中是否存在（按能力分组回答：组名 / 方法数 / 该组所属模块是否已有可用工具）。当所需能力不在当前工具集内时用它判断"是没有还是没启用"，避免凭空猜测调用不存在的接口。只返回元数据，不返回请求构造。
- `feishu.guidance_read`（只读）：按需读取某个域的深层避坑/示例文本（如 im/topics-and-replies、docx/block-editing）。常驻 guidance 只放要点；当某个工具连续失败或你不确定某域的约束时，先读它再重试。只读，需 feishu:base。
- `feishu.schema_read`（只读）：查询任意飞书 SDK 方法的签名事实（HTTP 方法 / 路由 / 参数 / 令牌 / 风险 / 是否已策展为工具）。当需要调用一个当前工具集未覆盖的 API 时，先用本工具查它的方法签名，再用 feishu.api_call 发起调用。只读编译期目录，不调用下游。
- `feishu.tool_search`（只读）：在已策展的飞书工具中按关键字/域/读写过滤搜索。返回工具名、域、是否写操作、风险等级、所需参数、是否已启用。当工具变多后用它快速定位能用哪个工具，以及该工具是否在当前宿主已启用（未启用的会给出原因提示）。只读，需 feishu:base。
