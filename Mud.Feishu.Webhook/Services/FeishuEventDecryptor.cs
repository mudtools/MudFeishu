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
/// <remarks>
/// <b>F-5 热路径</b>：AES-256-CBC 解密与 SHA-256 派生均为微秒级，因此
/// ① 不再用 <c>Task.Run</c> 切换到线程池（切换开销大于计算本身，且会让
/// <c>DecryptionTimeoutMs</c> 的取消语义依赖调度时机）；② 派生密钥按 EncryptKey 缓存
/// （基数 = 应用数，密钥轮转只会新增条目）。
/// </remarks>
public class FeishuEventDecryptor(ILogger<FeishuEventDecryptor> logger) : IFeishuEventDecryptor
{
    private readonly ILogger<FeishuEventDecryptor> _logger = logger;
    private const int BlockSize = 16;

    /// <summary>
    /// 派生密钥缓存：EncryptKey → SHA-256(UTF8(EncryptKey)) 的 32 字节。
    /// </summary>
    /// <remarks>
    /// 无容量上限是**有意为之**：键空间是"宿主配置的应用数"（通常 1~10），
    /// 不是请求数；加上限/清空只会在密钥轮转时制造无谓的抖动。
    /// </remarks>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte[]> KeyCache = new(StringComparer.Ordinal);

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
                    // v2.0 / v1.0 / data 包裹形态统一由共享解析器解析（单一真源）。
                    // 本地按根级 event_id/event_type/tenant_key/app_id 读取的解析
                    // 与官方 v1.0 格式（根级 uuid/token/ts、事件字段在 event 内）不符，
                    // 会导致 v1 事件五个关键字段全空、被 fail-closed 400 拒绝。
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
    /// <remarks>
    /// 同步计算（F-5）：AES 解密 + SHA-256 派生合计微秒级，<c>Task.Run</c> 的线程切换
    /// 反而更贵，并会让取消在所有 TFM 上依赖线程池调度时机。取消检查保留在入口，
    /// 语义仍由中间的 <c>DecryptionTimeoutMs</c> → <c>OperationCanceledException</c> → 503 承担。
    /// </remarks>
    private Task<string?> DecryptAes256CbcAsync(byte[] encryptedBytes, string encryptKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("开始解密，密钥长度: {KeyLength}, 加密数据长度: {DataLength}", encryptKey.Length, encryptedBytes.Length);

            if (encryptedBytes.Length <= BlockSize)
            {
                // 短密文：连 IV 都不完整，直接判失败（返回 null → 中间件 400），不抛加密库异常
                _logger.LogError("密文长度 {Length} 不足以容纳 IV 与数据块", encryptedBytes.Length);
                return Task.FromResult<string?>(null);
            }

            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = GetOrCreateDerivedKey(encryptKey);
            var iv = new byte[BlockSize];
            Buffer.BlockCopy(encryptedBytes, 0, iv, 0, BlockSize);
            aes.IV = iv;

            using var transform = aes.CreateDecryptor();
            var result = transform.TransformFinalBlock(encryptedBytes, BlockSize, encryptedBytes.Length - BlockSize);

            cancellationToken.ThrowIfCancellationRequested();
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("解密成功，结果长度: {ResultLength}", result.Length);
            return Task.FromResult<string?>(Encoding.UTF8.GetString(result));
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("AES 解密操作被取消");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AES 解密时发生错误");
            return Task.FromResult<string?>(null);
        }
    }

    /// <summary>
    /// 取得（并缓存）由 EncryptKey 派生的 32 字节 AES 密钥。
    /// </summary>
    /// <param name="encryptKey">飞书 EncryptKey（32 字符）</param>
    /// <returns>SHA-256(UTF8(EncryptKey))</returns>
    private static byte[] GetOrCreateDerivedKey(string encryptKey)
    {
        if (KeyCache.TryGetValue(encryptKey, out var cached))
            return cached;

#if NET6_0_OR_GREATER
        var keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(encryptKey));
#else
        byte[] keyBytes;
        using (var sha256 = SHA256.Create())
        {
            keyBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(encryptKey));
        }
#endif
        // GetOrAdd：并发下以首个写入为准（内容相同，无差异）
        return KeyCache.GetOrAdd(encryptKey, keyBytes);
    }
}
