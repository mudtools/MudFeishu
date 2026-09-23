// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Mud.Feishu.Abstractions.Tests.Authentication;

/// <summary>
/// P2-1：TokenRecoveryExecutor 401 自动刷新重试行为测试。
/// 验证收到 401 时自动刷新令牌并重试的核心路径，以及禁用恢复的边界条件。
/// </summary>
public class TokenRecoveryExecutorTests
{
    private static TokenRecoveryExecutor CreateExecutor(
        Mock<ITokenManager> tokenManagerMock,
        TokenRecoveryOptions? options = null)
    {
        options ??= new TokenRecoveryOptions();
        var optionsMonitor = Mock.Of<IOptionsMonitor<TokenRecoveryOptions>>(m => m.CurrentValue == options);
        return new TokenRecoveryExecutor(tokenManagerMock.Object, optionsMonitor);
    }

    private static HttpRequestMessage CreateRequestWithAuth()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://open.feishu.cn/open-apis/test");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "old-token");
        return request;
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRefreshTokenAndRetry_WhenFirstResponseIs401()
    {
        // Arrange
        var tokenManagerMock = new Mock<ITokenManager>();
        tokenManagerMock
            .Setup(x => x.GetOrRefreshTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("new-access-token");

        var executor = CreateExecutor(tokenManagerMock);
        var request = CreateRequestWithAuth();

        var sendCount = 0;
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendFunc = (req, ct) =>
        {
            sendCount++;
            var statusCode = sendCount == 1 ? HttpStatusCode.Unauthorized : HttpStatusCode.OK;
            var response = new HttpResponseMessage(statusCode) { RequestMessage = req };
            return Task.FromResult(response);
        };

        // Act
        var result = await executor.ExecuteAsync(request, sendFunc, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        sendCount.Should().Be(2); // 初始发送 + 1 次重试
        tokenManagerMock.Verify(x => x.GetOrRefreshTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturn401_WhenRetryAlsoReturns401()
    {
        // Arrange
        var tokenManagerMock = new Mock<ITokenManager>();
        tokenManagerMock
            .Setup(x => x.GetOrRefreshTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("new-access-token");

        var executor = CreateExecutor(tokenManagerMock);
        var request = CreateRequestWithAuth();

        var sendCount = 0;
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendFunc = (req, ct) =>
        {
            sendCount++;
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized) { RequestMessage = req };
            return Task.FromResult(response);
        };

        // Act
        var result = await executor.ExecuteAsync(request, sendFunc, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        sendCount.Should().Be(2); // 初始 + 1 次重试（RecoveryMaxRetries 默认 1）
        tokenManagerMock.Verify(x => x.GetOrRefreshTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotAttemptRecovery_WhenRequestHasNoAuthorizationHeader()
    {
        // Arrange
        var tokenManagerMock = new Mock<ITokenManager>();
        tokenManagerMock
            .Setup(x => x.GetOrRefreshTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("new-access-token");

        var executor = CreateExecutor(tokenManagerMock);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://open.feishu.cn/open-apis/test");

        var sendCount = 0;
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendFunc = (req, ct) =>
        {
            sendCount++;
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized) { RequestMessage = req };
            return Task.FromResult(response);
        };

        // Act
        var result = await executor.ExecuteAsync(request, sendFunc, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        sendCount.Should().Be(1); // 无 Authorization 头 → 不重试
        tokenManagerMock.Verify(x => x.GetOrRefreshTokenAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotAttemptRecovery_WhenRecoveryMaxRetriesIsZero()
    {
        // Arrange
        var tokenManagerMock = new Mock<ITokenManager>();
        tokenManagerMock
            .Setup(x => x.GetOrRefreshTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("new-access-token");

        var options = new TokenRecoveryOptions { RecoveryMaxRetries = 0 };
        var executor = CreateExecutor(tokenManagerMock, options);
        var request = CreateRequestWithAuth();

        var sendCount = 0;
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendFunc = (req, ct) =>
        {
            sendCount++;
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized) { RequestMessage = req };
            return Task.FromResult(response);
        };

        // Act
        var result = await executor.ExecuteAsync(request, sendFunc, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        sendCount.Should().Be(1); // RecoveryMaxRetries=0 → 不重试
        tokenManagerMock.Verify(x => x.GetOrRefreshTokenAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnResponseDirectly_WhenStatusCodeIsNot401()
    {
        // Arrange
        var tokenManagerMock = new Mock<ITokenManager>();
        var executor = CreateExecutor(tokenManagerMock);
        var request = CreateRequestWithAuth();

        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendFunc = (req, ct) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = req };
            return Task.FromResult(response);
        };

        // Act
        var result = await executor.ExecuteAsync(request, sendFunc, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        tokenManagerMock.Verify(x => x.GetOrRefreshTokenAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotAttemptRecovery_WhenDisabled()
    {
        // Arrange
        var tokenManagerMock = new Mock<ITokenManager>();
        tokenManagerMock
            .Setup(x => x.GetOrRefreshTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("new-access-token");

        var options = new TokenRecoveryOptions { Enabled = false };
        var executor = CreateExecutor(tokenManagerMock, options);
        var request = CreateRequestWithAuth();

        var sendCount = 0;
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendFunc = (req, ct) =>
        {
            sendCount++;
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized) { RequestMessage = req };
            return Task.FromResult(response);
        };

        // Act
        var result = await executor.ExecuteAsync(request, sendFunc, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        sendCount.Should().Be(1); // Enabled=false → 直接返回
        tokenManagerMock.Verify(x => x.GetOrRefreshTokenAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
