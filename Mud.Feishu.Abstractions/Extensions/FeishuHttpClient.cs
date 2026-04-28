// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Mud.Feishu.Abstractions;

internal class FeishuHttpClient : IEnhancedHttpClient
{
    private readonly IEnhancedHttpClient _innerClient;
    private readonly ILogger<FeishuHttpClient> _logger;
    private readonly IOptions<JsonSerializerOptions> _jsonSerializerOptions;

    public FeishuHttpClient(
        IEnhancedHttpClient innerClient,
        ILogger<FeishuHttpClient> logger,
        IOptions<JsonSerializerOptions> serializerOptions)
    {
        _innerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonSerializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));
    }

    public Uri? BaseAddress => _innerClient.BaseAddress;

    public IEnhancedHttpClient WithBaseAddress(string baseAddress)
    {
        return _innerClient.WithBaseAddress(baseAddress);
    }

    public IEnhancedHttpClient WithBaseAddress(Uri baseAddress)
    {
        return _innerClient.WithBaseAddress(baseAddress);
    }

    public Task<TResult?> SendAsync<TResult>(HttpRequestMessage request, object? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
    {
        return _innerClient.SendAsync<TResult>(request, jsonSerializerOptions, cancellationToken);
    }

    public Task<byte[]?> DownloadAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        return _innerClient.DownloadAsync(request, cancellationToken);
    }

    public Task<FileInfo> DownloadLargeAsync(HttpRequestMessage request, string filePath, bool overwrite = true, CancellationToken cancellationToken = default)
    {
        return _innerClient.DownloadLargeAsync(request, filePath, overwrite, cancellationToken);
    }

    public Task<HttpResponseMessage> SendRawAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        return _innerClient.SendRawAsync(request, cancellationToken);
    }

    public Task<Stream> SendStreamAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        return _innerClient.SendStreamAsync(request, cancellationToken);
    }

    public Task<TResult?> GetAsync<TResult>(string requestUri, CancellationToken cancellationToken = default)
    {
        return _innerClient.GetAsync<TResult>(requestUri, cancellationToken);
    }

    public Task<TResult?> PostAsJsonAsync<TRequest, TResult>(string requestUri, TRequest requestData, CancellationToken cancellationToken = default)
    {
        return _innerClient.PostAsJsonAsync<TRequest, TResult>(requestUri, requestData, cancellationToken);
    }

    public Task<TResult?> PutAsJsonAsync<TRequest, TResult>(string requestUri, TRequest requestData, CancellationToken cancellationToken = default)
    {
        return _innerClient.PutAsJsonAsync<TRequest, TResult>(requestUri, requestData, cancellationToken);
    }

    public Task<TResult?> DeleteAsJsonAsync<TResult>(string requestUri, CancellationToken cancellationToken = default)
    {
        return _innerClient.DeleteAsJsonAsync<TResult>(requestUri, cancellationToken);
    }

    public Task<TResult?> DeleteAsJsonAsync<TRequest, TResult>(string requestUri, TRequest requestData, CancellationToken cancellationToken = default)
    {
        return _innerClient.DeleteAsJsonAsync<TRequest, TResult>(requestUri, requestData, cancellationToken);
    }

    public Task<TResult?> PatchAsJsonAsync<TRequest, TResult>(string requestUri, TRequest requestData, CancellationToken cancellationToken = default)
    {
        return _innerClient.PatchAsJsonAsync<TRequest, TResult>(requestUri, requestData, cancellationToken);
    }

    public Task<TResult?> SendXmlAsync<TResult>(HttpRequestMessage request, Encoding? encoding = null, CancellationToken cancellationToken = default)
    {
        return _innerClient.SendXmlAsync<TResult>(request, encoding, cancellationToken);
    }

    public Task<TResult?> PostAsXmlAsync<TRequest, TResult>(string requestUri, TRequest requestData, Encoding? encoding = null, CancellationToken cancellationToken = default)
    {
        return _innerClient.PostAsXmlAsync<TRequest, TResult>(requestUri, requestData, encoding, cancellationToken);
    }

    public Task<TResult?> PutAsXmlAsync<TRequest, TResult>(string requestUri, TRequest requestData, Encoding? encoding = null, CancellationToken cancellationToken = default)
    {
        return _innerClient.PutAsXmlAsync<TRequest, TResult>(requestUri, requestData, encoding, cancellationToken);
    }

    public Task<TResult?> GetXmlAsync<TResult>(string requestUri, Encoding? encoding = null, CancellationToken cancellationToken = default)
    {
        return _innerClient.GetXmlAsync<TResult>(requestUri, encoding, cancellationToken);
    }

    public string EncryptContent(object content, string propertyName = "data", SerializeType serializeType = SerializeType.Json)
    {
        _logger.LogWarning("飞书 SDK 不支持请求体加密功能，此方法不应被调用");
        throw new NotSupportedException("飞书 SDK 不需要请求体加密功能。如需使用加密功能，请使用其他支持加密的 HTTP 客户端实现。");
    }

    public string DecryptContent(string encryptedContent)
    {
        _logger.LogWarning("飞书 SDK 不支持响应解密功能，此方法不应被调用");
        throw new NotSupportedException("飞书 SDK 不需要响应解密功能。如需使用解密功能，请使用其他支持解密的 HTTP 客户端实现。");
    }

    public byte[] EncryptBytes(byte[] data)
    {
        _logger.LogWarning("飞书 SDK 不支持字节加密功能，此方法不应被调用");
        throw new NotSupportedException("飞书 SDK 不需要字节加密功能。如需使用加密功能，请使用其他支持加密的 HTTP 客户端实现。");
    }

    public byte[] DecryptBytes(byte[] encryptedData)
    {
        _logger.LogWarning("飞书 SDK 不支持字节解密功能，此方法不应被调用");
        throw new NotSupportedException("飞书 SDK 不需要字节解密功能。如需使用解密功能，请使用其他支持解密的 HTTP 客户端实现。");
    }
}
