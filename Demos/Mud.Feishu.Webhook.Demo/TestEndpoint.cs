// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Mud.Feishu.Webhook.Demo;

/// <summary>
/// 测试端点，用于捕获和分析飞书原始回调数据
/// </summary>
public static class TestEndpoint
{
    private static readonly List<CapturedRequest> _capturedRequests = new();
    private static readonly object _lock = new();

    /// <summary>
    /// 注册测试端点
    /// </summary>
    public static void MapTestEndpoints(this WebApplication app)
    {
        // 捕获原始请求端点
        app.MapPost("/test/capture", async (HttpContext context) =>
        {
            try
            {
                // 读取原始请求体
                context.Request.EnableBuffering();
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;

                // 捕获请求信息
                var captured = new CapturedRequest
                {
                    Timestamp = DateTime.Now,
                    Path = context.Request.Path,
                    Method = context.Request.Method,
                    Headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                    Body = body,
                    ContentType = context.Request.ContentType ?? "",
                    ClientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
                };

                lock (_lock)
                {
                    _capturedRequests.Add(captured);
                    // 只保留最近100个请求
                    if (_capturedRequests.Count > 100)
                    {
                        _capturedRequests.RemoveAt(0);
                    }
                }

                return Results.Ok(new CaptureResponse
                {
                    Message = "✅ 请求已捕获",
                    RequestId = captured.Timestamp.Ticks,
                    BodyLength = body.Length,
                    Preview = body.Length > 200 ? body.Substring(0, 200) + "..." : body
                });
            }
            catch (Exception ex)
            {
                return Results.Problem($"捕获请求失败: {ex.Message}");
            }
        }).WithName("CaptureRequest").WithTags("Test");

        // 查看捕获的请求列表
        app.MapGet("/test/captured", () =>
        {
            lock (_lock)
            {
                return Results.Ok(new CapturedRequestsResponse
                {
                    Total = _capturedRequests.Count,
                    Requests = _capturedRequests.OrderByDescending(r => r.Timestamp).Take(10).Select(r => new CapturedRequestSummary
                    {
                        Timestamp = r.Timestamp,
                        Method = r.Method,
                        Path = r.Path,
                        ClientIp = r.ClientIp,
                        BodyPreview = r.Body.Length > 100 ? r.Body.Substring(0, 100) + "..." : r.Body,
                        BodyLength = r.Body.Length
                    }).ToList()
                });
            }
        }).WithName("GetCapturedRequests").WithTags("Test");

        // 查看特定请求详情
        app.MapGet("/test/captured/{index:int}", (int index) =>
        {
            lock (_lock)
            {
                if (index < 0 || index >= _capturedRequests.Count)
                {
                    return Results.NotFound($"索引 {index} 超出范围 (0-{_capturedRequests.Count - 1})");
                }

                var request = _capturedRequests[_capturedRequests.Count - 1 - index]; // 最新的在前
                return Results.Ok(request);
            }
        }).WithName("GetCapturedRequestDetail").WithTags("Test");

        // 清空捕获的请求
        app.MapDelete("/test/captured", () =>
        {
            lock (_lock)
            {
                var count = _capturedRequests.Count;
                _capturedRequests.Clear();
                return Results.Ok(new ClearResponse { Message = $"已清空 {count} 个捕获的请求" });
            }
        }).WithName("ClearCapturedRequests").WithTags("Test");

        // 模拟飞书事件回调（用于测试）
        app.MapPost("/test/mock-feishu-event", ([FromBody] MockFeishuEvent mockEvent) =>
        {
            try
            {
                // 构造标准的飞书Webhook格式
                var feishuRequest = new MockFeishuRequest
                {
                    Encrypt = mockEvent.EncryptedData,
                    Timestamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString(),
                    Nonce = Guid.NewGuid().ToString("N"),
                    Signature = "mock_signature_" + Guid.NewGuid().ToString("N")
                };

                return Results.Ok(new MockFeishuEventResponse
                {
                    Message = "✅ 模拟飞书事件已生成",
                    FeishuRequest = feishuRequest,
                    Hint = "请将此数据POST到 /feishu/webhook 端点进行测试"
                });
            }
            catch (Exception ex)
            {
                return Results.Problem($"生成模拟事件失败: {ex.Message}");
            }
        }).WithName("MockFeishuEvent").WithTags("Test");
    }
}

/// <summary>
/// 捕获响应
/// </summary>
internal class CaptureResponse
{
    public string Message { get; set; } = string.Empty;
    public long RequestId { get; set; }
    public int BodyLength { get; set; }
    public string Preview { get; set; } = string.Empty;
}

/// <summary>
/// 捕获请求列表响应
/// </summary>
internal class CapturedRequestsResponse
{
    public int Total { get; set; }
    public List<CapturedRequestSummary> Requests { get; set; } = new();
}

/// <summary>
/// 捕获请求摘要
/// </summary>
internal class CapturedRequestSummary
{
    public DateTime Timestamp { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string ClientIp { get; set; } = string.Empty;
    public string BodyPreview { get; set; } = string.Empty;
    public int BodyLength { get; set; }
}

/// <summary>
/// 清空操作响应
/// </summary>
internal class ClearResponse
{
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 模拟飞书事件响应
/// </summary>
internal class MockFeishuEventResponse
{
    public string Message { get; set; } = string.Empty;
    public MockFeishuRequest FeishuRequest { get; set; } = new();
    public string Hint { get; set; } = string.Empty;
}

/// <summary>
/// 模拟飞书请求体
/// </summary>
internal class MockFeishuRequest
{
    public string Encrypt { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}

/// <summary>
/// 已捕获的请求（internal：AOT 下 RDG/源生成 JSON 需要可访问类型）
/// </summary>
internal class CapturedRequest
{
    public DateTime Timestamp { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public string Body { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string ClientIp { get; set; } = string.Empty;
}

/// <summary>
/// 模拟飞书事件请求体（internal：AOT 下 RDG 需要可访问类型，私有嵌套类会触发 RDG012）
/// </summary>
internal class MockFeishuEvent
{
    public string EncryptedData { get; set; } = string.Empty;
}
