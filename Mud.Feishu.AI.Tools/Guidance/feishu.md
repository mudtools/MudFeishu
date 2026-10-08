
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
