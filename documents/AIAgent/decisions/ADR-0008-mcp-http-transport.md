# ADR-0008：MCP Streamable HTTP 传输的租户绑定的形态（E-3 / GAP-10）

- 状态：**已执行**
- 日期：2026-10-20（来源：`MudFeishu-AI-审查整改-Bug修复与功能完善方案-2026-10-16.md` §5 E-3）
- 影响面：`Mud.Feishu.AI.Mcp/Http/*`、`Mud.Feishu.AI.Mcp/McpWireJson.cs`、
  `FeishuMcpServerOptions.Http`、`Extensions/McpServiceCollectionExtensions.cs`、
  `documents/AIAgent/MCP多租户部署Runbook.md`

## 背景

`Mud.Feishu.AI.Mcp` 只有 stdio 一条传输：宿主起一个进程、进程配一个 `appKey`、逐行读 JSON-RPC。
这个形态把「进程」当成了租户边界，安全上很干净，但部署上很重——网关/函数计算/远程 MCP 客户端
要的是「一个 HTTP 端点」。E-3 要补 Streamable HTTP，同时不能把 stdio 那条线换来的隔离性质丢掉：
一个能被模型或提示注入影响的入口，只要允许自报租户，多租户隔离就形同虚设。

## 决策

### 1. 传输层与框架无关，不引任何 Web 依赖

适配器消费/产出的是请求与响应**快照**（`McpHttpRequest` / `McpHttpResponse`），宿主负责把
`HttpContext` 映射进来再映射回去。理由：本包是 SDK 的可选周边，一旦依赖 `Microsoft.AspNetCore.*`
就同时依赖了宿主的技术选型（最小 API / MVC / FastEndpoints / 云函数），且用例不得不起监听。
零新包也让传输层用例可以完全离线断言。

正文由宿主读好后整体传入：JSON-RPC 消息必须整体解析，长度上限属宿主（请求体限制是 Web 层职责）。

### 2. 租户身份的唯一来源是宿主认证层

`McpHttpRequest.AppKey` / `UserId` **只准由认证层填写**。适配器绝不从 URL 路径、查询串、请求体字段
或任意自定义头推导租户。

- 协议参数里的租户标识可由模型影响，采信它等于把「切换租户」交给客户端——与 stdio 侧
  「appKey 只从配置来」是同一条线，只是入口从进程参数换成了 HTTP 字段；
- 认证层没给出身份 ⇒ 401，不做「回落进程配置」的兜底（HTTP 进程可能服务多个租户，
  兜底等于把请求交给随机一个租户）。

### 3. `TenantBoundPerRequest`：把跨租户能力面变成显式承担的决定

`FeishuMcpServerOptions.AppKey` 仍然必填，唯一的豁免是显式设置
`FeishuMcp:Http.TenantBoundPerRequest=true`：

| 形态 | 配置 | 语义 |
|---|---|---|
| 单租户进程（stdio 同构） | `AppKey=cli_a`，`TenantBoundPerRequest=false`（默认） | 只服务 `cli_a`，其它身份 403 |
| 多租户进程 | `AppKey` 可空，`TenantBoundPerRequest=true` | 身份逐请求由认证层给出，会话绑定，`AllowedAppKeys` 作兜底名单 |

默认关闭的理由：打开它意味着「认证层已经把租户判对」这件事由宿主承担，这不是「接了一个端点」
应该隐式触发的授权变更。跨租户宿主的另一条正确形态仍是**每个租户一个进程**（stdio 或独立部署的 HTTP）。

### 4. 会话绑定租户，握手标记每会话一份

`initialize` 在认证成功时创建会话，会话持有 `AppKey`/`UserId`/协商出的协议版本与握手标记；
后续每条请求必须带 `Mcp-Session-Id`，且认证给出的身份必须与会话绑定**逐字相等**，否则 403
（`DELETE` 同规则，否则泄漏一个会话标识就成了一条跨租户注销通道）。

- 允许「按请求改绑」等于「拿到 A 租户的会话标识 = 切到 B 租户」，正是 stdio 用进程边界避免的事；
- 握手标记挂会话而不是挂进程，但 `FeishuMcpToolServer` 仍是**单实例共享**：工具面（365 枚 Schema）
  不随会话重建，两条传输共用同一份白名单与同一份执行链。

### 5. 只做 JSON 响应，不做 SSE；批量不做

能力面是请求-响应式（`tools/*` / `resources/*`），没有服务端主动推送，因此不实现
`text/event-stream`，`GET` 一律 405（带 `Allow: POST, DELETE`），JSON-RPC 批量数组回 400。
这是明确取舍，不是未完成：批量语义要逐条回执，伪装成支持会让客户端拿不到它期望的那条。
通知（无 `id`）回 202 空正文，而不是「看起来像一次成功的往返」。

### 6. Origin 判据 fail-closed

`AllowedOrigins` 为空 ⇒ **任何带 `Origin` 头的请求都被拒**（403）。Origin 只由浏览器发出，
服务端 Agent/CLI 不发，因此默认形态对既有客户端零影响；而 DNS 重绑定/被入侵的网页打到
一个「能读飞书全量数据」的端点时必然带 Origin。拒绝文案不点名清单内容，避免把探测变成反馈。

### 7. 会话目录是进程内的，多实例部署要显式处理

`FeishuMcpSessionDirectory` 只做进程内驻留：数量上界 `MaxSessions`（超限的 `initialize` 回 503）、
空闲 `SessionIdleTimeoutSeconds` 惰性回收（不起后台线程）。多实例部署的**受支持形态只有会话粘滞**——
本实现不提供「会话状态外置」的接缝（目录是 `internal`，没有可替换的存储抽象）：跨进程共享会话意味着
把「会话标识 → 租户」这张表放到多个进程都能写的地方，那正是 §2 好不容易从模型手里拿走的租户判据，
换成一个需要自行保证一致性与失效的外部存储等于把信任重新外包。真要跨实例续用会话，正确做法是客户端
重新 `initialize`（代价是一次握手），不是共享会话。这不是正确性风险：会话标识泄漏换不到租户，
最坏只是「这条会话得重握手」。

## 连带影响

- stdio 的逐行顺序处理**未被推翻**：顺序处理是它自己的 AsyncLocal 串号防线。HTTP 侧的等价防线是
  每次工具调用内 `Begin`/`dispose` 一个作用域 + 会话状态按标识隔离，由
  `InterleavedSessions_ShouldNotLeakTenantAcrossCalls` 观测。
- 线协议 JSON 编码器上收到 `McpWireJson`（单实例）：两条传输各配一份必然漂移，
  漂移表现为同一个中文字符串一边是原文、一边是 `\uXXXX`（既是 token 膨胀也是可读性灾难）。
- 协议层的 `method` 字段读取改为「类型不符按缺失处理」：原实现用 `GetValue<string>()`，
  对 `{"method":123}` 会抛出并击穿「本方法不抛异常」的契约（stdio 表现为断线，HTTP 表现为 500）。
- public 面新增 4 个类型（`FeishuMcpHttpHandler` / `McpHttpRequest` / `McpHttpResponse` /
  `FeishuMcpHttpOptions`）；会话与目录是 `internal`，宿主不需要、也不应自己拼装会话。
