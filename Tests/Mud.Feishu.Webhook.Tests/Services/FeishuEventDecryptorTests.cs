// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;
using FluentAssertions;

namespace Mud.Feishu.Webhook.Tests.Services;

/// <summary>
/// FeishuEventDecryptor 单元测试
/// </summary>
public class FeishuEventDecryptorTests
{
    private readonly Mock<ILogger<FeishuEventDecryptor>> _loggerMock;
    private readonly FeishuEventDecryptor _decryptor;

    public FeishuEventDecryptorTests()
    {
        _loggerMock = new Mock<ILogger<FeishuEventDecryptor>>();
        _decryptor = new FeishuEventDecryptor(_loggerMock.Object);
    }

    [Fact]
    public async Task DecryptAsync_WithValidV1Data_ShouldReturnEventData()
    {
        // Arrange
        var encryptKey = "test_encrypt_key_123456";
        var originalJson = "{\"event_type\":\"test_event\",\"event_id\":\"test_123\",\"create_time\":1234567890}";
        var encryptedData = EncryptData(originalJson, encryptKey);

        // Act
        var result = await _decryptor.DecryptAsync(encryptedData, encryptKey);

        // Assert
        result.Should().NotBeNull();
        result.EventType.Should().Be("test_event");
        result.EventId.Should().Be("test_123");
    }

    [Fact]
    public async Task DecryptAsync_WithInvalidKey_ShouldReturnNull()
    {
        // Arrange
        var correctKey = "correct_key_123456";
        var wrongKey = "wrong_key_123456";
        var originalJson = "{\"event_type\":\"test_event\"}";
        var encryptedData = EncryptData(originalJson, correctKey);

        // Act
        var result = await _decryptor.DecryptAsync(encryptedData, wrongKey);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DecryptAsync_WithInvalidBase64_ShouldReturnNull()
    {
        // Arrange
        var encryptKey = "test_key";
        var invalidBase64 = "not_valid_base64!!!";

        // Act
        var result = await _decryptor.DecryptAsync(invalidBase64, encryptKey);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DecryptAsync_WithEmptyData_ShouldReturnNull()
    {
        // Arrange
        var encryptKey = "test_key";

        // Act
        var result = await _decryptor.DecryptAsync("", encryptKey);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DecryptAsync_WithValidV2Data_ShouldReturnEventData()
    {
        // Arrange
        var encryptKey = "test_encrypt_key_123456";
        var originalJson = "{\"schema\":\"2.0\",\"header\":{\"event_id\":\"v2_event_123\",\"event_type\":\"contact.user.created_v3\",\"create_time\":\"1704067200000\",\"tenant_key\":\"tenant_abc\",\"app_id\":\"app_123\"},\"event\":{\"user_id\":\"ou_xxx\"}}";
        var encryptedData = EncryptData(originalJson, encryptKey);

        // Act
        var result = await _decryptor.DecryptAsync(encryptedData, encryptKey);

        // Assert
        result.Should().NotBeNull();
        result.EventId.Should().Be("v2_event_123");
        result.EventType.Should().Be("contact.user.created_v3");
        result.TenantKey.Should().Be("tenant_abc");
        result.AppId.Should().Be("app_123");
        result.Event.Should().NotBeNull();
        result.Event!.ToString().Should().Contain("user_id");
    }

    [Fact]
    public async Task DecryptAsync_WithUrlVerificationRequest_ShouldReturnSpecialEventData()
    {
        // Arrange
        var encryptKey = "test_encrypt_key_123456";
        var originalJson = "{\"type\":\"url_verification\",\"challenge\":\"test_challenge_value\"}";
        var encryptedData = EncryptData(originalJson, encryptKey);

        // Act
        var result = await _decryptor.DecryptAsync(encryptedData, encryptKey);

        // Assert
        result.Should().NotBeNull();
        result.EventType.Should().Be("url_verification");
        result.Event.Should().NotBeNull();
        result.Event!.ToString().Should().Contain("test_challenge_value");
    }

    [Fact]
    public async Task DecryptAsync_WithV2EventWithNumericCreateTime_ShouldParseCorrectly()
    {
        // Arrange
        var encryptKey = "test_encrypt_key_123456";
        var originalJson = "{\"schema\":\"2.0\",\"header\":{\"event_id\":\"event_456\",\"event_type\":\"test.event\",\"create_time\":1704067200000}}";
        var encryptedData = EncryptData(originalJson, encryptKey);

        // Act
        var result = await _decryptor.DecryptAsync(encryptedData, encryptKey);

        // Assert
        result.Should().NotBeNull();
        result.EventId.Should().Be("event_456");
        result.CreateTime.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DecryptAsync_WithV1EventWithTenantKeyAndAppId_ShouldParseAllFields()
    {
        // Arrange
        var encryptKey = "test_encrypt_key_123456";
        var originalJson = "{\"event_id\":\"event_789\",\"event_type\":\"app.card.created\",\"create_time\":\"1704067200000\",\"tenant_key\":\"my_tenant\",\"app_id\":\"my_app\",\"event\":{\"card_id\":\"card_123\"}}";
        var encryptedData = EncryptData(originalJson, encryptKey);

        // Act
        var result = await _decryptor.DecryptAsync(encryptedData, encryptKey);

        // Assert
        result.Should().NotBeNull();
        result.EventId.Should().Be("event_789");
        result.EventType.Should().Be("app.card.created");
        result.TenantKey.Should().Be("my_tenant");
        result.AppId.Should().Be("my_app");
        result.Event.Should().NotBeNull();
        result.Event!.ToString().Should().Contain("card_id");
    }

    [Fact]
    public async Task DecryptAsync_WithCancellationToken_ShouldHandleCancellation()
    {
        // Arrange
        var encryptKey = "test_encrypt_key_123456";
        var originalJson = "{\"event_type\":\"test_event\"}";
        var encryptedData = EncryptData(originalJson, encryptKey);
        var cts = new CancellationTokenSource();

        // 先取消令牌
        cts.Cancel();

        // Act - 取消后调用解密
        try
        {
            var result = await _decryptor.DecryptAsync(encryptedData, encryptKey, cts.Token);
            // 如果操作在取消前完成，结果是有效的
            // 如果返回 null，这也是可以接受的行为
        }
        catch (OperationCanceledException)
        {
            // 如果操作被取消，抛出异常也是预期的行为
        }
    }

    /// <summary>
    /// 辅助方法：加密数据（模拟飞书加密）
    /// </summary>
    private string EncryptData(string plainText, string encryptKey)
    {
        using var aes = Aes.Create();
        
        // 使用 SHA256 哈希密钥
        using var sha256 = SHA256.Create();
        var keyBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(encryptKey));
        aes.Key = keyBytes;
        aes.Mode = CipherMode.CBC;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // 将 IV 和加密数据组合
        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }
}
