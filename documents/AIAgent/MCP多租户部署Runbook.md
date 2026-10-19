# MCP 多租户部署 Runbook

> 本文档适用于 `Mud.Feishu.AI.Mcp` 包的运维与部署。

## 1. 部署模型

### 1.1 进程 = 租户边界

MCP（Model Context Protocol）采用 stdio 线协议，每个 MCP Server 实例是一个独立进程。飞书 MCP 部署遵循以下原则：

| 原则 | 说明 |
| --- | --- |
| **进程 = 租户** | 每个租户一个进程实例，进程间无共享状态 |
| **配置隔离** | 每个进程独立读取自己的 `FeishuAppConfig`（AppKey/AppSecret） |
| **日志走 stderr** | stdio 线协议禁止 stdout 输出非 JSON-RPC 内容（见 §3） |

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

### 1.4 HTTP 部署模型（R-7 起）

除 stdio 外，`Mud.Feishu.AI.Mcp` 自 R-7 起提供 HTTP 传输宿主 `FeishuMcpHttpHost`（基于 `HttpListener`，四 TFM 统一、零新依赖）。HTTP 形态适用于**单进程多租户**场景——按 `appKey` 路由而非按进程隔离。

| 维度 | stdio | HTTP |
| --- | --- | --- |
| 隔离边界 | 进程 = 租户 | appKey = 租户（请求体携带） |
| 会话状态 | 有状态（initialize 握手门） | 无状态（每请求自洽，跳过握手门） |
| 部署形态 | 每租户一个进程 | 单进程监听一个前缀 |
| 适用场景 | 桌面 Agent / CLI | 服务化 / 远程 Agent |

**HTTP 部署清单**：

```bash
# 单进程多租户：监听 http://+:8080/mcp
dotnet Mud.Feishu.AI.Mcp.dll --urls http://+:8080/mcp
```

配置（`appsettings.json`）：

```json
{
  "FeishuMcp": {
    "HttpPrefix": "http://+:8080/",
    "HttpPath": "/mcp",
    "MaxRequestBytes": 1048576
  }
}
```

**请求形态**：`POST /mcp`，body 为 JSON-RPC 2.0 单条对象；`tools/call` 的 `params` 须含 `_appKey` 字段以路由到对应租户上下文。响应为单条 JSON-RPC 2.0 对象（非 SSE 流——本实现是单请求单响应，不维持长连接）。

**安全注意**：
- `HttpPrefix` 生产环境应绑定 TLS 反向代理后的内网前缀，**不应直接暴露公网**；
- HTTP 形态不内置鉴权——由宿主在 `HttpListener` 前置网关层完成（如 mTLS、Bearer Token）；
- `MaxRequestBytes` 默认 1 MiB，超限返回 `413 Payload Too Large`。

## 2. 写工具 HITL 语义对照表

### 2.1 两条路径

| 路径 | 机制 | HITL 实现 |
| --- | --- | --- |
| **进程内 Agent** | MAF `FunctionInvokingChatClient` + `ApprovalRequiredAIFunction` | MAF 审批管线：模型→`ToolApprovalRequestContent`→宿主通道→`ApprovalResponse`→继续执行 |
| **MCP** | 执行链授权器 `FeishuToolBinding` + `IFeishuToolApprovalChannel` | `NeedsUserConfirmation` → 中性拒绝（`needs_user_confirmation`）+ 宿主通道 |

### 2.2 效果差异

| 维度 | 进程内 Agent | MCP |
| --- | --- | --- |
| 批准来源 | MAF `ApprovalResponseBindingChatClient` 只接受框架发出的请求绑定 | 宿主经 `IFeishuToolApprovalChannel` 确认 |
| 模型自批复 | 不可（框架绑定） | 不可（执行链拒绝） |
| 续跑机制 | `RunApprovalContinuationAsync` 用历史中的 `ToolApprovalResponseContent` 续跑 | MCP 客户端发新 `tools/call` 请求 |

### 2.3 MCP 通道缺失时

当 `IFeishuToolApprovalChannel` 未注册时，写工具的 `NeedsUserConfirmation` 退化为纯提示——工具**不执行**（fail-closed），返回 `[tool_error] ... (needs_user_confirmation) ...`，但**不阻断**模型后续动作。

## 3. stdio 线协议排障清单

### 3.1 stdout 污染

**症状**：MCP 客户端报 JSON 解析错误。

**原因**：stdout 输出了非 JSON-RPC 2.0 消息（如日志、`Console.WriteLine`）。

**排查**：
1. 确认所有日志配置走 `stderr`（`Logging:LogLevel:*` 不影响 stdout）；
2. 确认无 `Console.WriteLine` 残留（MCP Server 包中全局搜索）；
3. 用 `2>err.log` 分离 stderr 验证 stdout 纯净。

### 3.2 Flush / 缓冲

**症状**：MCP 客户端收不到响应（超时）。

**原因**：stdout 被缓冲，未及时 Flush。

**排查**：
1. 确认 MCP Server 使用 `Console.Out` 时设置了 `AutoFlush = true`；
2. 确认 JSON-RPC 响应写入后调用 `Flush()`；
3. .NET 的 `Console.Out` 默认行缓冲，换行后应自动 Flush——检查是否有不换行的写入。

### 3.3 EOF 终止

**症状**：MCP 客户端断开后 Server 进程不退出。

**原因**：stdin EOF 未正确处理。

**排查**：
1. 确认 Server 监听 `Console.In` 的 EOF（`ReadLine` 返回 `null`）并优雅退出；
2. 确认 `CancellationToken` 正确链接到 stdin EOF 信号。

## 4. HTTP 传输排障清单（R-7 起）

### 4.1 404 / 405

**症状**：客户端收到 `404 Not Found` 或 `405 Method Not Allowed`。

**原因**：路径不匹配 `HttpPath`（默认 `/mcp`），或用了非 POST 方法。

**排查**：确认请求为 `POST {HttpPrefix}{HttpPath}`（如 `POST http://+:8080/mcp`）。

### 4.2 413 Payload Too Large

**症状**：客户端收到 `413`。

**原因**：请求 body 超过 `MaxRequestBytes`（默认 1 MiB）。

**排查**：确认 `tools/call` 入参未夹带超大附件（附件应走 `attachments` 暂存通道，不入 JSON-RPC body）；按需调高 `FeishuMcp:MaxRequestBytes`。

### 4.3 租户上下文缺失

**症状**：`tools/call` 返回 `feishu.app_key_missing`（fail-closed）。

**原因**：HTTP 形态下 `params` 未携带 `_appKey`，或该 appKey 未在 `FeishuApps` 配置中登记。

**排查**：确认请求 body 的 `params._appKey` 与 `FeishuApps:0:AppKey` 一致；多租户时每个租户的 appKey 都需在配置中登记。

### 4.4 并发串号

**症状**：不同租户的请求交叉返回了错误租户的数据。

**原因**：`FeishuMcpHttpHost` 每请求独立创建 `FeishuMcpToolServer` 上下文，**不应**跨请求复用上下文实例。

**排查**：确认未在宿主层缓存 `FeishuMcpToolServer` 实例；`AddFeishuMcpHttpHost` 已将 host 注册为 Singleton，但每请求的 server 上下文是 Transient 语义。

## 5. Demo 模式

`Demos/Mud.Feishu.Agent.Demo` 支持 `FEISHU_DEMO_MCP=1` 模式：

```bash
# 在同一进程内起 stdio MCP Server + 打印 tools/list 摘要
FEISHU_DEMO_MCP=1 dotnet run --project Demos/Mud.Feishu.Agent.Demo
```

此模式用于验证 MCP Server 的工具暴露面与配置面可用性，不连接真实 MCP 客户端。
