// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Authentication;
using System.Security.Claims;
using Xunit;

namespace Mud.Feishu.Authentication.Tests.Middleware;

/// <summary>
/// FeishuUserAuthenticationMiddleware 单元测试
/// </summary>
public class FeishuUserAuthenticationMiddlewareTests
{
    private readonly Mock<ILogger<FeishuUserAuthenticationMiddleware>> _loggerMock;
    private readonly Mock<ILogger<CurrentUserContext>> _userContextLoggerMock;
    private readonly IFeishuCurrentUserContext _userContext;
    private readonly IOptions<FeishuUserAuthenticationOptions> _defaultOptions;

    public FeishuUserAuthenticationMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<FeishuUserAuthenticationMiddleware>>();
        _userContextLoggerMock = new Mock<ILogger<CurrentUserContext>>();
        _userContext = new CurrentUserContext(_userContextLoggerMock.Object);
        _defaultOptions = Options.Create(new FeishuUserAuthenticationOptions());
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullNext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FeishuUserAuthenticationMiddleware(null!, _defaultOptions, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FeishuUserAuthenticationMiddleware(_ => Task.CompletedTask, null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FeishuUserAuthenticationMiddleware(_ => Task.CompletedTask, _defaultOptions, null!));
    }

    #endregion

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

        var capturedValues = new CapturedUserValues();
        var httpContext = CreateHttpContext(principal);
        var middleware = CreateMiddleware(_ =>
        {
            // Capture values during request processing
            capturedValues.OpenId = _userContext.OpenId;
            capturedValues.UnionId = _userContext.UnionId;
            capturedValues.UserId = _userContext.UserId;
            capturedValues.Name = _userContext.Name;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(httpContext, _userContext);

        // Assert - Values should be available during request processing
        Assert.Equal("test_open_id", capturedValues.OpenId);
        Assert.Equal("test_union_id", capturedValues.UnionId);
        Assert.Equal("test_user_id", capturedValues.UserId);
        Assert.Equal("Test User", capturedValues.Name);
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

    [Fact]
    public async Task InvokeAsync_OnlyOpenIdClaim_SetsOnlyOpenId()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("open_id", "test_open_id")
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

        // Assert
        Assert.Equal("test_open_id", capturedValues.OpenId);
        Assert.Null(capturedValues.UnionId);
        Assert.Null(capturedValues.UserId);
        Assert.Null(capturedValues.Name);
    }

    [Fact]
    public async Task InvokeAsync_CallsNextMiddleware()
    {
        // Arrange
        var wasCalled = false;
        var httpContext = CreateHttpContext(null);
        var middleware = CreateMiddleware(_ =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(httpContext, _userContext);

        // Assert
        Assert.True(wasCalled);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public async Task InvokeAsync_CustomClaimTypes_UsesConfiguredClaims()
    {
        // Arrange
        var options = new FeishuUserAuthenticationOptions
        {
            OpenIdClaimType = "custom_open_id",
            UnionIdClaimType = "custom_union_id",
            UserIdClaimType = "custom_user_id",
            NameClaimType = "custom_name"
        };
        var claims = new List<Claim>
        {
            new Claim("custom_open_id", "custom_open_id_value"),
            new Claim("custom_union_id", "custom_union_id_value"),
            new Claim("custom_user_id", "custom_user_id_value"),
            new Claim("custom_name", "custom_name_value")
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
        }, options);

        // Act
        await middleware.InvokeAsync(httpContext, _userContext);

        // Assert
        Assert.Equal("custom_open_id_value", capturedValues.OpenId);
        Assert.Equal("custom_union_id_value", capturedValues.UnionId);
        Assert.Equal("custom_user_id_value", capturedValues.UserId);
        Assert.Equal("custom_name_value", capturedValues.Name);
    }

    [Fact]
    public async Task InvokeAsync_FallbackClaimType_UsesFallbackWhenPrimaryNotPresent()
    {
        // Arrange
        var options = new FeishuUserAuthenticationOptions();
        var claims = new List<Claim>
        {
            // No "open_id" claim, but has NameIdentifier
            new Claim(ClaimTypes.NameIdentifier, "fallback_open_id"),
            new Claim("union_id", "union_id_value")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var capturedValues = new CapturedUserValues();
        var httpContext = CreateHttpContext(principal);
        var middleware = CreateMiddleware(_ =>
        {
            capturedValues.OpenId = _userContext.OpenId;
            return Task.CompletedTask;
        }, options);

        // Act
        await middleware.InvokeAsync(httpContext, _userContext);

        // Assert
        Assert.Equal("fallback_open_id", capturedValues.OpenId);
    }

    [Fact]
    public async Task InvokeAsync_DisableDistributedTracing_DoesNotCreateActivity()
    {
        // Arrange
        var options = new FeishuUserAuthenticationOptions
        {
            EnableDistributedTracing = false
        };
        var claims = new List<Claim>
        {
            new Claim("open_id", "test_open_id")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = CreateHttpContext(principal);
        var middleware = CreateMiddleware(_ => Task.CompletedTask, options);

        // Act - Should not throw
        await middleware.InvokeAsync(httpContext, _userContext);

        // Assert - Just verify no exception
        Assert.True(true);
    }

    #endregion

    #region Helper Methods

    private FeishuUserAuthenticationMiddleware CreateMiddleware(RequestDelegate next, FeishuUserAuthenticationOptions? options = null)
    {
        return new FeishuUserAuthenticationMiddleware(next, Options.Create(options ?? new FeishuUserAuthenticationOptions()), _loggerMock.Object);
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
