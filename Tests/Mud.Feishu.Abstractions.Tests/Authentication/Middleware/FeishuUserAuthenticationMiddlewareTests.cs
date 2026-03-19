// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Mud.Feishu.Authentication;
using System.Security.Claims;

namespace Mud.Feishu.Tests.Authentication.Middleware;

/// <summary>
/// FeishuUserAuthenticationMiddleware 集成测试
/// </summary>
public class FeishuUserAuthenticationMiddlewareTests
{
    private readonly Mock<ILogger<FeishuUserAuthenticationMiddleware>> _loggerMock;
    private readonly ICurrentUserContext _userContext;

    public FeishuUserAuthenticationMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<FeishuUserAuthenticationMiddleware>>();
        _userContext = new CurrentUserContext();
    }

    #region InvokeAsync Tests

    [Fact]
    public async Task InvokeAsync_AuthenticatedUser_SetsUserContext()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("open_id", "test_open_id"),
            new Claim("union_id", "test_union_id"),
            new Claim("user_id", "test_user_id"),
            new Claim(ClaimTypes.Name, "Test User")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = CreateHttpContext(principal);
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(httpContext, _userContext);

        // Assert
        Assert.Equal("test_open_id", _userContext.OpenId);
        Assert.Equal("test_union_id", _userContext.UnionId);
        Assert.Equal("test_user_id", _userContext.UserId);
        Assert.Equal("Test User", _userContext.Name);
        Assert.True(_userContext.IsAuthenticated);
    }

    [Fact]
    public async Task InvokeAsync_NoUser_DoesNotSetUserContext()
    {
        // Arrange
        var httpContext = CreateHttpContext(null);
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(httpContext, _userContext);

        // Assert
        Assert.Null(_userContext.OpenId);
        Assert.False(_userContext.IsAuthenticated);
    }

    [Fact]
    public async Task InvokeAsync_AfterRequest_ClearsUserContext()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("open_id", "test_open_id")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = CreateHttpContext(principal);
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(httpContext, _userContext);

        // Assert - After request completes, context should be cleared
        Assert.Null(_userContext.OpenId);
        Assert.False(_userContext.IsAuthenticated);
    }

    [Fact]
    public async Task InvokeAsync_ExtractsAllClaims()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("open_id", "open_id_value"),
            new Claim("union_id", "union_id_value"),
            new Claim("user_id", "user_id_value"),
            new Claim(ClaimTypes.Name, "user_name")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var capturedValues = new CapturedUserValues();
        var httpContext = CreateHttpContext(principal);
        var middleware = CreateMiddleware(_ =>
        {
            capturedValues.OpenId = _userContext.OpenId;
            capturedValues.UnionId = _userContext.UnionId;
            capturedValues.UserId = _userContext.UserId;
            capturedValues.Name = _userContext.Name;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(httpContext, _userContext);

        // Assert - During request, all values should be available
        Assert.Equal("open_id_value", capturedValues.OpenId);
        Assert.Equal("union_id_value", capturedValues.UnionId);
        Assert.Equal("user_id_value", capturedValues.UserId);
        Assert.Equal("user_name", capturedValues.Name);
    }

    [Fact]
    public async Task InvokeAsync_MissingOpenIdClaim_DoesNotSetContext()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("union_id", "test_union_id"),
            new Claim(ClaimTypes.Name, "Test User")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = CreateHttpContext(principal);
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(httpContext, _userContext);

        // Assert
        Assert.Null(_userContext.OpenId);
        Assert.False(_userContext.IsAuthenticated);
    }

    [Fact]
    public async Task InvokeAsync_UsesNameIdentifierFallback()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "fallback_open_id")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = CreateHttpContext(principal);
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(httpContext, _userContext);

        // Assert
        Assert.Null(_userContext.OpenId); // NameIdentifier is not used as fallback for open_id
        Assert.False(_userContext.IsAuthenticated);
    }

    [Fact]
    public async Task InvokeAsync_UnauthenticatedIdentity_DoesNotSetContext()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("open_id", "test_open_id")
        };
        var identity = new ClaimsIdentity(claims); // No authentication type = not authenticated
        var principal = new ClaimsPrincipal(identity);

        var httpContext = CreateHttpContext(principal);
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(httpContext, _userContext);

        // Assert
        Assert.Null(_userContext.OpenId);
        Assert.False(_userContext.IsAuthenticated);
    }

    [Fact]
    public async Task InvokeAsync_ExceptionInNextMiddleware_ClearsUserContext()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("open_id", "test_open_id")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = CreateHttpContext(principal);
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("Test exception"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            middleware.InvokeAsync(httpContext, _userContext));

        // Assert - Context should be cleared even after exception
        Assert.Null(_userContext.OpenId);
        Assert.False(_userContext.IsAuthenticated);
    }

    #endregion

    #region Helper Methods

    private FeishuUserAuthenticationMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new FeishuUserAuthenticationMiddleware(next, _loggerMock.Object);
    }

    private static HttpContext CreateHttpContext(ClaimsPrincipal? user)
    {
        var context = new DefaultHttpContext();
        context.User = user ?? new ClaimsPrincipal();
        return context;
    }

    private class CapturedUserValues
    {
        public string? OpenId { get; set; }
        public string? UnionId { get; set; }
        public string? UserId { get; set; }
        public string? Name { get; set; }
    }

    #endregion
}
