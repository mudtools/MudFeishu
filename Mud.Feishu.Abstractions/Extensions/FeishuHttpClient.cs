// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Mud.Feishu.Abstractions;

internal class FeishuHttpClient : EnhancedHttpClient, IEnhancedHttpClient
{
    private readonly ILogger<FeishuHttpClient> _logger;
    private readonly IOptions<JsonSerializerOptions> _jsonSerializerOptions;

    public FeishuHttpClient(
        HttpClient httpClient,
        ILogger<FeishuHttpClient> logger,
        bool? enableLogging,
        IOptions<JsonSerializerOptions> serializerOptions) : base(httpClient, logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonSerializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));

    }

    protected override JsonSerializerOptions? GetJsonSerializerOptions()
    {
        return _jsonSerializerOptions.Value;
    }

    /// <summary>
    /// 加密内容（当前 SDK 不支持此功能）
    /// </summary>
    /// <param name="content">要加密的内容</param>
    /// <param name="propertyName">属性名称</param>
    /// <param name="serializeType">序列化类型</param>
    /// <returns>不支持此操作</returns>
    /// <exception cref="NotSupportedException">始终抛出，因为飞书 SDK 不需要请求体加密功能</exception>
    public override string EncryptContent(object content, string propertyName = "data", SerializeType serializeType = SerializeType.Json)
    {
        _logger.LogWarning("飞书 SDK 不支持请求体加密功能，此方法不应被调用");
        throw new NotSupportedException("飞书 SDK 不需要请求体加密功能。如需使用加密功能，请使用其他支持加密的 HTTP 客户端实现。");
    }

    /// <summary>
    /// 解密内容（当前 SDK 不支持此功能）
    /// </summary>
    /// <param name="encryptedContent">要解密的加密字符串</param>
    /// <returns>不支持此操作</returns>
    /// <exception cref="NotSupportedException">始终抛出，因为飞书 SDK 不需要响应解密功能</exception>
    public override string DecryptContent(string encryptedContent)
    {
        _logger.LogWarning("飞书 SDK 不支持响应解密功能，此方法不应被调用");
        throw new NotSupportedException("飞书 SDK 不需要响应解密功能。如需使用解密功能，请使用其他支持解密的 HTTP 客户端实现。");
    }

    /// <summary>
    /// 加密字节数组（当前 SDK 不支持此功能）
    /// </summary>
    /// <param name="data">要加密的字节数组</param>
    /// <returns>不支持此操作</returns>
    /// <exception cref="NotSupportedException">始终抛出，因为飞书 SDK 不需要字节加密功能</exception>
    public override byte[] EncryptBytes(byte[] data)
    {
        _logger.LogWarning("飞书 SDK 不支持字节加密功能，此方法不应被调用");
        throw new NotSupportedException("飞书 SDK 不需要字节加密功能。如需使用加密功能，请使用其他支持加密的 HTTP 客户端实现。");
    }

    /// <summary>
    /// 解密字节数组（当前 SDK 不支持此功能）
    /// </summary>
    /// <param name="encryptedData">要解密的加密字节数组</param>
    /// <returns>不支持此操作</returns>
    /// <exception cref="NotSupportedException">始终抛出，因为飞书 SDK 不需要字节解密功能</exception>
    public override byte[] DecryptBytes(byte[] encryptedData)
    {
        _logger.LogWarning("飞书 SDK 不支持字节解密功能，此方法不应被调用");
        throw new NotSupportedException("飞书 SDK 不需要字节解密功能。如需使用解密功能，请使用其他支持解密的 HTTP 客户端实现。");
    }
}