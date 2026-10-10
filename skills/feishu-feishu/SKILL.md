---
name: feishu-feishu
description: 飞书 feishu 域工具集：4 个工具（只读 4 / 写 0）。「这个能力有没有」→ `feishu.capability_lookup`（分组级存在性）；
version: 1.0.0
---

# 能力出处与元工具（feishu.*）

## 路由优先级（先判断对象属于哪个域）
- 「这个能力有没有」→ `feishu.capability_lookup`（**分组级**存在性）；
  「这个方法怎么调」→ `feishu.schema_read`（**方法级**签名：HTTP/路由/参数/令牌/风险）；
  「某个域怎么用、有哪些坑」→ `feishu.guidance_read`；
  「已策展的工具里哪个能干这事、启用了吗」→ `feishu.tool_search`。
- 「要调一个没被策展为工具的方法」→ **没有这样的通道**：先用 `feishu.capability_lookup` 判断
  「是不存在（放弃）」还是「存在但未策展」；未策展时**如实告知用户该能力暂不可用**（可建议宿主策展），
  不要臆造调用，也不要把「SDK 里有」说成「现在能用」。
- 四个元工具都是**只读**：不执行业务动作，只回答「有没有 / 怎么调 / 怎么用 / 哪个能用」。

## 前置链
能力不在工具集 → `feishu.capability_lookup`；要方法签名 → `feishu.schema_read`（三种判据：`method` 精确名 / `keyword` 关键字 / `module` 模块，**至少给一个**）；
要深层避坑 → `feishu.guidance_read(domain, topic)`；要定位已策展工具 → `feishu.tool_search(keyword/domain/write_only/read_only/limit)`。

## 避坑
- `capability_lookup` 只回元数据，**不代表能调用**；报「能力存在但未策展」时如实告知，禁止臆造 API。
- `guidance_read` 的键是 `{域}/{主题}`；键不存在时会**列出全部候选键**，从清单里选，不要自行拼造。
- `schema_read` 的 `module` 取**接口名里的资源段**（`IFeishuTenantV1OkrPeriod` → `OkrPeriod`），不是域前缀。
- `schema_read` 的 `curated=false` 只说明「该方法没被策展成工具」——**不是**「可以绕过策展去调它」。
  工具面刻意不提供万能兜底调用（R-12 决策）：一条"任意方法 + 任意参数"的通道会绕开每工具一策展的
  授权、幂等与风险分级，是全工具面风险最高的面。
- `tool_search` 只返回**已注册**工具（含未启用）；未启用的会给出提示，**不要**把它当作「不存在」。

## 安全规则
- 元工具全部**只读**（`feishu:base` scope），不发起任何飞书业务请求，也不产生侧效应。
- **禁止把「SDK 里有」当成「现在能用」向用户承诺**：两者之间隔着「是否策展为工具」与「是否在白名单里」。
- 需要写类动作时，必须走对应的**策展工具**（如 `im.send_message` / `bitable.add_record`）：
  它们经 `FeishuAgent:WriteAllowList` 键控 + 授权门禁，并支持 `dry_run` 预演。
- 不得用「先查签名、再手工拼 HTTP 请求」之类的方式绕开策展边界——模型没有直连网络的能力，
  这类指令只会浪费一轮对话。

## 示例
- 「能发卡片吗」→ `capability_lookup` → 如实告知结果
- 「有没有导出会议报告的方法」→ `schema_read(keyword="export")` → 拿到签名 → 若 `curated=false`，
  告知用户「该能力存在但当前工具集未提供」
- 「哪个工具能读多维表格记录」→ `tool_search(keyword="记录", domain="bitable")` → 得到工具名与启用状态

## 命令（工具）清单
- `feishu.capability_lookup`（只读）：查询飞书开放平台能力在 SDK 中是否存在（按能力分组回答：组名 / 方法数 / 该组所属模块是否已有可用工具）。当所需能力不在当前工具集内时用它判断"是没有还是没启用"，避免凭空猜测调用不存在的接口。只返回元数据，不返回请求构造。
- `feishu.guidance_read`（只读）：按需读取某个域的深层避坑/示例文本（如 im/topics-and-replies、docx/block-editing）。常驻 guidance 只放要点；当某个工具连续失败或你不确定某域的约束时，先读它再重试。只读，需 feishu:base。
- `feishu.schema_read`（只读）：查询任意飞书 SDK 方法的签名事实（HTTP 方法 / 路由 / 参数 / 令牌 / 风险 / 是否已策展为工具）。当需要调用一个当前工具集未覆盖的 API 时，先用本工具查它的方法签名。本工具只读编译期目录、不调用下游，也不提供任何调用通道——查到的未策展方法无法调用，请如实告知用户。
- `feishu.tool_search`（只读）：在已策展的飞书工具中按关键字/域/读写过滤搜索。返回工具名、域、是否写操作、风险等级、所需参数、是否已启用。当工具变多后用它快速定位能用哪个工具，以及该工具是否在当前宿主已启用（未启用的会给出原因提示）。只读，需 feishu:base。
