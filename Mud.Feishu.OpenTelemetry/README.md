# Mud.Feishu.OpenTelemetry

[![NuGet](https://img.shields.io/nuget/v/Mud.Feishu.OpenTelemetry.svg)](https://www.nuget.org/packages/Mud.Feishu.OpenTelemetry/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE-MIT)

Mud.Feishu OpenTelemetry 适配包，一键开启飞书 SDK 的分布式追踪与指标采集。

## 项目简介

`Mud.Feishu.OpenTelemetry` 是 MudFeishu SDK 的可观测性扩展，通过一行代码自动注册飞书 SDK 的 ActivitySource 和 Meter，实现分布式追踪（Tracing）、指标采集（Metrics）和日志导出（Logging）。基于 OpenTelemetry .NET SDK 构建，支持 OTLP gRPC 导出，可与 Jaeger、Tempo、Prometheus、Grafana 等主流可观测性平台无缝集成。

## 特性

- ✅ **一键启用** - 一行代码完成飞书 SDK 全链路可观测性配置
- ✅ **自动注册** - 自动注册 `Mud.Feishu` 和 `Mud.HttpUtils` 的 ActivitySource 和 Meter
- ✅ **追踪（Tracing）** - 飞书事件处理、Webhook、WebSocket、HTTP 出站请求全链路追踪
- ✅ **指标（Metrics）** - Token 缓存、事件处理、HTTP 请求、WebSocket 连接等指标采集
- ✅ **日志导出** - 可选的 OTLP 日志导出
- ✅ **采样策略** - 支持 ParentBased + TraceIdRatioBased 采样
- ✅ **资源标签** - 自动添加 `service.name`、`service.version`、`deployment.environment` 标签
- ✅ **配置验证** - 内置 `IValidateOptions` 验证，支持 `ValidateOnStart`
- ✅ **灵活扩展** - 提供自定义配置委托，可追加/覆盖默认配置
- ✅ **多框架支持** - 支持 .NET Standard 2.0、.NET 6.0、.NET 8.0、.NET 10.0

## 安装

```bash
dotnet add package Mud.Feishu.OpenTelemetry
```

## 快速开始

### 方式一：代码配置（推荐）

```csharp
using Mud.Feishu.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

// 一键开启飞书 SDK 的 OpenTelemetry 追踪与指标采集
builder.Services.AddFeishuOpenTelemetry(options =>
{
    options.OtlpEndpoint = new Uri("http://otel-collector:4317");
    options.ServiceName = "my-feishu-app";
    options.SamplingRatio = 0.1; // 生产环境建议 0.1~0.3
});

var app = builder.Build();
app.Run();
```

### 方式二：从配置文件绑定

```json
{
  "FeishuOpenTelemetry": {
    "ServiceName": "my-feishu-app",
    "ServiceVersion": "1.0.0",
    "DeploymentEnvironment": "production",
    "SamplingRatio": 0.1,
    "OtlpEndpoint": "http://otel-collector:4317",
    "EnableTracing": true,
    "EnableMetrics": true,
    "EnableLogging": false,
    "IncludeMudHttpUtils": true,
    "EnableHttpClientInstrumentation": true,
    "EnableAspNetCoreInstrumentation": true
  }
}
```

```csharp
builder.Services.AddFeishuOpenTelemetry(builder.Configuration);
```

## 自动注册的可观测性源

### ActivitySource（追踪）

| ActivitySource | 说明 |
| --- | --- |
| `Mud.Feishu` | 飞书事件处理、Webhook、WebSocket 追踪 |
| `Mud.HttpUtils.HttpClient` | HTTP 出站请求、Token 刷新、重试追踪（可选，默认开启） |

### Meter（指标）

| Meter | 说明 |
| --- | --- |
| `Mud.Feishu` | 事件处理、WebSocket 连接、Webhook 指标 |
| `Mud.HttpUtils` | HTTP 请求、Token 刷新、重试、熔断器、下载指标（可选，默认开启） |

### 采集的指标列表

| 指标名称 | 类型 | 说明 |
| --- | --- | --- |
| `feishu_token_fetch_total` | Counter | 令牌获取总次数 |
| `feishu_token_cache_hit_total` | Counter | 令牌缓存命中次数 |
| `feishu_token_cache_miss_total` | Counter | 令牌缓存未命中次数 |
| `feishu_token_refresh_total` | Counter | 令牌刷新次数 |
| `feishu_cached_tokens` | ObservableGauge | 当前缓存的令牌数 |
| `feishu_event_handling_total` | Counter | 事件处理总次数 |
| `feishu_event_handling_success_total` | Counter | 事件处理成功次数 |
| `feishu_event_handling_failure_total` | Counter | 事件处理失败次数 |
| `feishu_event_handling_duration_ms` | Histogram | 事件处理持续时间（毫秒） |
| `feishu_http_request_total` | Counter | HTTP 请求总次数 |
| `feishu_http_request_duration_ms` | Histogram | HTTP 请求持续时间（毫秒） |
| `feishu_websocket_connections` | ObservableGauge | WebSocket 连接数 |

## 配置选项

| 配置项 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `EnableTracing` | bool | true | 是否启用追踪 |
| `EnableMetrics` | bool | true | 是否启用指标 |
| `EnableLogging` | bool | false | 是否启用 OTLP 日志导出 |
| `IncludeMudHttpUtils` | bool | true | 是否同时注册 Mud.HttpUtils 的 ActivitySource 和 Meter |
| `EnableHttpClientInstrumentation` | bool | true | 是否启用 .NET HttpClient Instrumentation |
| `EnableAspNetCoreInstrumentation` | bool | true | 是否启用 ASP.NET Core 入站请求 Instrumentation |
| `OtlpEndpoint` | Uri? | `http://localhost:4317` | OTLP 导出端点，设为 null 则不配置 OTLP 导出器 |
| `ServiceName` | string | `Mud.Feishu.Application` | 服务名称（OTel Resource） |
| `ServiceVersion` | string | SDK 版本 | 服务版本（OTel Resource） |
| `DeploymentEnvironment` | string | `production` | 部署环境（OTel Resource） |
| `SamplingRatio` | double | 1.0 | 采样比率（0.0~1.0），生产环境建议 0.1~0.3 |
| `ConfigureTracing` | Action? | null | 自定义追踪配置委托 |
| `ConfigureMetrics` | Action? | null | 自定义指标配置委托 |
| `ConfigureLogging` | Action? | null | 自定义日志配置委托 |

## 高级用法

### 自定义导出器

```csharp
builder.Services.AddFeishuOpenTelemetry(options =>
{
    // 不使用默认 OTLP 导出，自定义配置
    options.OtlpEndpoint = null;
    options.SamplingRatio = 1.0;

    // 自定义追踪配置：追加 Console 导出器
    options.ConfigureTracing = tp => tp.AddConsoleExporter();

    // 自定义指标配置：追加 Prometheus 导出器
    options.ConfigureMetrics = mp => mp.AddPrometheusExporter();
});
```

### 仅启用飞书指标（不含 HTTP 追踪）

```csharp
builder.Services.AddFeishuOpenTelemetry(options =>
{
    options.EnableTracing = false;
    options.IncludeMudHttpUtils = false;
    options.EnableHttpClientInstrumentation = false;
    options.EnableAspNetCoreInstrumentation = false;
});
```

### 配合 ASP.NET Core 启动验证

```csharp
builder.Services.AddFeishuOpenTelemetry(builder.Configuration);

var builder2 = WebApplication.CreateBuilder(args);

var app = builder2.Build();

// 启用配置验证（启动时检查 FeishuOpenTelemetryOptions 有效性）
app.Services.GetRequiredService<IOptions<FeishuOpenTelemetryOptions>>()
    .Value.Validate(null, null);

app.Run();
```

## 依赖项

| 包 | 版本 | 说明 |
| --- | --- | --- |
| **Mud.Feishu.Abstractions** | * | 飞书 SDK 抽象层（提供 ActivitySource 和 Meter 定义） |
| **OpenTelemetry** | 1.16.0 | OpenTelemetry .NET SDK |
| **OpenTelemetry.Extensions.Hosting** | 1.16.0 | 主机集成 |
| **OpenTelemetry.Exporter.OpenTelemetryProtocol** | 1.16.0 | OTLP 导出器 |
| **OpenTelemetry.Instrumentation.Http** | 1.16.0 | HttpClient Instrumentation |
| **OpenTelemetry.Instrumentation.AspNetCore** | 1.16.0 | ASP.NET Core Instrumentation |

## 框架支持

- .NET Standard 2.0
- .NET 6.0
- .NET 8.0
- .NET 10.0

## 相关项目

- [Mud.Feishu.Abstractions](../Mud.Feishu.Abstractions) - 事件处理抽象层（提供 ActivitySource 和 Meter 定义）
- [Mud.Feishu](../Mud.Feishu) - 核心 HTTP API 客户端库
- [Mud.Feishu.WebSocket](../Mud.Feishu.WebSocket) - WebSocket 实时事件订阅
- [Mud.Feishu.Webhook](../Mud.Feishu.Webhook) - Webhook HTTP 回调事件处理

## 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](../LICENSE) 文件

---

**Mud.Feishu.OpenTelemetry** - 一键开启飞书 SDK 全链路可观测性！
