# MCP 多租户部署 Runbook

> 本文档适用于 `Mud.Feishu.AI.Mcp` 包的运维与部署。

## 1. 部署模型

### 1.1 进程 = 租户边界

MCP（Model Context Protocol）在 stdio 形态下每个 MCP Server 实例是一个独立进程。飞书 MCP 部署遵循以下原则：

| 原则 | 说明 |
| --- | --- |
| **进程 = 租户** | 每个租户一个进程实例，进程间无共享状态（stdio 形态） |
| **配置隔离** | 每个进程独立读取自己的 `FeishuAppConfig`（AppKey/AppSecret） |
| **日志走 stderr** | stdio 线协议禁止 stdout 输出非 JSON-RPC 内容（见 §4） |

> **Streamable HTTP 形态的边界不同**：一个进程可服务多个远端调用方，租户边界从「进程」变为
> **「宿主认证层逐请求给出的身份」**（见 §2）。两种形态共用同一执行链与同一白名单，**传输层不改变任何授权语义**。

### 1.2 部署清单

```bash
# 租户 A
FEISHU_APP_KEY=cli_a_xxx FEISHU_APP_SECRET=secret_a \
  dotnet Mud.Feishu.AI.Mcp.dll --urls stdio 2>logs/tenant-a.err

# 租户 B
FEISHU_APP_KEY=cli_b_xxx FEISHU_APP_SECRET=secret_b \
  dotnet Mud.Feishu.AI.Mcp.dll --urls stdio 2>logs/tenant-b.err
```

### 1.3 配置矩阵

| 配置项 | 来源 | 多租户隔离方式 |
| --- | --- | --- |
| `FeishuAppConfig:AppKey` | 环境变量 / `appsettings.json` | 每进程独立 |
| `FeishuAgentOptions:Tools` | 配置文件 | 每进程独立（白名单按租户裁剪） |
| `FeishuAgentOptions:WriteAllowList` | 配置文件 | 每进程独立（写工具按租户裁剪） |
| 令牌缓存 | 进程内 `IFeishuTokenStore` | 进程隔离（单进程内多租户需 Redis） |
| `FeishuMcp:Http:*` | 配置文件 | HTTP 传输判据（Origin 白名单 / 会话上限 / 逐请求绑租户开关），见 §2 |

## 2. Streamable HTTP 传输部署（E-3）

> 判据与安全裁定的来源：`documents/AIAgent/decisions/ADR-0008-mcp-http-transport.md`。
> 本节只讲**怎么部署**与**出问题时看哪一条**。

### 2.1 何时用 HTTP，何时仍用 stdio

| 形态 | 适用 | 租户边界 |
| --- | --- | --- |
| **stdio** | 桌面端 / 单客户端 harness / Native AOT 单文件冷启动 | 进程（`FeishuMcp:AppKey` 必填） |
| **Streamable HTTP** | 远端共享一个服务、多个调用方、需要网关或负载均衡 | **宿主认证层逐请求给出的身份** |

HTTP 是**新增形态，不是替代**：stdio 的既有语义（逐行顺序处理、进程级租户、stdout 即协议通道）逐条不变。
本包**不引入任何 Web 框架依赖**——端点映射由宿主完成（最小 API / MVC / FastEndpoints / 云函数皆可适配）。

### 2.2 端点映射与配置

```csharp
builder.Services.AddFeishuTools();
builder.Services.AddFeishuMcpServer(o =>
{
    o.AppKey = "cli_xxx";                                        // 单租户 HTTP 部署：与 stdio 同一形态
    o.Http.AllowedOrigins.Add("https://mcp.example.com");        // 只在确有浏览器客户端时添加
    o.Http.AllowedAppKeys.Add("cli_xxx");                        // 纵深防御名单（可留空）
});

app.MapPost("/mcp", async (HttpContext ctx, FeishuMcpHttpHandler handler, CancellationToken ct) =>
{
    var request = new McpHttpRequest("POST")
    {
        AppKey = ctx.User.FindFirst("feishu_app_key")?.Value,    // 唯一身份来源：认证层
        UserId = ctx.User.FindFirst("feishu_user_id")?.Value,
        SessionId = ctx.Request.Headers["Mcp-Session-Id"],
        ProtocolVersion = ctx.Request.Headers["MCP-Protocol-Version"],
        Origin = ctx.Request.Headers.Origin,
        ContentType = ctx.Request.ContentType,
        Accept = ctx.Request.Headers.Accept,
        Body = await new StreamReader(ctx.Request.Body).ReadToEndAsync(ct),
    };
    var response = await handler.HandleAsync(request, ct);
    ctx.Response.StatusCode = response.StatusCode;
    foreach (var (name, value) in response.Headers) { ctx.Response.Headers[name] = value; }
    if (response.Body is not null)
    {
        ctx.Response.ContentType = response.ContentType;
        await ctx.Response.WriteAsync(response.Body, ct);
    }
});
// DELETE 形态同上，仅动词换为 "DELETE" 且通常只写状态码（204 无正文）
```

配置节（`FeishuMcp:Http`，全部可选，默认值即安全默认）：

| 键 | 默认 | 语义 |
| --- | --- | --- |
| `AllowedOrigins` | 空 | **空清单 = 拒绝一切带 `Origin` 的请求**（DNS 重绑定 / 跨源页面防线）。服务端 Agent 与 CLI 不发 `Origin`，故默认形态对它们零影响 |
| `AllowedAppKeys` | 空 | 认证给出的 appKey 的第二道名单（兜底「认证服务被误配成放行全部」），**不是身份来源** |
| `TenantBoundPerRequest` | `false` | 为 `true` 才允许 `AppKey` 留空、逐请求换租户（一个进程服务多租户）。**打开它 = 宿主明确承担跨租户判定**，不该由「接了个端点」隐式发生 |
| `MaxSessions` | 64 | 进程内并发驻留会话上限，超限回 503（防握手风暴，不是业务配额） |
| `SessionIdleTimeoutSeconds` | 900 | 空闲会话在下一次访问或计数检查时**惰性**回收，不起后台线程 |

本节**没有「是否启用 HTTP」开关**：通道对外是否存在，由宿主「映射不映射那个端点」决定。再加一个布尔开关就出现
「配置说开着、实际没接线」的第二真相源。

### 2.3 会话生命周期

1. 客户端 `POST initialize` → 服务端创建会话，响应头回 `Mcp-Session-Id`（32 位随机十六进制），并绑定
   **该请求的租户身份**与协商后的协议版本；
2. 后续每条请求必须带同一个 `Mcp-Session-Id`，且认证层必须给出**同一个** appKey，否则 403；
3. `DELETE /mcp`（带会话头）终止会话 → 204；会话标识未知 → 404；
4. 未完成握手就发 `tools/call` → 400（协议层要求先 `initialize`）；
5. 空闲超过 `SessionIdleTimeoutSeconds` 的会话被回收，客户端随后收到 404 → 应重新握手
   （**这是预期行为，不是服务端缺陷**）。

**多实例部署**：会话目录在进程内存，因此要么做**会话粘滞**（同一 `Mcp-Session-Id` 落到同一实例），
要么把 `MaxSessions` 理解为**单实例**上限（N 个实例即 N 倍驻留量）。跨实例共享会话不在本层职责内——
协议会话只承载握手标记与租户绑定，把它外置只会多一份需要失效的缓存。

### 2.4 HTTP 状态码对照（传输层拒绝）

| 状态码 | 触发 | 处置 |
| --- | --- | --- |
| `401` | 认证层没给出 appKey（空白） | 检查端点是否映射在认证之后；**不要**改为从 URL 或正文读租户 |
| `403` | 身份不在 `AllowedAppKeys`；或 `TenantBoundPerRequest=false` 而身份 ≠ 配置的 `AppKey`；或会话已绑定别的租户；或带 `Origin` 而白名单为空 | 逐条对照 §2.2 的四个键；`Origin` 拒绝的文案**不回显来源值**（避免把请求内容反射进响应） |
| `405` | `GET`（**本实现不做 SSE**） | 响应带 `Allow: POST, DELETE`；客户端坚持 `GET` / `text/event-stream` 说明它按旧 SSE 传输配置 |
| `406` | `Accept` 不接受 `application/json` | 修正客户端 Accept；`*/*` 放行，**只声明 `text/event-stream` 一律 406**（本实现不做 SSE，返回 JSON 会违反客户端声明） |
| `415` | `Content-Type` 不是 `application/json` | 常见于表单提交或漏设头 |
| `400` | 正文为空 / 非 JSON / 非对象 / 批量请求 / 缺会话标识 / `MCP-Protocol-Version` 头与协商结果不一致 | 批量请求（JSON 数组）**明确不支持**，请逐条发送 |
| `404` | `Mcp-Session-Id` 未知或已被回收 | 客户端重新 `initialize` |
| `503` | 会话数达 `MaxSessions` | 提高上限或缩短空闲阈值；同时排查「客户端崩溃重连风暴」 |
| `202` | 通知（无 `id`，如 `notifications/initialized`） | 正常，无响应正文 |
| `204` | `DELETE` 成功终止会话 | 正常，无响应正文 |

> **两类错误形态刻意分开**：JSON-RPC 层错误（方法不存在、参数非法、工具被执行链拒绝）仍是 `200` +
> `{ "error": { "code", "message" } }`——那是给客户端程序读的协议载荷；传输层拒绝是 4xx +
> `{ "error": "<原因>" }`——HTTP 语义不该伪装成一次合法的 JSON-RPC 往返。

### 2.5 安全红线（部署时不得放宽）

- **租户身份只能来自认证层**：本包绝不从 URL 路径、查询串、请求体或任意自定义头推导租户。
  把它「顺手」改成从 header 取租户，等于把切租户的凭据交给客户端——协议参数里的租户标识可被模型
  （乃至提示注入）影响。
- **`Origin` fail-closed**：需要浏览器客户端时逐条加白名单，**不要**图省事塞通配项或绕过该校验。
- **请求体大小上限归宿主**：那是 Web 层职责（Kestrel / 网关配置），协议层不设第二道上限。
- **不做 SSE、不做批量**：这是明确取舍，不是未完成。若客户端确实需要服务端推送，那是翻转「自研协议层」
  这条决策的信号，请回到 ADR-0008 与 MCP 依赖选型记录走决策流程，而不是在本层临时补一条流。
- 工具面白名单、写工具 HITL、出站净化与审计**全部不变**：HTTP 只多了一层传输与会话判定，
  新增判据全部是**拒绝**形态——不合格请求宁可 4xx，也不会被交给执行链。

## 3. 写工具 HITL 语义对照表

### 3.1 两条路径

| 路径 | 机制 | HITL 实现 |
| --- | --- | --- |
| **进程内 Agent** | MAF `FunctionInvokingChatClient` + `ApprovalRequiredAIFunction` | MAF 审批管线：模型→`ToolApprovalRequestContent`→宿主通道→`ApprovalResponse`→继续执行 |
| **MCP** | 执行链授权器 `FeishuToolBinding` + `IFeishuToolApprovalChannel` | `NeedsUserConfirmation` → 中性拒绝（`needs_user_confirmation`）+ 宿主通道 |

### 3.2 效果差异

| 维度 | 进程内 Agent | MCP |
| --- | --- | --- |
| 批准来源 | MAF `ApprovalResponseBindingChatClient` 只接受框架发出的请求绑定 | 宿主经 `IFeishuToolApprovalChannel` 确认 |
| 模型自批复 | 不可（框架绑定） | 不可（执行链拒绝） |
| 续跑机制 | `RunApprovalContinuationAsync` 用历史中的 `ToolApprovalResponseContent` 续跑 | MCP 客户端发新 `tools/call` 请求 |

### 3.3 MCP 通道缺失时

当 `IFeishuToolApprovalChannel` 未注册时，写工具的 `NeedsUserConfirmation` 退化为纯提示——工具**不执行**（fail-closed），返回 `[tool_error] ... (needs_user_confirmation) ...`，但**不阻断**模型后续动作。

## 4. stdio 线协议排障清单

### 4.1 stdout 污染

**症状**：MCP 客户端报 JSON 解析错误。

**原因**：stdout 输出了非 JSON-RPC 2.0 消息（如日志、`Console.WriteLine`）。

**排查**：
1. 确认所有日志配置走 `stderr`（`Logging:LogLevel:*` 不影响 stdout）；
2. 确认无 `Console.WriteLine` 残留（MCP Server 包中全局搜索）；
3. 用 `2>err.log` 分离 stderr 验证 stdout 纯净。

**正确形态**（宿主侧配置；本包不配置任何日志 provider）：

```csharp
// net8+：控制台 provider 的全部级别导向 stderr
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
```

更低 TFM 或使用第三方 provider 时，改用文件日志 / Serilog 等**不写 stdout** 的出口
（参照 `Demos/Mud.Feishu.AI.CapabilityDemo/Mcp/McpStdioRunner.cs`：MCP 形态以文件日志运行）。
判据不变：**丢弃 stderr 后，stdio 会话期间 stdout 必须只有 JSON-RPC 行**。

### 4.2 Flush / 缓冲

**症状**：MCP 客户端收不到响应（超时）。

**原因**：stdout 被缓冲，未及时 Flush。

**排查**：
1. 确认 MCP Server 使用 `Console.Out` 时设置了 `AutoFlush = true`；
2. 确认 JSON-RPC 响应写入后调用 `Flush()`；
3. .NET 的 `Console.Out` 默认行缓冲，换行后应自动 Flush——检查是否有不换行的写入。

### 4.3 EOF 终止

**症状**：MCP 客户端断开后 Server 进程不退出。

**原因**：stdin EOF 未正确处理。

**排查**：
1. 确认 Server 监听 `Console.In` 的 EOF（`ReadLine` 返回 `null`）并优雅退出；
2. 确认 `CancellationToken` 正确链接到 stdin EOF 信号。

## 5. Demo 模式

参照实现在 `Demos/Mud.Feishu.AI.CapabilityDemo`，两种形态：

```bash
# ① stdio 运行（可直接挂到 MCP 客户端；日志走文件，stdout 只有 JSON-RPC 行）
dotnet run --project Demos/Mud.Feishu.AI.CapabilityDemo -- --act mcp

# ② 脚本化客户端全链路自检（零网络、零真实客户端：握手 → tools/list → resources 读取）
dotnet run --project Demos/Mud.Feishu.AI.CapabilityDemo -- --verify
```

- ① 的承载类是 `Mcp/McpStdioRunner.cs`（`AddFeishuMcpServer` + `RunFeishuMcpStdioAsync`，以 `consoleAllowed: false` 运行）；
- ② 的 MCP 部分在 `SelfCheck/McpProtocolChecks.cs`，用字符串读写器驱动完整会话，用于验证工具暴露面与配置面可用性。

**装配自检**：工具面为空时本包在构造期输出一条 `MCP 工具面为空（<成因>）` 告警并点名
`FeishuAgent:Tools` / `WriteAllowList`（成因：`registry_absent` / `factory_absent` / `whitelist_empty`）；
非空时输出一条 `MCP 工具面就绪：tools=… domains=…` 读数。排障先看这两行。

## 6. `tools/call` 结果形态与进度通道（决策索引）

- **只回 text content，不产出 `structuredContent` / `outputSchema`**：执行链结果是"错误契约 JSON 行 +
  `[next]` 行 + 正文"的文本协议，且经 JSON 感知截断（截断后非合法 JSON），发出结构化内容会误导客户端。
- **不实现 `notifications/progress`**：需把进度管道穿透工具执行链（跨中立包），当前工具结果有界、HTTP 中断已覆盖取消。

两项均为明确决策而非未完成，理由与边界见 `documents/AIAgent/decisions/ADR-0009-mcp-output-and-progress-scope.md`。
