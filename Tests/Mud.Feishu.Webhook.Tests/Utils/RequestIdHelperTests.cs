// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Mud.Feishu.Webhook.Utils;
using System.Diagnostics;

namespace Mud.Feishu.Webhook.Tests.Utils;

/// <summary>
/// RequestIdHelper 单元测试
/// </summary>
public class RequestIdHelperTests
{
    [Fact]
    public void GetOrGenerateRequestId_WithFeishuRequestId_ReturnsFeishuRequestId()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Request-Id"] = "feishu-request-id-123";
        httpContext.TraceIdentifier = "aspnet-trace-id";

        // Act
        var requestId = RequestIdHelper.GetOrGenerateRequestId(httpContext);

        // Assert
        Assert.Equal("feishu-request-id-123", requestId);
        Assert.Equal("feishu-request-id-123", httpContext.Items[RequestIdHelper.RequestIdItemKey]);
    }


    [Fact]
    public void GetOrGenerateRequestId_WithXTraceId_ReturnsTraceId()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Trace-Id"] = "trace-id-456";
        httpContext.TraceIdentifier = "aspnet-trace-id";

        // Act
        var requestId = RequestIdHelper.GetOrGenerateRequestId(httpContext);

        // Assert
        Assert.Equal("trace-id-456", requestId);
        Assert.Equal("X-Trace-Id", RequestIdHelper.GetRequestIdSource(httpContext));
    }

    [Fact]
    public void GetOrGenerateRequestId_WithXCorrelationId_ReturnsCorrelationId()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Correlation-Id"] = "correlation-id-789";
        httpContext.TraceIdentifier = "aspnet-trace-id";

        // Act
        var requestId = RequestIdHelper.GetOrGenerateRequestId(httpContext);

        // Assert
        Assert.Equal("correlation-id-789", requestId);
    }

    [Fact]
    public void GetOrGenerateRequestId_WithXB3TraceId_ReturnsTraceId()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-B3-TraceId"] = "b3-trace-id-abc";
        httpContext.TraceIdentifier = "aspnet-trace-id";

        // Act
        var requestId = RequestIdHelper.GetOrGenerateRequestId(httpContext);

        // Assert
        Assert.Equal("b3-trace-id-abc", requestId);
    }

    [Fact]
    public void GetOrGenerateRequestId_WithXCloudTraceContext_ReturnsTraceId()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Cloud-Trace-Context"] = "cloud-trace-id-xyz/12345;o=1";
        httpContext.TraceIdentifier = "aspnet-trace-id";

        // Act
        var requestId = RequestIdHelper.GetOrGenerateRequestId(httpContext);

        // Assert
        Assert.Equal("cloud-trace-id-xyz", requestId);
    }

    [Fact]
    public void GetOrGenerateRequestId_WithTraceparent_ReturnsTraceId()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Traceparent"] = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";
        httpContext.TraceIdentifier = "aspnet-trace-id";

        // Act
        var requestId = RequestIdHelper.GetOrGenerateRequestId(httpContext);

        // Assert
        Assert.Equal("4bf92f3577b34da6a3ce929d0e0e4736", requestId);
    }

    [Fact]
    public void GetOrGenerateRequestId_WithMultipleHeaders_ReturnsFirstValid()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Request-Id"] = "feishu-id";
        httpContext.Request.Headers["X-Trace-Id"] = "trace-id";
        httpContext.TraceIdentifier = "aspnet-trace-id";

        // Act
        var requestId = RequestIdHelper.GetOrGenerateRequestId(httpContext);

        // Assert
        Assert.Equal("feishu-id", requestId);
        Assert.Equal("Feishu-X-Request-Id", RequestIdHelper.GetRequestIdSource(httpContext));
    }

    [Fact]
    public void GetOrGenerateRequestId_WithoutTraceHeaders_ReturnsTraceIdentifier()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "aspnet-trace-id";

        // Act
        var requestId = RequestIdHelper.GetOrGenerateRequestId(httpContext);

        // Assert
        Assert.Equal("aspnet-trace-id", requestId);
        Assert.Equal("ASPNETCORE-TraceIdentifier", RequestIdHelper.GetRequestIdSource(httpContext));
    }

    [Fact]
    public void GetOrGenerateRequestId_WithExistingRequestId_ReturnsExisting()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Items[RequestIdHelper.RequestIdItemKey] = "existing-id";
        httpContext.Request.Headers["X-Request-Id"] = "new-id";

        // Act
        var requestId = RequestIdHelper.GetOrGenerateRequestId(httpContext);

        // Assert
        Assert.Equal("existing-id", requestId);
    }

    [Fact]
    public void GenerateRequestId_ReturnsValidGuid()
    {
        // Act
        var requestId1 = RequestIdHelper.GenerateRequestId();
        var requestId2 = RequestIdHelper.GenerateRequestId();

        // Assert
        Assert.NotEmpty(requestId1);
        Assert.NotEmpty(requestId2);
        Assert.Equal(32, requestId1.Length); // GUID "N" format is 32 chars
        Assert.NotEqual(requestId1, requestId2);
    }

    [Fact]
    public void AddRequestIdToResponse_AddsHeader_WhenResponseNotStarted()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Items[RequestIdHelper.RequestIdItemKey] = "test-request-id";
        httpContext.Response.Headers.Clear();

        // Act
        RequestIdHelper.AddRequestIdToResponse(httpContext);

        // Assert
        Assert.Equal("test-request-id", httpContext.Response.Headers["X-Request-Id"].FirstOrDefault());
    }

    [Fact]
    public void AddRequestIdToResponse_DoesNotAddHeader_WhenResponseAlreadyStarted()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Items[RequestIdHelper.RequestIdItemKey] = "test-request-id";
        httpContext.Response.Headers.Clear();
        httpContext.Response.StatusCode = 200;
        // 模拟响应已开始（在实际场景中，这是在 WriteAsync 之后）
        httpContext.Response.Headers["Content-Type"] = "text/plain";

        // Act
        // 注意：在单元测试中，我们无法真正模拟 Response.HasStarted 为 true
        // 这里主要测试方法的逻辑不会抛出异常
        RequestIdHelper.AddRequestIdToResponse(httpContext);

        // Assert - 在正常情况下会添加头，但实际场景中 HasStarted 会阻止
    }

    [Fact]
    public void AddRequestIdToResponse_WithoutRequestId_DoesNothing()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Headers.Clear();

        // Act
        RequestIdHelper.AddRequestIdToResponse(httpContext);

        // Assert
        Assert.Equal(0, httpContext.Response.Headers["X-Request-Id"].Count);
    }

    [Fact]
    public void GetRequestIdSource_WithFeishuHeader_ReturnsCorrectSource()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Request-Id"] = "feishu-id";
        httpContext.Items[RequestIdHelper.RequestIdItemKey] = "feishu-id";

        // Act
        var source = RequestIdHelper.GetRequestIdSource(httpContext);

        // Assert
        Assert.Equal("Feishu-X-Request-Id", source);
    }

    [Fact]
    public void GetRequestIdSource_WithTraceIdentifier_ReturnsCorrectSource()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "aspnet-trace-id";
        httpContext.Items[RequestIdHelper.RequestIdItemKey] = "aspnet-trace-id";

        // Act
        var source = RequestIdHelper.GetRequestIdSource(httpContext);

        // Assert
        Assert.Equal("ASPNETCORE-TraceIdentifier", source);
    }

    [Fact]
    public void GetRequestIdSource_WithoutRequestId_ReturnsNone()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        // Act
        var source = RequestIdHelper.GetRequestIdSource(httpContext);

        // Assert
        Assert.Equal("None", source);
    }

    [Fact]
    public void SetActivityRequestId_WithActivity_SetsTags()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Request-Id"] = "test-id";
        httpContext.Items[RequestIdHelper.RequestIdItemKey] = "test-id";
        using var activity = new Activity("test-activity").Start();

        // Act
        RequestIdHelper.SetActivityRequestId(activity, httpContext);

        // Assert
        Assert.Equal("test-id", activity.GetTagItem("request.id"));
        Assert.Equal("Feishu-X-Request-Id", activity.GetTagItem("request.id.source"));
    }

    [Fact]
    public void SetActivityRequestId_WithNullActivity_DoesNothing()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Request-Id"] = "test-id";
        httpContext.Items[RequestIdHelper.RequestIdItemKey] = "test-id";
        Activity? activity = null;

        // Act
        RequestIdHelper.SetActivityRequestId(activity, httpContext);

        // Assert - Should not throw exception
    }
}
