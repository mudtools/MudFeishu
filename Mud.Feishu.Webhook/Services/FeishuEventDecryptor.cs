// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;

namespace Mud.Feishu.Webhook;

/// <summary>
/// 飞书事件解密服务实现
/// </summary>
public class FeishuEventDecryptor : IFeishuEventDecryptor
{
    private readonly ILogger<FeishuEventDecryptor> _logger;
    private const int BlockSize = 16;
    /// <inheritdoc />
    public FeishuEventDecryptor(ILogger<FeishuEventDecryptor> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<EventData?> DecryptAsync(string encryptedData, string encryptKey, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("开始解密事件数据");

            // Base64 解码
            var encryptedBytes = Convert.FromBase64String(encryptedData);

            // 使用 AES-256-CBC 解密
            var decryptedJson = await DecryptAes256CbcAsync(encryptedBytes, encryptKey, cancellationToken);

            if (string.IsNullOrEmpty(decryptedJson))
            {
                _logger.LogError("事件数据解密失败");
                return null;
            }

            _logger.LogDebug("事件数据解密成功，数据长度: {Length}", decryptedJson.Length);

            // 解析事件数据（支持 v1.0 和 v2.0 版本，以及 URL 验证请求）
            EventData eventData;
            string? schemaVersion = null;

            using (var jsonDoc = JsonDocument.Parse(decryptedJson!))
            {
                var root = jsonDoc.RootElement;

                // 检查是否为 URL 验证请求
                if (root.TryGetProperty("type", out var typeElement) && typeElement.GetString() == "url_verification")
                {
                    // 特殊处理 URL 验证请求
                    eventData = new EventData();
                    eventData.EventType = "url_verification";
                    // 将整个 JSON 作为 Event，这样 HandleEncryptedVerificationAsync 可以从中提取 challenge
                    eventData.Event = decryptedJson;
                }
                // 检查是否为v2.0版本
                else if (root.TryGetProperty("schema", out var schemaElement))
                {
                    schemaVersion = schemaElement.GetString();
                    // v2.0版本解析
                    eventData = ParseV2Event(root);
                }
                else
                {
                    // v1.0版本：手动解析，以正确处理create_time字段是字符串的情况
                    eventData = ParseV1Event(root);
                }
            }

            if (!string.IsNullOrEmpty(eventData.EventType) && _logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("事件数据解密成功 - EventType: [{EventType}], EventId: [{EventId}], Schema: {Schema}",
                    eventData.EventType, eventData.EventId, schemaVersion ?? "v1.0");
            }
            else
            {
                // EventType 为空可能是 URL 验证请求或其他非事件请求，这是正常行为
                _logger.LogDebug("事件数据解密后EventType为空（可能是URL验证请求），解密数据长度: {Length}", decryptedJson!.Length);
            }

            return eventData;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("解密事件数据操作被取消");
            throw;
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "事件数据 Base64 解码失败，数据格式无效");
            return null;
        }
        catch (System.Security.Cryptography.CryptographicException ex)
        {
            _logger.LogError(ex, "事件数据 AES 解密失败，密钥或数据可能不正确");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "事件数据 JSON 解析失败");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解密事件数据时发生未知错误");
            throw new InvalidOperationException("解密事件数据时发生错误", ex);
        }
    }

    /// <summary>
    /// 解析时间戳字段（支持字符串和数字格式，自动转换为秒）
    /// </summary>
    private static long ParseCreateTime(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String &&
            long.TryParse(element.GetString(), out var timeLong))
        {
            return timeLong / 1000; // 转换为秒
        }
        if (element.TryGetInt64(out var timeInt))
        {
            return timeInt / 1000;
        }
        return 0;
    }

    /// <summary>
    /// 解析v2.0版本的事件
    /// </summary>
    private EventData ParseV2Event(JsonElement root)
    {
        var eventData = new EventData();

        // 解析header
        if (root.TryGetProperty("header", out var headerElement))
        {
            // 构建 Header 对象，保留完整的 v2.0 header 数据
            var header = new FeishuEventHeader { Schema = "2.0" };

            if (headerElement.TryGetProperty("event_id", out var eventIdElement))
            {
                var eventId = eventIdElement.GetString() ?? string.Empty;
                header.EventId = eventId;
                eventData.EventId = eventId;
            }

            if (headerElement.TryGetProperty("event_type", out var eventTypeElement))
            {
                var eventType = eventTypeElement.GetString() ?? string.Empty;
                header.EventType = eventType;
                eventData.EventType = eventType;
            }

            if (headerElement.TryGetProperty("create_time", out var createTimeElement))
            {
                header.CreateTime = createTimeElement.ValueKind == JsonValueKind.String
                    ? createTimeElement.GetString()
                    : createTimeElement.TryGetInt64(out var ct) ? ct.ToString() : null;
                eventData.CreateTime = ParseCreateTime(createTimeElement);
            }

            if (headerElement.TryGetProperty("token", out var tokenElement))
                header.Token = tokenElement.GetString();

            if (headerElement.TryGetProperty("tenant_key", out var tenantKeyElement))
            {
                header.TenantKey = tenantKeyElement.GetString() ?? string.Empty;
                eventData.TenantKey = header.TenantKey;
            }

            if (headerElement.TryGetProperty("app_id", out var appIdElement))
            {
                header.AppId = appIdElement.GetString() ?? string.Empty;
                eventData.AppId = header.AppId;
            }

            // 设置完整 Header
            eventData.Header = header;
        }

        // 解析 schema（在 header 之外）
        if (root.TryGetProperty("schema", out var schemaElement))
        {
            eventData.Header ??= new FeishuEventHeader();
            eventData.Header.Schema = schemaElement.GetString();
        }

        // 解析event
        if (root.TryGetProperty("event", out var eventElement))
        {
            // 将event对象转换为原始JSON字符串,避免依赖已释放的JsonDocument
            // 这样可以确保eventData.Event不依赖于using块内的JsonDocument
            eventData.Event = eventElement.GetRawText();
        }

        return eventData;
    }

    /// <summary>
    /// 解析v1.0版本的事件
    /// </summary>
    private EventData ParseV1Event(JsonElement root)
    {
        var eventData = new EventData();

        // 解析基本字段
        if (root.TryGetProperty("event_id", out var eventIdElement))
            eventData.EventId = eventIdElement.GetString() ?? string.Empty;

        if (root.TryGetProperty("event_type", out var eventTypeElement))
            eventData.EventType = eventTypeElement.GetString() ?? string.Empty;

        if (root.TryGetProperty("create_time", out var createTimeElement))
        {
            eventData.CreateTime = ParseCreateTime(createTimeElement);
        }

        if (root.TryGetProperty("tenant_key", out var tenantKeyElement))
            eventData.TenantKey = tenantKeyElement.GetString() ?? string.Empty;

        if (root.TryGetProperty("app_id", out var appIdElement))
            eventData.AppId = appIdElement.GetString() ?? string.Empty;

        // 解析event
        if (root.TryGetProperty("event", out var eventElement))
        {
            // 将event对象转换为原始JSON字符串,避免依赖已释放的JsonDocument
            // 这样可以确保eventData.Event不依赖于using块内的JsonDocument
            eventData.Event = eventElement.GetRawText();
        }

        return eventData;
    }



    /// <summary>
    /// 使用 AES-256-CBC 解密数据
    /// </summary>
    /// <param name="encryptedBytes">加密的字节数组</param>
    /// <param name="encryptKey">加密密钥</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>解密后的字符串</returns>
    private async Task<string?> DecryptAes256CbcAsync(byte[] encryptedBytes, string encryptKey, CancellationToken cancellationToken = default)
    {
        // 使用 Task.Run 将 CPU 密集型操作放到线程池执行，避免阻塞请求线程
        return await Task.Run(() =>
        {
            try
            {
                // 检查取消令牌
                cancellationToken.ThrowIfCancellationRequested();

                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug("开始解密，密钥长度: {KeyLength}, 加密数据长度: {DataLength}", encryptKey.Length, encryptedBytes.Length);

                using var aes = Aes.Create();
                // 飞书的 EncryptKey 直接使用 UTF-8 编码后进行 SHA-256 哈希得到 32 字节密钥
                byte[] keyBytes;
                using (var sha256 = SHA256.Create())
                {
                    keyBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(encryptKey));
                }
                aes.Key = keyBytes;
                aes.Mode = CipherMode.CBC;
                aes.IV = encryptedBytes.Take(BlockSize).ToArray();

                using var transform = aes.CreateDecryptor();
                var result = transform.TransformFinalBlock(encryptedBytes, BlockSize, encryptedBytes.Length - BlockSize);

                // 再次检查取消令牌
                cancellationToken.ThrowIfCancellationRequested();
                if (result == null)
                {
                    _logger.LogError("解密失败，结果为空");
                    return null;
                }
                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug("解密成功，结果长度: {ResultLength}", result?.Length ?? 0);
                return Encoding.UTF8.GetString(result!);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("AES 解密操作被取消");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AES 解密时发生错误");
                return null;
            }
        }, cancellationToken);
    }
}
