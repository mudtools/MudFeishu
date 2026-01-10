// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.Webhook.Services;
using Xunit;
using Xunit.Abstractions;

namespace Mud.Feishu.Webhook.Tests;

/// <summary>
/// 飞书事件去重服务单元测试
/// </summary>
public class FeishuWebhookEventDeduplicatorTests
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<FeishuWebhookEventDeduplicator> _logger;

    public FeishuWebhookEventDeduplicatorTests(ITestOutputHelper output)
    {
        _output = output;
        _logger = new TestLogger<FeishuWebhookEventDeduplicator>(output);
    }

    [Fact]
    public void TryMarkAsProcessed_NewEvent_ReturnsFalse()
    {
        // Arrange
        var deduplicator = new FeishuWebhookEventDeduplicator(_logger);
        const string eventId = "test_event_id_1";

        // Act
        var result = deduplicator.TryMarkAsProcessed(eventId);

        // Assert
        Assert.False(result); // 未处理过
    }

    [Fact]
    public void TryMarkAsProcessed_DuplicateEvent_ReturnsTrue()
    {
        // Arrange
        var deduplicator = new FeishuWebhookEventDeduplicator(_logger);
        const string eventId = "test_event_id_2";

        // 第一次标记
        var firstResult = deduplicator.TryMarkAsProcessed(eventId);
        Assert.False(firstResult);

        // 第二次标记（重复）
        var secondResult = deduplicator.TryMarkAsProcessed(eventId);

        // Assert
        Assert.True(secondResult); // 已处理过
    }

    [Fact]
    public void TryMarkAsProcessed_NullEventId_ReturnsFalse()
    {
        // Arrange
        var deduplicator = new FeishuWebhookEventDeduplicator(_logger);

        // Act
        var result = deduplicator.TryMarkAsProcessed(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryMarkAsProcessed_EmptyEventId_ReturnsFalse()
    {
        // Arrange
        var deduplicator = new FeishuWebhookEventDeduplicator(_logger);

        // Act
        var result = deduplicator.TryMarkAsProcessed(string.Empty);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsProcessed_UnprocessedEvent_ReturnsFalse()
    {
        // Arrange
        var deduplicator = new FeishuWebhookEventDeduplicator(_logger);
        const string eventId = "test_event_id_3";

        // Act
        var result = deduplicator.IsProcessed(eventId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsProcessed_ProcessedEvent_ReturnsTrue()
    {
        // Arrange
        var deduplicator = new FeishuWebhookEventDeduplicator(_logger);
        const string eventId = "test_event_id_4";

        // 先标记为已处理
        deduplicator.TryMarkAsProcessed(eventId);

        // Act
        var result = deduplicator.IsProcessed(eventId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetCacheStats_ReturnsCorrectStats()
    {
        // Arrange
        var deduplicator = new FeishuWebhookEventDeduplicator(_logger);
        const int eventCount = 5;

        // 添加 5 个事件
        for (int i = 0; i < eventCount; i++)
        {
            deduplicator.TryMarkAsProcessed($"event_{i}");
        }

        // Act
        var (totalCached, expiredCount) = deduplicator.GetCacheStats();

        // Assert
        Assert.Equal(eventCount, totalCached);
        Assert.Equal(0, expiredCount); // 刚添加的事件不应该过期
    }

    [Fact]
    public void ClearCache_ClearsAllCachedEvents()
    {
        // Arrange
        var deduplicator = new FeishuWebhookEventDeduplicator(_logger);
        const int eventCount = 5;

        // 添加事件
        for (int i = 0; i < eventCount; i++)
        {
            deduplicator.TryMarkAsProcessed($"event_{i}");
        }

        // 确认缓存中有事件
        var (totalCached, _) = deduplicator.GetCacheStats();
        Assert.Equal(eventCount, totalCached);

        // Act
        deduplicator.ClearCache();

        // Assert
        var (newTotalCached, _) = deduplicator.GetCacheStats();
        Assert.Equal(0, newTotalCached);
    }
}

/// <summary>
/// 飞书 Nonce 去重服务单元测试
/// </summary>
public class FeishuWebhookNonceDeduplicatorTests
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<FeishuWebhookNonceDeduplicator> _logger;

    public FeishuWebhookNonceDeduplicatorTests(ITestOutputHelper output)
    {
        _output = output;
        _logger = new TestLogger<FeishuWebhookNonceDeduplicator>(output);
    }

    [Fact]
    public void TryMarkAsUsed_NewNonce_ReturnsFalse()
    {
        // Arrange
        var deduplicator = new FeishuWebhookNonceDeduplicator(_logger);
        const string nonce = "test_nonce_1";

        // Act
        var result = deduplicator.TryMarkAsUsed(nonce);

        // Assert
        Assert.False(result); // 未使用过
    }

    [Fact]
    public void TryMarkAsUsed_ReuseNonce_ReturnsTrue()
    {
        // Arrange
        var deduplicator = new FeishuWebhookNonceDeduplicator(_logger);
        const string nonce = "test_nonce_2";

        // 第一次使用
        var firstResult = deduplicator.TryMarkAsUsed(nonce);
        Assert.False(firstResult);

        // 第二次使用（重放）
        var secondResult = deduplicator.TryMarkAsUsed(nonce);

        // Assert
        Assert.True(secondResult); // 已使用过，拒绝重放
    }

    [Fact]
    public void TryMarkAsUsed_NullNonce_ReturnsFalse()
    {
        // Arrange
        var deduplicator = new FeishuWebhookNonceDeduplicator(_logger);

        // Act
        var result = deduplicator.TryMarkAsUsed(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryMarkAsUsed_EmptyNonce_ReturnsFalse()
    {
        // Arrange
        var deduplicator = new FeishuWebhookNonceDeduplicator(_logger);

        // Act
        var result = deduplicator.TryMarkAsUsed(string.Empty);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsUsed_UnusedNonce_ReturnsFalse()
    {
        // Arrange
        var deduplicator = new FeishuWebhookNonceDeduplicator(_logger);
        const string nonce = "test_nonce_3";

        // Act
        var result = deduplicator.IsUsed(nonce);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsUsed_UsedNonce_ReturnsTrue()
    {
        // Arrange
        var deduplicator = new FeishuWebhookNonceDeduplicator(_logger);
        const string nonce = "test_nonce_4";

        // 先标记为已使用
        deduplicator.TryMarkAsUsed(nonce);

        // Act
        var result = deduplicator.IsUsed(nonce);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetCacheCount_ReturnsCorrectCount()
    {
        // Arrange
        var deduplicator = new FeishuWebhookNonceDeduplicator(_logger);
        const int nonceCount = 10;

        // 添加 nonce
        for (int i = 0; i < nonceCount; i++)
        {
            deduplicator.TryMarkAsUsed($"nonce_{i}");
        }

        // Act
        var count = deduplicator.GetCacheCount();

        // Assert
        Assert.Equal(nonceCount, count);
    }

    [Fact]
    public void ClearCache_ClearsAllCachedNonces()
    {
        // Arrange
        var deduplicator = new FeishuWebhookNonceDeduplicator(_logger);
        const int nonceCount = 5;

        // 添加 nonce
        for (int i = 0; i < nonceCount; i++)
        {
            deduplicator.TryMarkAsUsed($"nonce_{i}");
        }

        // 确认缓存中有 nonce
        var count = deduplicator.GetCacheCount();
        Assert.Equal(nonceCount, count);

        // Act
        deduplicator.ClearCache();

        // Assert
        var newCount = deduplicator.GetCacheCount();
        Assert.Equal(0, newCount);
    }
}
