// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Configuration;

namespace Mud.Feishu.Webhook.Tests;

/// <summary>
/// 飞书 Webhook 中间件集成测试示例
/// 注意：此文件为测试示例，需要在测试项目中安装所需的 NuGet 包：
/// - Microsoft.AspNetCore.TestHost
/// - Microsoft.AspNetCore.Mvc.Testing
/// - Xunit
/// - FluentAssertions
/// </summary>
public class FeishuWebhookMiddlewareIntegrationTests
{
    /*
     * 示例 1: 测试 URL 验证请求
     */
    /*
    [Fact]
    public async Task HandleEvent_ValidVerificationRequest_ReturnsChallenge()
    {
        // Arrange
        var webHostBuilder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddFeishuWebhookServiceBuilder(options =>
                {
                    options.VerificationToken = "test_token";
                    options.EncryptKey = "test_encrypt_key_32_chars_long!!";
                }).AddHandler<MockEventHandler>().Build();
            })
            .Configure(app =>
            {
                app.UseFeishuWebhook();
            });

        var server = new TestServer(webHostBuilder);
        var client = server.CreateClient();

        var payload = new
        {
            type = "url_verification",
            challenge = "test_challenge_12345",
            token = "test_token"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await client.PostAsync("/feishu/Webhook", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        result.GetProperty("challenge").GetString().Should().Be("test_challenge_12345");
    }
    */

    /*
     * 示例 2: 测试签名验证失败
     */
    /*
    [Fact]
    public async Task HandleEvent_InvalidSignature_ReturnsUnauthorized()
    {
        // Arrange
        var webHostBuilder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddFeishuWebhookServiceBuilder(options =>
                {
                    options.VerificationToken = "test_token";
                    options.EncryptKey = "test_encrypt_key_32_chars_long!!";
                    options.EnforceHeaderSignatureValidation = true;
                }).AddHandler<MockEventHandler>().Build();
            })
            .Configure(app =>
            {
                app.UseFeishuWebhook();
            });

        var server = new TestServer(webHostBuilder);
        var client = server.CreateClient();

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var nonce = "test_nonce";

        var payload = new
        {
            encrypt = "invalid_encrypt_data",
            timestamp,
            nonce
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        // 发送无效签名
        client.DefaultRequestHeaders.Add("X-Lark-Signature", "invalid_signature");

        // Act
        var response = await client.PostAsync("/feishu/Webhook", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    */

    /*
     * 示例 3: 测试事件去重
     */
    /*
    [Fact]
    public async Task HandleEvent_DuplicateEvent_ReturnsSuccessWithoutProcessing()
    {
        // Arrange
        var webHostBuilder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddFeishuWebhookServiceBuilder(options =>
                {
                    options.VerificationToken = "test_token";
                    options.EncryptKey = "test_encrypt_key_32_chars_long!!";
                }).AddHandler<MockEventHandler>().Build();
            })
            .Configure(app =>
            {
                app.UseFeishuWebhook();
            });

        var server = new TestServer(webHostBuilder);
        var client = server.CreateClient();

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var nonce = "test_nonce_1";
        var encrypt = "test_encrypt_data";

        // 计算签名
        var signString = $"{timestamp}\n{nonce}\n{encrypt}";
        using var hmac = new System.Security.Cryptography.HMACSHA256(
            System.Text.Encoding.UTF8.GetBytes("test_encrypt_key_32_chars_long!!"));
        var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signString));
        var signature = Convert.ToBase64String(hashBytes);

        var payload = new
        {
            encrypt,
            timestamp,
            nonce,
            signature
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        client.DefaultRequestHeaders.Add("X-Lark-Signature", signature);

        // 第一次请求
        var response1 = await client.PostAsync("/feishu/Webhook", content);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        // 第二次请求（相同事件）
        var response2 = await client.PostAsync("/feishu/Webhook", content);
        response2.StatusCode.Should().Be(HttpStatusCode.OK); // 去重仍返回成功，避免飞书重试
    }
    */

    /*
     * 示例 4: 测试并发控制
     */
    /*
    [Fact]
    public async Task HandleEvent_ConcurrentRequests_RespectsMaxConcurrency()
    {
        // Arrange
        var webHostBuilder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddFeishuWebhookServiceBuilder(options =>
                {
                    options.VerificationToken = "test_token";
                    options.EncryptKey = "test_encrypt_key_32_chars_long!!";
                    options.MaxConcurrentEvents = 2; // 限制并发为 2
                }).AddHandler<SlowMockEventHandler>().Build();
            })
            .Configure(app =>
            {
                app.UseFeishuWebhook();
            });

        var server = new TestServer(webHostBuilder);
        var client = server.CreateClient();

        var tasks = new List<Task<HttpResponseMessage>>();

        // Act
        // 发送 5 个并发请求
        for (int i = 0; i < 5; i++)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var nonce = $"test_nonce_{i}";
            var encrypt = $"test_encrypt_data_{i}";

            var signString = $"{timestamp}\n{nonce}\n{encrypt}";
            using var hmac = new System.Security.Cryptography.HMACSHA256(
                System.Text.Encoding.UTF8.GetBytes("test_encrypt_key_32_chars_long!!"));
            var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signString));
            var signature = Convert.ToBase64String(hashBytes);

            var payload = new
            {
                encrypt,
                timestamp,
                nonce,
                signature
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "/feishu/Webhook");
            request.Content = content;
            request.Headers.Add("X-Lark-Signature", signature);

            tasks.Add(client.SendAsync(request));
        }

        // Assert
        var responses = await Task.WhenAll(tasks);
        responses.All(r => r.StatusCode == HttpStatusCode.OK).Should().BeTrue();
    }
    */

    /*
     * 示例 5: 测试超时处理
     */
    /*
    [Fact]
    public async Task HandleEvent_SlowHandler_ReturnsTimeoutError()
    {
        // Arrange
        var webHostBuilder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddFeishuWebhookServiceBuilder(options =>
                {
                    options.VerificationToken = "test_token";
                    options.EncryptKey = "test_encrypt_key_32_chars_long!!";
                    options.EventHandlingTimeoutMs = 1000; // 1 秒超时
                }).AddHandler<SlowMockEventHandler>().Build();
            })
            .Configure(app =>
            {
                app.UseFeishuWebhook();
            });

        var server = new TestServer(webHostBuilder);
        var client = server.CreateClient();

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var nonce = "test_nonce";
        var encrypt = "test_encrypt_data";

        var signString = $"{timestamp}\n{nonce}\n{encrypt}";
        using var hmac = new System.Security.Cryptography.HMACSHA256(
            System.Text.Encoding.UTF8.GetBytes("test_encrypt_key_32_chars_long!!"));
        var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signString));
        var signature = Convert.ToBase64String(hashBytes);

        var payload = new
        {
            encrypt,
            timestamp,
            nonce,
            signature
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        client.DefaultRequestHeaders.Add("X-Lark-Signature", signature);

        // Act
        var response = await client.PostAsync("/feishu/Webhook", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        result.GetProperty("error").GetProperty("message").GetString().Should().Contain("timeout");
    }
    */
}

/*
/// <summary>
/// 模拟事件处理器（用于测试）
/// </summary>
public class MockEventHandler : IFeishuEventHandler
{
    public string SupportedEventType => "mock.event.v1";

    public Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

/*
/// <summary>
/// 模拟慢速事件处理器（用于测试并发和超时）
/// </summary>
public class SlowMockEventHandler : IFeishuEventHandler
{
    public string SupportedEventType => "mock.event.v1";

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        // 模拟慢速处理（5 秒）
        await Task.Delay(5000, cancellationToken);
    }
}
*/
