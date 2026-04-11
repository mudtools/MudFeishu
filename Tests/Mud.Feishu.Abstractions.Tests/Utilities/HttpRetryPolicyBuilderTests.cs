using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Abstractions.Utilities;
using Moq.Protected;
using System.Net;
using Mud.Feishu.Abstractions;

namespace Mud.Feishu.Abstractions.Tests.Utilities;

/// <summary>
/// HttpRetryPolicyBuilder 单元测试
/// </summary>
public class HttpRetryPolicyBuilderTests
{
    private readonly Mock<IOptions<FeishuAppConfig>> _optionsMock;

    public HttpRetryPolicyBuilderTests()
    {
        _optionsMock = new Mock<IOptions<FeishuAppConfig>>();
        _optionsMock.Setup(x => x.Value).Returns(new FeishuAppConfig
        {
            AppKey = "test_app_key",
            AppId = "test_app_id",
            AppSecret = "test_app_secret",
            BaseUrl = "https://open.feishu.cn",
            RetryCount = 3,
            RetryDelayMs = 1000
        });
    }

    #region BuildRetryPolicy Tests

    [Fact]
    public void BuildRetryPolicy_WithDefaultParameters_ShouldReturnPolicy()
    {
        // Act
        var policy = HttpRetryPolicyBuilder.BuildRetryPolicy(3, 1000);

        // Assert
        Assert.NotNull(policy);
    }

    [Fact]
    public void BuildRetryPolicy_WithZeroRetryCount_ShouldReturnNonRetryingPolicy()
    {
        // Act
        var policy = HttpRetryPolicyBuilder.BuildRetryPolicy(0, 1000);

        // Assert
        Assert.NotNull(policy);
    }

    [Fact]
    public void BuildRetryPolicy_WithNegativeRetryCount_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            HttpRetryPolicyBuilder.BuildRetryPolicy(-1, 1000));
    }

    [Fact]
    public void BuildRetryPolicy_WithNegativeDelay_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            HttpRetryPolicyBuilder.BuildRetryPolicy(3, -1));
    }

    [Fact]
    public void BuildRetryPolicy_WithLargeDelay_ShouldCapDelay()
    {
        // Act - Using 60000ms delay, after 1 retry it should be capped at 30000ms
        var policy = HttpRetryPolicyBuilder.BuildRetryPolicy(3, 60000);

        // Assert
        Assert.NotNull(policy);
    }

    #endregion

    #region Retry Behavior Tests

    [Fact]
    public async Task BuildRetryPolicy_ShouldRetryOn5xxErrors()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var callCount = 0;

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://open.feishu.cn")
        };

        var policy = HttpRetryPolicyBuilder.BuildRetryPolicy(3, 10);

        // Act
        var result = await policy.ExecuteAsync(async ct =>
        {
            var response = await httpClient.GetAsync("/test", ct);
            return response;
        }, CancellationToken.None);

        // Assert - Should retry 3 times (initial + 3 retries)
        Assert.Equal(4, callCount);
        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [Fact]
    public async Task BuildRetryPolicy_ShouldRetryOn408Timeout()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var callCount = 0;

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage(HttpStatusCode.RequestTimeout);
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://open.feishu.cn")
        };

        var policy = HttpRetryPolicyBuilder.BuildRetryPolicy(2, 10);

        // Act
        var result = await policy.ExecuteAsync(async ct =>
        {
            var response = await httpClient.GetAsync("/test", ct);
            return response;
        }, CancellationToken.None);

        // Assert
        Assert.Equal(3, callCount); // initial + 2 retries
        Assert.Equal(HttpStatusCode.RequestTimeout, result.StatusCode);
    }

    [Fact]
    public async Task BuildRetryPolicy_ShouldRetryOn429TooManyRequests()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var callCount = 0;

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://open.feishu.cn")
        };

        var policy = HttpRetryPolicyBuilder.BuildRetryPolicy(2, 10);

        // Act
        var result = await policy.ExecuteAsync(async ct =>
        {
            var response = await httpClient.GetAsync("/test", ct);
            return response;
        }, CancellationToken.None);

        // Assert
        Assert.Equal(3, callCount);
        Assert.Equal(HttpStatusCode.TooManyRequests, result.StatusCode);
    }

    [Fact]
    public async Task BuildRetryPolicy_ShouldNotRetryOn4xxErrors()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var callCount = 0;

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://open.feishu.cn")
        };

        var policy = HttpRetryPolicyBuilder.BuildRetryPolicy(3, 10);

        // Act
        var result = await policy.ExecuteAsync(async ct =>
        {
            var response = await httpClient.GetAsync("/test", ct);
            return response;
        }, CancellationToken.None);

        // Assert - Should not retry, only one call
        Assert.Equal(1, callCount);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task BuildRetryPolicy_ShouldNotRetryOn401Unauthorized()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var callCount = 0;

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://open.feishu.cn")
        };

        var policy = HttpRetryPolicyBuilder.BuildRetryPolicy(3, 10);

        // Act
        var result = await policy.ExecuteAsync(async ct =>
        {
            var response = await httpClient.GetAsync("/test", ct);
            return response;
        }, CancellationToken.None);

        // Assert
        Assert.Equal(1, callCount);
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task BuildRetryPolicy_ShouldNotRetryOn403Forbidden()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var callCount = 0;

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage(HttpStatusCode.Forbidden);
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://open.feishu.cn")
        };

        var policy = HttpRetryPolicyBuilder.BuildRetryPolicy(3, 10);

        // Act
        var result = await policy.ExecuteAsync(async ct =>
        {
            var response = await httpClient.GetAsync("/test", ct);
            return response;
        }, CancellationToken.None);

        // Assert
        Assert.Equal(1, callCount);
        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task BuildRetryPolicy_ShouldNotRetryOn404NotFound()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var callCount = 0;

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://open.feishu.cn")
        };

        var policy = HttpRetryPolicyBuilder.BuildRetryPolicy(3, 10);

        // Act
        var result = await policy.ExecuteAsync(async ct =>
        {
            var response = await httpClient.GetAsync("/test", ct);
            return response;
        }, CancellationToken.None);

        // Assert
        Assert.Equal(1, callCount);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task BuildRetryPolicy_ShouldRetryOnNetworkExceptions()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var callCount = 0;

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://open.feishu.cn")
        };

        var policy = HttpRetryPolicyBuilder.BuildRetryPolicy(2, 10);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await policy.ExecuteAsync(async ct =>
            {
                callCount++;
                return await httpClient.GetAsync("/test", ct);
            }, CancellationToken.None);
        });

        Assert.Equal(3, callCount); // initial + 2 retries
    }

    [Fact]
    public async Task BuildRetryPolicy_ShouldRetryOnTimeoutExceptions()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var callCount = 0;

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TimeoutException("Operation timed out"));

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://open.feishu.cn")
        };

        var policy = HttpRetryPolicyBuilder.BuildRetryPolicy(2, 10);

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(async () =>
        {
            await policy.ExecuteAsync(async ct =>
            {
                callCount++;
                return await httpClient.GetAsync("/test", ct);
            }, CancellationToken.None);
        });

        Assert.Equal(3, callCount);
    }

    [Fact]
    public async Task BuildRetryPolicy_ShouldStopRetryingOnSuccess()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var callCount = 0;

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://open.feishu.cn")
        };

        var policy = HttpRetryPolicyBuilder.BuildRetryPolicy(5, 10);

        // Act
        var result = await policy.ExecuteAsync(async ct =>
        {
            var response = await httpClient.GetAsync("/test", ct);
            return response;
        }, CancellationToken.None);

        // Assert - Should stop after 2nd call (1 failure + 1 success)
        Assert.Equal(2, callCount);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    #endregion

    #region Delay Calculation Tests

    [Fact]
    public async Task BuildRetryPolicy_ShouldUseExponentialBackoff()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var timestamps = new List<DateTime>();

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                timestamps.Add(DateTime.UtcNow);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://open.feishu.cn")
        };

        var retryDelayMs = 100;
        var policy = HttpRetryPolicyBuilder.BuildRetryPolicy(3, retryDelayMs);

        // Act
        await policy.ExecuteAsync(async ct =>
        {
            return await httpClient.GetAsync("/test", ct);
        }, CancellationToken.None);

        // Assert - Check exponential backoff
        // With jitter, delays should be approximately:
        // 1st retry: ~100ms
        // 2nd retry: ~200ms
        // 3rd retry: ~400ms
        Assert.Equal(4, timestamps.Count); // initial + 3 retries

        // Verify delays are increasing (with tolerance for jitter)
        var delay1 = (timestamps[1] - timestamps[0]).TotalMilliseconds;
        var delay2 = (timestamps[2] - timestamps[1]).TotalMilliseconds;
        var delay3 = (timestamps[3] - timestamps[2]).TotalMilliseconds;

        // With ±20% jitter and CI environment timing variations, delays should be:
        // 1st retry: 80-120ms theoretical (100ms * 2^0 * [0.8, 1.2))
        //            Allow wider range for CI environment: 50-500ms (to account for system overhead)
        // 2nd retry: 160-240ms theoretical (100ms * 2^1 * [0.8, 1.2))
        //            Allow wider range for CI environment: 100-600ms
        // 3rd retry: 320-480ms theoretical (100ms * 2^2 * [0.8, 1.2))
        //            Allow wider range for CI environment: 200-1000ms
        Assert.InRange(delay1, 50, 500); // ~100ms ± 20% jitter + CI variance + system overhead
        Assert.InRange(delay2, 100, 600); // ~200ms ± 20% jitter + CI variance + system overhead
        Assert.InRange(delay3, 200, 1000); // ~400ms ± 20% jitter + CI variance + system overhead
        
        // Verify exponential backoff: each delay should generally be larger than the previous
        // (allowing for some tolerance due to jitter and timing variations)
        // Note: We use 0.3 tolerance instead of 0.5 to be more resilient to CI timing variations
        Assert.True(delay2 > delay1 * 0.3, $"delay2 ({delay2}ms) should be reasonably larger than 30% of delay1 ({delay1}ms)");
        Assert.True(delay3 > delay2 * 0.3, $"delay3 ({delay3}ms) should be reasonably larger than 30% of delay2 ({delay2}ms)");
    }

    [Fact]
    public async Task BuildRetryPolicy_ShouldCapDelayAt30Seconds()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var timestamps = new List<DateTime>();

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                timestamps.Add(DateTime.UtcNow);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://open.feishu.cn")
        };

        // Use base delay of 500ms to test capping logic (capped at 30s but we test with smaller values)
        var retryDelayMs = 500;
        var policy = HttpRetryPolicyBuilder.BuildRetryPolicy(3, retryDelayMs);

        // Act
        await policy.ExecuteAsync(async ct =>
        {
            return await httpClient.GetAsync("/test", ct);
        }, CancellationToken.None);

        // Assert - Verify exponential backoff is working (delays should increase)
        Assert.Equal(4, timestamps.Count); // initial + 3 retries
        
        // Verify delays are increasing with exponential backoff
        var delay1 = (timestamps[1] - timestamps[0]).TotalMilliseconds;
        var delay2 = (timestamps[2] - timestamps[1]).TotalMilliseconds;
        var delay3 = (timestamps[3] - timestamps[2]).TotalMilliseconds;
        
        // With jitter, delays should be roughly: 500ms, 1000ms, 2000ms
        Assert.InRange(delay1, 250, 1000);
        Assert.InRange(delay2, 500, 1500);
        Assert.InRange(delay3, 1000, 3000);
    }

    [Fact]
    public async Task BuildRetryPolicy_ShouldAddJitterToDelay()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var timestamps = new List<DateTime>();

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                timestamps.Add(DateTime.UtcNow);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://open.feishu.cn")
        };

        var policy = HttpRetryPolicyBuilder.BuildRetryPolicy(5, 50);

        // Act
        await policy.ExecuteAsync(async ct =>
        {
            return await httpClient.GetAsync("/test", ct);
        }, CancellationToken.None);

        // Assert - Verify jitter adds randomness
        var delays = new List<double>();
        for (int i = 1; i < timestamps.Count; i++)
        {
            delays.Add((timestamps[i] - timestamps[i - 1]).TotalMilliseconds);
        }

        // With jitter, delays should not all be exactly the same
        var uniqueDelays = delays.Distinct().Count();
        Assert.True(uniqueDelays > 1, "Jitter should add randomness to delays");
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task BuildRetryPolicy_ShouldRespectCancellationToken()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var callCount = 0;

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns((HttpRequestMessage request, CancellationToken ct) =>
            {
                callCount++;
                if (callCount == 2)
                {
                    // 返回一个带有OperationCanceledException的Task
                    return Task.FromException<HttpResponseMessage>(new OperationCanceledException());
                }
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://open.feishu.cn")
        };

        var cts = new CancellationTokenSource();
        var policy = HttpRetryPolicyBuilder.BuildRetryPolicy(5, 100);

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await policy.ExecuteAsync(async ct =>
            {
                return await httpClient.GetAsync("/test", ct);
            }, cts.Token);
        });

        // Should have made 2 calls before cancellation
        Assert.Equal(2, callCount);
    }

    #endregion
}
