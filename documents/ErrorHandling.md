# 错误处理指南

本文说明 Mud.Feishu SDK 的错误通道设计，重点覆盖**文件下载类接口**（`Task<byte[]?>`）这一
与其余接口语义不同的特例。

## 1. 统一响应模型（绝大多数接口）

除文件下载外，所有接口返回 `FeishuApiResult<T>?`（或 `FeishuApiListResult<T>?` /
`FeishuApiPageListResult<T>?` / `FeishuNullDataApiResult?`）：

```csharp
var result = await _messageApi.SendMessageAsync(request, "open_id");
if (result is { Code: 0 })
{
    // 成功
}
else
{
    // 业务错误：result.Code / result.Msg
}
```

注意：**HTTP 200 且 `Code != 0` 是飞书最常见的业务错误形态**，此时不会抛异常，
必须显式检查 `Code`。

## 2. 传输层错误

服务端返回非 2xx 状态码时，HTTP 执行器统一抛出 `Mud.HttpUtils.ApiException`：

| 属性 | 说明 |
| --- | --- |
| `StatusCode` | HTTP 状态码（401 / 403 / 404 / 429 / 5xx 等） |
| `Content` | 响应体内容（已按 `MaxExceptionContentLength` 截断；敏感内容经 `IExceptionRedactor` 擦除） |
| `RequestUri` | 请求地址（已脱敏） |
| `RequestContent` | 捕获的请求体（仅在启用 `CaptureRequestContent` 时填充） |

```csharp
try
{
    var result = await _userApi.GetUserAsync("ou_xxx");
}
catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
{
    // 触发飞书限流：按 Retry-After 退避重试
}
```

401 由 SDK 自动恢复：应用上下文中的 `TokenRecoveryEnhancedClient` 会刷新令牌并重试一次，
业务代码通常无需处理（仅在自行调用认证接口获取令牌的场景下需要留意）。

## 3. 文件下载类接口（`Task<byte[]?>`）

以下 9 个方法直接返回二进制内容，**不经过 `FeishuApiResult<T>`**，因此其错误语义需要单独说明：

| 模块 | 方法 |
| --- | --- |
| `IFeishuV1Message_Tenant` | `GetMessageFile`、`DownFileAsync`、`DownImageAsync` |
| `IFeishuV1DriveFiles` | `DownloadFileAsync`、`DownloadExportFileAsync` |
| `IFeishuV1DriveMedia` | `DownloadFileAsync` |
| `IFeishuV1Board` | `DownloadWhiteboardImageAsync` |
| `IFeishuV1VideoConferencingExports` | `DownloadExportAsync` |
| `IFeishuV1AttendanceUserSettings_Tenant` | `DownloadFileAsync` |
| `IFeishuV1HelpDeskTicket_Tenant` | `GetTicketImageAsync` |

**行为契约（已由 `Tests/Mud.Feishu.Tests/Http/DownloadErrorSemanticsTests.cs` 锁定）**

1. 服务端返回 **非 2xx** → 抛 `ApiException`（`Content` 为错误响应体）。
   调用方无需区分「失败」与「成功但内容为空」，前者是异常。
2. 服务端返回 **2xx** → 原样返回响应体字节（可空签名，实际不会为 `null`；空响应体对应空数组）。
3. **残余风险：HTTP 200 + JSON 错误体**。飞书部分业务错误以 `HTTP 200`
   配合 `{"code":99991672,"msg":"..."}` 返回，此时本方法会把**错误 JSON 的字节**当作文件内容返回。
   执行器不做内容嗅探（无法在不破坏二进制语义的前提下区分），因此需要调用方自检：

```csharp
var bytes = await _driveFilesApi.DownloadFileAsync(fileToken);
if (bytes is null || bytes.Length == 0)
{
    throw new InvalidOperationException("下载内容为空");
}

// 低价自检：错误响应体总是 JSON，且以 '{' 开头
if (bytes.Length > 1 && bytes[0] == (byte)'{')
{
    var errorJson = System.Text.Encoding.UTF8.GetString(bytes);
    throw new InvalidOperationException($"下载失败，服务端返回错误体：{errorJson}");
}

await File.WriteAllBytesAsync(localPath, bytes);
```

> 若需要更严格的判定，可改用 `IFeishuV1DriveFiles` 等接口上形如
> `Task<FeishuApiResult<...>?>` 的元数据接口先校验文件是否存在 / 是否有权限，
> 再下载内容。

## 4. 异常类型速查

| 异常 | 触发场景 |
| --- | --- |
| `ApiException` | 非 2xx 状态码；JSON/XML 反序列化失败 |
| `ApiRequestException` | 传输层失败（DNS / TLS / 连接被拒），或成功响应体超出 `MaxSuccessResponseBytes` |
| `TaskCanceledException` | 请求超时（`HttpClient.Timeout` 或弹性策略超时）或调用方取消 |
| `InvalidOperationException` | 配置缺失（如使用 `[Body(EnableEncrypt=true)]` 但未注册 `IEncryptionProvider`） |

## 5. 相关配置

| 配置项 | 作用 |
| --- | --- |
| `FeishuAppConfig.TimeOut` | 命名客户端超时（秒），支持配置热更新 |
| `FeishuAppConfig.RetryCount` / `RetryDelayMs` | 弹性重试（指数退避） |
| `FeishuAppConfig.CircuitBreaker*` | 熔断策略 |
| `EnhancedHttpClientOptions.MaxSuccessResponseBytes` | 成功响应体上限（防 OOM，0 = 不限制） |
| `EnhancedHttpClientOptions.MaxExceptionContentLength` | 异常内容截断长度 |
| `EnhancedHttpClientOptions.CaptureRequestContent` | 是否捕获请求体用于诊断（含敏感数据，慎用） |
