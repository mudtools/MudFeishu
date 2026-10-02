// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.EventHandlers;

namespace Mud.Feishu.Webhook;

/// <summary>
/// 飞书事件解密服务实现
/// </summary>
public class FeishuEventDecryptor(ILogger<FeishuEventDecryptor> logger) : IFeishuEventDecryptor
{
    private readonly ILogger<FeishuEventDecryptor> _logger = logger;
    private const int BlockSize = 16;

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

            // 等价于 string.IsNullOrEmpty 判断（is not 模式保证后续 decryptedJson 非空的流分析在所有 TFM 下成立）
            if (decryptedJson is not { Length: > 0 })
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
                else
                {
                    // R-E1（AD-1）：v2.0 / v1.0 / data 包裹形态统一由共享解析器解析（单一真源）。
                    // 此前本地 ParseV1Event 按根级 event_id/event_type/tenant_key/app_id 读取，
                    // 与官方 v1.0 格式（根级 uuid/token/ts、事件字段在 event 内）不符，
                    // 导致 v1 事件五个关键字段全空、被 fail-closed 400 拒绝（E-P0-1）。
                    if (!FeishuEventDataParser.TryParse(root, out eventData, out _))
                    {
                        _logger.LogWarning("事件数据解析失败：无法确定事件类型（可能是畸形或非事件报文）");
                        return null;
                    }
                    schemaVersion = eventData.Schema;
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
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                // 飞书的 EncryptKey 直接使用 UTF-8 编码后进行 SHA-256 哈希得到 32 字节密钥
                byte[] keyBytes;
                using (var sha256 = SHA256.Create())
                {
                    keyBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(encryptKey));
                }
                aes.Key = keyBytes;
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
