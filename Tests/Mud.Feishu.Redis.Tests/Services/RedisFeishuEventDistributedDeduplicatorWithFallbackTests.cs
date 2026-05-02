// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Configuration;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Redis.Services;

namespace Mud.Feishu.Redis.Tests.Services;

public class RedisFeishuEventDistributedDeduplicatorWithFallbackTests
{
    private readonly Mock<IConnectionMultiplexer> _connectionMultiplexerMock;
    private readonly Mock<IDatabase> _databaseMock;
    private readonly Mock<ILogger<RedisFeishuEventDistributedDeduplicatorWithFallback>> _loggerMock;
    private readonly Mock<IFallbackAlertService> _alertServiceMock;

    public RedisFeishuEventDistributedDeduplicatorWithFallbackTests()
    {
        _connectionMultiplexerMock = new Mock<IConnectionMultiplexer>();
        _databaseMock = new Mock<IDatabase>();
        _loggerMock = new Mock<ILogger<RedisFeishuEventDistributedDeduplicatorWithFallback>>();
        _alertServiceMock = new Mock<IFallbackAlertService>();

        _connectionMultiplexerMock
            .Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);

        _alertServiceMock
            .Setup(x => x.RaiseAlertAsync(It.IsAny<FallbackAlertType>(), It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<Dictionary<string, object?>>()))
            .Returns(Task.CompletedTask);
    }

    private RedisFeishuEventDistributedDeduplicatorWithFallback CreateSut(
        TimeSpan? cacheExpiration = null,
        TimeSpan? processingTimeout = null,
        string? keyPrefix = null,
        int maxRetryCount = 3,
        IFallbackAlertService? alertService = null)
    {
        return new RedisFeishuEventDistributedDeduplicatorWithFallback(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object,
            cacheExpiration ?? TimeSpan.FromHours(24),
            processingTimeout ?? TimeSpan.FromMinutes(10),
            keyPrefix,
            maxRetryCount,
            TimeSpan.FromMilliseconds(10),
            TimeSpan.FromMilliseconds(50),
            alertService);
    }

    private RedisFeishuEventDistributedDeduplicatorWithFallback CreateSutWithOptions(
        DeduplicationOptions? options = null,
        IFallbackAlertService? alertService = null)
    {
        return new RedisFeishuEventDistributedDeduplicatorWithFallback(
            _connectionMultiplexerMock.Object,
            options ?? DeduplicationOptions.Default,
            _loggerMock.Object,
            alertService);
    }

    [Fact]
    public void Constructor_WhenRedisIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new RedisFeishuEventDistributedDeduplicatorWithFallback(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithOptions_WhenRedisIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new RedisFeishuEventDistributedDeduplicatorWithFallback(null!, DeduplicationOptions.Default, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithOptions_WhenOptionsIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new RedisFeishuEventDistributedDeduplicatorWithFallback(_connectionMultiplexerMock.Object, (DeduplicationOptions)null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        var sut = CreateSut();

        Assert.True(sut.IsUsingRedis);
        Assert.Equal(0, sut.ConsecutiveFailures);
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_WhenEmptyEventId_ShouldReturnSuccess()
    {
        var sut = CreateSut();

        var result = await sut.TryMarkAsProcessingAsync("");

        Assert.False(result.IsDuplicate);
        Assert.Equal(string.Empty, result.EventId);
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_WhenNullEventId_ShouldReturnSuccess()
    {
        var sut = CreateSut();

        var result = await sut.TryMarkAsProcessingAsync(null!);

        Assert.False(result.IsDuplicate);
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_WhenNewEvent_ShouldReturnSuccess()
    {
        SetupHashGetAllEmpty();

        var sut = CreateSut();

        var result = await sut.TryMarkAsProcessingAsync("evt_001");

        Assert.False(result.IsDuplicate);
        Assert.Equal("evt_001", result.EventId);
        Assert.Equal(DeduplicationStatus.Processing, result.Status);
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_WhenCompletedEvent_ShouldReturnDuplicate()
    {
        SetupHashGetAll(new[]
        {
            new HashEntry("status", "completed"),
            new HashEntry("timestamp", DateTime.UtcNow.ToString("O"))
        });

        var sut = CreateSut();

        var result = await sut.TryMarkAsProcessingAsync("evt_001");

        Assert.True(result.IsDuplicate);
        Assert.Equal(DeduplicationStatus.Completed, result.Status);
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_WhenProcessingEventNotTimedOut_ShouldReturnDuplicate()
    {
        SetupHashGetAll(new[]
        {
            new HashEntry("status", "processing"),
            new HashEntry("timestamp", DateTime.UtcNow.ToString("O"))
        });

        var sut = CreateSut();

        var result = await sut.TryMarkAsProcessingAsync("evt_001");

        Assert.True(result.IsDuplicate);
        Assert.True(result.WasProcessing);
        Assert.Equal(DeduplicationStatus.Processing, result.Status);
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_WhenProcessingEventTimedOut_ShouldReturnTimeoutRecoverable()
    {
        var oldTimestamp = DateTime.UtcNow.AddMinutes(-20).ToString("O");
        SetupHashGetAll(new[]
        {
            new HashEntry(new RedisValue("status"), new RedisValue("processing")),
            new HashEntry(new RedisValue("timestamp"), new RedisValue(oldTimestamp))
        });

        var sut = CreateSut(processingTimeout: TimeSpan.FromMinutes(10));

        var result = await sut.TryMarkAsProcessingAsync("evt_001");

        Assert.True(result.WasProcessing);
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_WhenRedisThrowsException_ShouldFallbackToMemory()
    {
        _databaseMock
            .Setup(x => x.HashGetAllAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Connection failed"));

        var sut = CreateSut(maxRetryCount: 1);

        var result = await sut.TryMarkAsProcessingAsync("evt_001");

        Assert.False(result.IsDuplicate);
        Assert.Equal(1, sut.ConsecutiveFailures);
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_WhenRedisConnectionException_ShouldFallbackToMemory()
    {
        _databaseMock
            .Setup(x => x.HashGetAllAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection lost"));

        var sut = CreateSut(maxRetryCount: 1);

        var result = await sut.TryMarkAsProcessingAsync("evt_001");

        Assert.False(result.IsDuplicate);
        Assert.Equal(1, sut.ConsecutiveFailures);
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_WhenConsecutiveFailuresReach3_ShouldMarkRedisUnavailable()
    {
        _databaseMock
            .Setup(x => x.HashGetAllAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Connection failed"));

        var sut = CreateSut(maxRetryCount: 1);

        await sut.TryMarkAsProcessingAsync("evt_001");
        await sut.TryMarkAsProcessingAsync("evt_002");
        await sut.TryMarkAsProcessingAsync("evt_003");

        Assert.False(sut.IsUsingRedis);
        Assert.True(sut.ConsecutiveFailures >= 3);
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_WhenRedisMarkedUnavailable_ShouldUseFallbackDirectly()
    {
        _databaseMock
            .Setup(x => x.HashGetAllAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Connection failed"));

        var sut = CreateSut(maxRetryCount: 1);

        for (int i = 0; i < 3; i++)
        {
            await sut.TryMarkAsProcessingAsync($"evt_{i}");
        }

        Assert.False(sut.IsUsingRedis);

        var result = await sut.TryMarkAsProcessingAsync("evt_after_fallback");
        Assert.False(result.IsDuplicate);
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_WhenFallbackUsed_ShouldDeduplicateInMemory()
    {
        _databaseMock
            .Setup(x => x.HashGetAllAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Connection failed"));

        var sut = CreateSut(maxRetryCount: 1);

        for (int i = 0; i < 3; i++)
        {
            await sut.TryMarkAsProcessingAsync($"evt_{i}");
        }

        var result1 = await sut.TryMarkAsProcessingAsync("fallback_evt");
        Assert.False(result1.IsDuplicate);

        var result2 = await sut.TryMarkAsProcessingAsync("fallback_evt");
        Assert.True(result2.IsDuplicate);
    }

    [Fact]
    public async Task MarkAsCompletedAsync_WhenEmptyEventId_ShouldNotThrow()
    {
        var sut = CreateSut();

        await sut.MarkAsCompletedAsync("");
    }

    [Fact]
    public async Task MarkAsCompletedAsync_WhenNullEventId_ShouldNotThrow()
    {
        var sut = CreateSut();

        await sut.MarkAsCompletedAsync(null!);
    }

    [Fact]
    public async Task MarkAsCompletedAsync_WhenRedisAvailable_ShouldUpdateHash()
    {
        var sut = CreateSut();

        await sut.MarkAsCompletedAsync("evt_001");

        _databaseMock.Verify(
            x => x.HashSetAsync(It.IsAny<RedisKey>(), It.IsAny<HashEntry[]>(), It.IsAny<CommandFlags>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task MarkAsCompletedAsync_WhenRedisUnavailable_ShouldFallbackToMemory()
    {
        _databaseMock
            .Setup(x => x.HashGetAllAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Connection failed"));

        var sut = CreateSut(maxRetryCount: 1);

        for (int i = 0; i < 3; i++)
        {
            await sut.TryMarkAsProcessingAsync($"evt_{i}");
        }

        await sut.MarkAsCompletedAsync("evt_0");

        var isProcessed = await sut.IsProcessedAsync("evt_0");
        Assert.True(isProcessed);
    }

    [Fact]
    public async Task MarkAsCompletedAsync_WhenRedisThrowsOnComplete_ShouldFallbackToMemory()
    {
        _databaseMock
            .Setup(x => x.HashSetAsync(It.IsAny<RedisKey>(), It.IsAny<HashEntry[]>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Write failed"));

        var sut = CreateSut();

        await sut.MarkAsCompletedAsync("evt_001");
    }

    [Fact]
    public async Task RollbackProcessingAsync_WhenEmptyEventId_ShouldNotThrow()
    {
        var sut = CreateSut();

        await sut.RollbackProcessingAsync("");
    }

    [Fact]
    public async Task RollbackProcessingAsync_WhenRedisAvailable_ShouldDeleteKey()
    {
        _databaseMock
            .Setup(x => x.HashGetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync("processing");

        var sut = CreateSut();

        await sut.RollbackProcessingAsync("evt_001");

        _databaseMock.Verify(
            x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task RollbackProcessingAsync_WhenStatusNotProcessing_ShouldNotDeleteKey()
    {
        _databaseMock
            .Setup(x => x.HashGetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync("completed");

        var sut = CreateSut();

        await sut.RollbackProcessingAsync("evt_001");

        _databaseMock.Verify(
            x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()),
            Times.Never);
    }

    [Fact]
    public async Task RollbackProcessingAsync_WhenRedisUnavailable_ShouldFallbackToMemory()
    {
        _databaseMock
            .Setup(x => x.HashGetAllAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Connection failed"));

        var sut = CreateSut(maxRetryCount: 1);

        for (int i = 0; i < 3; i++)
        {
            await sut.TryMarkAsProcessingAsync($"evt_{i}");
        }

        await sut.RollbackProcessingAsync("evt_0");

        var status = await sut.GetStatusAsync("evt_0");
        Assert.Equal(DeduplicationStatus.Pending, status);
    }

    [Fact]
    public async Task IsProcessedAsync_WhenEmptyEventId_ShouldReturnFalse()
    {
        var sut = CreateSut();

        var result = await sut.IsProcessedAsync("");

        Assert.False(result);
    }

    [Fact]
    public async Task IsProcessedAsync_WhenCompletedEvent_ShouldReturnTrue()
    {
        _databaseMock
            .Setup(x => x.HashGetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync("completed");

        var sut = CreateSut();

        var result = await sut.IsProcessedAsync("evt_001");

        Assert.True(result);
    }

    [Fact]
    public async Task IsProcessedAsync_WhenProcessingEvent_ShouldReturnFalse()
    {
        _databaseMock
            .Setup(x => x.HashGetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync("processing");

        var sut = CreateSut();

        var result = await sut.IsProcessedAsync("evt_001");

        Assert.False(result);
    }

    [Fact]
    public async Task IsProcessedAsync_WhenNoEvent_ShouldReturnFalse()
    {
        _databaseMock
            .Setup(x => x.HashGetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var sut = CreateSut();

        var result = await sut.IsProcessedAsync("evt_001");

        Assert.False(result);
    }

    [Fact]
    public async Task IsProcessedAsync_WhenRedisFails_ShouldFallbackToMemory()
    {
        _databaseMock
            .Setup(x => x.HashGetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Connection failed"));

        var sut = CreateSut(maxRetryCount: 1);

        var result = await sut.IsProcessedAsync("evt_001");

        Assert.False(result);
    }

    [Fact]
    public async Task IsProcessedAsync_WhenRedisFails3Times_ShouldRaiseAlert()
    {
        _databaseMock
            .Setup(x => x.HashGetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Connection failed"));

        var sut = CreateSut(maxRetryCount: 1, alertService: _alertServiceMock.Object);

        await sut.IsProcessedAsync("evt_001");
        await sut.IsProcessedAsync("evt_002");
        await sut.IsProcessedAsync("evt_003");

        _alertServiceMock.Verify(
            x => x.RaiseAlertAsync(
                FallbackAlertType.RedisFallbackActivated,
                It.IsAny<string>(),
                It.IsAny<Exception>(),
                It.IsAny<Dictionary<string, object?>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetStatusAsync_WhenEmptyEventId_ShouldReturnPending()
    {
        var sut = CreateSut();

        var result = await sut.GetStatusAsync("");

        Assert.Equal(DeduplicationStatus.Pending, result);
    }

    [Fact]
    public async Task GetStatusAsync_WhenCompletedEvent_ShouldReturnCompleted()
    {
        SetupHashGetAll(new[]
        {
            new HashEntry("status", "completed"),
            new HashEntry("timestamp", DateTime.UtcNow.ToString("O"))
        });

        var sut = CreateSut();

        var result = await sut.GetStatusAsync("evt_001");

        Assert.Equal(DeduplicationStatus.Completed, result);
    }

    [Fact]
    public async Task GetStatusAsync_WhenProcessingEventNotTimedOut_ShouldReturnProcessing()
    {
        SetupHashGetAll(new[]
        {
            new HashEntry("status", "processing"),
            new HashEntry("timestamp", DateTime.UtcNow.ToString("O"))
        });

        var sut = CreateSut();

        var result = await sut.GetStatusAsync("evt_001");

        Assert.Equal(DeduplicationStatus.Processing, result);
    }

    [Fact]
    public async Task GetStatusAsync_WhenProcessingEventTimedOut_ShouldReturnPending()
    {
        var oldTimestamp = DateTime.UtcNow.AddMinutes(-20).ToString("O");
        SetupHashGetAll(new[]
        {
            new HashEntry(new RedisValue("status"), new RedisValue("processing")),
            new HashEntry(new RedisValue("timestamp"), new RedisValue(oldTimestamp))
        });

        var sut = CreateSut(processingTimeout: TimeSpan.FromMinutes(10));

        var result = await sut.GetStatusAsync("evt_001");

        Assert.Equal(DeduplicationStatus.Pending, result);
    }

    [Fact]
    public async Task GetStatusAsync_WhenNoEvent_ShouldReturnPending()
    {
        SetupHashGetAllEmpty();

        var sut = CreateSut();

        var result = await sut.GetStatusAsync("evt_001");

        Assert.Equal(DeduplicationStatus.Pending, result);
    }

    [Fact]
    public async Task GetStatusAsync_WhenRedisFails_ShouldFallbackToMemory()
    {
        _databaseMock
            .Setup(x => x.HashGetAllAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Connection failed"));

        var sut = CreateSut(maxRetryCount: 1);

        var result = await sut.GetStatusAsync("evt_001");

        Assert.Equal(DeduplicationStatus.Pending, result);
    }

    [Fact]
    public async Task CleanupExpiredAsync_ShouldReturnNonNegative()
    {
        var sut = CreateSut();

        var result = await sut.CleanupExpiredAsync();

        Assert.True(result >= 0);
    }

    [Fact]
    public async Task DisposeAsync_ShouldNotThrow()
    {
        var sut = CreateSut();

        await sut.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_WhenCalledTwice_ShouldNotThrow()
    {
        var sut = CreateSut();

        await sut.DisposeAsync();
        await sut.DisposeAsync();
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_WithAppKey_ShouldUseAppKeyInKey()
    {
        SetupHashGetAllEmpty();

        var sut = CreateSut(keyPrefix: "test:");

        await sut.TryMarkAsProcessingAsync("evt_001", "app1");

        _databaseMock.Verify(
            x => x.HashGetAllAsync(It.Is<RedisKey>(k => k.ToString().Contains("app1")), It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_WithCustomTtl_ShouldSetExpiry()
    {
        SetupHashGetAllEmpty();

        var customTtl = TimeSpan.FromMinutes(30);

        var sut = CreateSut();

        await sut.TryMarkAsProcessingAsync("evt_001", null, customTtl);

        _databaseMock.Verify(
            x => x.KeyExpireAsync(It.IsAny<RedisKey>(), customTtl, It.IsAny<ExpireWhen>(), It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task Constructor_WithDeduplicationOptions_ShouldInitialize()
    {
        var options = new DeduplicationOptions
        {
            CacheExpiration = TimeSpan.FromHours(12),
            ProcessingTimeout = TimeSpan.FromMinutes(5),
            KeyPrefix = "custom:",
            MaxRetryCount = 5
        };

        var sut = CreateSutWithOptions(options);

        Assert.True(sut.IsUsingRedis);
        Assert.Equal(0, sut.ConsecutiveFailures);
    }

    [Fact]
    public async Task FullWorkflow_ProcessingToCompleted_ShouldWork()
    {
        SetupHashGetAllEmpty();

        var sut = CreateSut();

        var markResult = await sut.TryMarkAsProcessingAsync("evt_workflow");
        Assert.False(markResult.IsDuplicate);

        _databaseMock
            .Setup(x => x.HashGetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync("completed");

        var isProcessed = await sut.IsProcessedAsync("evt_workflow");
        Assert.True(isProcessed);
    }

    [Fact]
    public async Task FullWorkflow_ProcessingToRollback_ShouldAllowReprocessing()
    {
        SetupHashGetAllEmpty();

        var sut = CreateSut();

        var markResult = await sut.TryMarkAsProcessingAsync("evt_rollback");
        Assert.False(markResult.IsDuplicate);

        _databaseMock
            .Setup(x => x.HashGetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync("processing");

        await sut.RollbackProcessingAsync("evt_rollback");
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_WhenCancellationRequested_ShouldRespectCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _databaseMock
            .Setup(x => x.HashGetAllAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new OperationCanceledException());

        var sut = CreateSut(maxRetryCount: 1);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => sut.TryMarkAsProcessingAsync("evt_cancel", cancellationToken: cts.Token));
    }

    [Fact]
    public async Task FallbackFullWorkflow_ShouldSupportCompleteStateMachine()
    {
        _databaseMock
            .Setup(x => x.HashGetAllAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Connection failed"));

        var sut = CreateSut(maxRetryCount: 1);

        for (int i = 0; i < 3; i++)
        {
            await sut.TryMarkAsProcessingAsync($"evt_{i}");
        }

        Assert.False(sut.IsUsingRedis);

        var markResult = await sut.TryMarkAsProcessingAsync("fb_evt");
        Assert.False(markResult.IsDuplicate);

        var duplicateResult = await sut.TryMarkAsProcessingAsync("fb_evt");
        Assert.True(duplicateResult.IsDuplicate);

        await sut.MarkAsCompletedAsync("fb_evt");

        var isProcessed = await sut.IsProcessedAsync("fb_evt");
        Assert.True(isProcessed);

        var status = await sut.GetStatusAsync("fb_evt");
        Assert.Equal(DeduplicationStatus.Completed, status);
    }

    [Fact]
    public async Task RedisRecovery_ShouldResetFailureCountOnSuccess()
    {
        var callCount = 0;
        _databaseMock
            .Setup(x => x.HashGetAllAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount <= 1)
                    throw new RedisException("Connection failed");
                return Array.Empty<HashEntry>();
            });

        var sut = CreateSut(maxRetryCount: 1);

        await sut.TryMarkAsProcessingAsync("evt_fail");
        Assert.Equal(1, sut.ConsecutiveFailures);

        await sut.TryMarkAsProcessingAsync("evt_success");
        Assert.Equal(0, sut.ConsecutiveFailures);
        Assert.True(sut.IsUsingRedis);
    }

    private void SetupHashGetAllEmpty()
    {
        _databaseMock
            .Setup(x => x.HashGetAllAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(Array.Empty<HashEntry>());
    }

    private void SetupHashGetAll(HashEntry[] entries)
    {
        _databaseMock
            .Setup(x => x.HashGetAllAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(entries);
    }
}
