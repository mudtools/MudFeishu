// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;

namespace Mud.Feishu.Webhook.Services;

/// <summary>
/// 飞书事件解密服务实现
/// </summary>
public class FeishuEventDecryptor : IFeishuEventDecryptor
{
    private readonly ILogger<FeishuEventDecryptor> _logger;

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

            // 反序列化为 EventData
            var eventData = JsonSerializer.Deserialize<EventData>(decryptedJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (eventData != null)
            {
                _logger.LogInformation("事件数据解密成功，事件类型: {EventType}, 事件ID: {EventId}",
                    eventData.EventType, eventData.EventId);
            }

            return eventData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解密事件数据时发生错误");
            return null;
        }
    }

    /// <summary>
    /// 使用 AES-256-CBC 解密数据
    /// </summary>
    /// <param name="encryptedBytes">加密的字节数组</param>
    /// <param name="key">加密密钥</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>解密后的字符串</returns>
    private async Task<string?> DecryptAes256CbcAsync(byte[] encryptedBytes, string key, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // 使方法保持异步签名

        try
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;

            // 从加密数据中提取 IV（前16字节）
            var iv = new byte[16];
            var actualEncryptedData = new byte[encryptedBytes.Length - 16];

            Array.Copy(encryptedBytes, 0, iv, 0, 16);
            Array.Copy(encryptedBytes, 16, actualEncryptedData, 0, actualEncryptedData.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(actualEncryptedData);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt, Encoding.UTF8);

            return srDecrypt.ReadToEnd();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AES 解密时发生错误");
            return null;
        }
    }
}