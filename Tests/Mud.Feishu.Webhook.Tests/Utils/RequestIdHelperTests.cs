// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Mud.Feishu.Webhook.Utils;

namespace Mud.Feishu.Webhook.Tests.Utils;

public class RequestIdHelperTests
{
    [Fact]
    public void GetOrGenerateRequestId_WithFeishuRequestId_ReturnsFeishuRequestId()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Request-Id"] = "feishu-request-id-123";
        httpContext.TraceIdentifier = "aspnet-trace-id";

        var requestId = RequestIdHelper.GetOrGenerateRequestId(httpContext);

        Assert.Equal("feishu-request-id-123", requestId);
        Assert.Equal("feishu-request-id-123", httpContext.Items[RequestIdHelper.RequestIdItemKey]);
    }

    [Fact]
    public void GetOrGenerateRequestId_WithXTraceId_ReturnsTraceId()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Trace-Id"] = "trace-id-456";
        httpContext.TraceIdentifier = "aspnet-trace-id";

        var requestId = RequestIdHelper.GetOrGenerateRequestId(httpContext);

        Assert.Equal("trace-id-456", requestId);
    }

    [Fact]
    public void GetOrGenerateRequestId_WithXCorrelationId_ReturnsCorrelationId()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Correlation-Id"] = "correlation-id-789";
        httpContext.TraceIdentifier = "aspnet-trace-id";

        var requestId = RequestIdHelper.GetOrGenerateRequestId(httpContext);

        Assert.Equal("correlation-id-789", requestId);
    }

    [Fact]
    public void GetOrGenerateRequestId_WithXB3TraceId_ReturnsTraceId()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-B3-TraceId"] = "b3-trace-id-abc";
        httpContext.TraceIdentifier = "aspnet-trace-id";

        var requestId = RequestIdHelper.GetOrGenerateRequestId(httpContext);

        Assert.Equal("b3-trace-id-abc", requestId);
    }

    [Fact]
    public void GetOrGenerateRequestId_WithXCloudTraceContext_ReturnsTraceId()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Cloud-Trace-Context"] = "cloud-trace-id-xyz/12345;o=1";
        httpContext.TraceIdentifier = "aspnet-trace-id";

        var requestId = RequestIdHelper.GetOrGenerateRequestId(httpContext);

        Assert.Equal("cloud-trace-id-xyz", requestId);
    }

    [Fact]
    public void GetOrGenerateRequestId_WithTraceparent_ReturnsTraceId()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Traceparent"] = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";
        httpContext.TraceIdentifier = "aspnet-trace-id";

        var requestId = RequestIdHelper.GetOrGenerateRequestId(httpContext);

        Assert.Equal("4bf92f3577b34da6a3ce929d0e0e4736", requestId);
    }

    [Fact]
    public void GetOrGenerateRequestId_WithMultipleHeaders_ReturnsFirstValid()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Request-Id"] = "feishu-id";
        httpContext.Request.Headers["X-Trace-Id"] = "trace-id";
        httpContext.TraceIdentifier = "aspnet-trace-id";

        var requestId = RequestIdHelper.GetOrGenerateRequestId(httpContext);

        Assert.Equal("feishu-id", requestId);
    }

    [Fact]
    public void GetOrGenerateRequestId_WithoutTraceHeaders_ReturnsTraceIdentifier()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "aspnet-trace-id";

        var requestId = RequestIdHelper.GetOrGenerateRequestId(httpContext);

        Assert.Equal("aspnet-trace-id", requestId);
    }

    [Fact]
    public void GetOrGenerateRequestId_WithExistingRequestId_ReturnsExisting()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items[RequestIdHelper.RequestIdItemKey] = "existing-id";
        httpContext.Request.Headers["X-Request-Id"] = "new-id";

        var requestId = RequestIdHelper.GetOrGenerateRequestId(httpContext);

        Assert.Equal("existing-id", requestId);
    }

    [Fact]
    public void AddRequestIdToResponse_AddsHeader_WhenResponseNotStarted()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items[RequestIdHelper.RequestIdItemKey] = "test-request-id";
        httpContext.Response.Headers.Clear();

        RequestIdHelper.AddRequestIdToResponse(httpContext);

        Assert.Equal("test-request-id", httpContext.Response.Headers["X-Request-Id"].FirstOrDefault());
    }

    [Fact]
    public void AddRequestIdToResponse_DoesNotAddHeader_WhenResponseAlreadyStarted()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items[RequestIdHelper.RequestIdItemKey] = "test-request-id";
        httpContext.Response.Headers.Clear();
        httpContext.Response.StatusCode = 200;
        httpContext.Response.Headers["Content-Type"] = "text/plain";

        RequestIdHelper.AddRequestIdToResponse(httpContext);
    }

    [Fact]
    public void AddRequestIdToResponse_WithoutRequestId_DoesNothing()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Headers.Clear();

        RequestIdHelper.AddRequestIdToResponse(httpContext);

        Assert.Equal(0, httpContext.Response.Headers["X-Request-Id"].Count);
    }
}
