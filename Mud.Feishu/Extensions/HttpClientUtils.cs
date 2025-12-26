// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Mud.Feishu.Extensions;

/// <summary>
/// 为HttpClient提供扩展方法的工具类
/// </summary>
internal static class HttpClientExtensions
{
    // 默认缓冲区大小（80KB）- 比默认的80K稍大，适合文件下载
    private const int DefaultBufferSize = 81920;

    /// <summary>
    /// 发送HTTP请求并反序列化响应结果
    /// </summary>
    /// <typeparam name="TResult">响应结果的类型，必须是类类型并具有无参构造函数</typeparam>
    /// <param name="client">HTTP客户端实例</param>
    /// <param name="httpRequestMessage">HTTP请求消息</param>
    /// <param name="jsonSerializerOptions">JSON序列化选项，可选</param>
    /// <param name="logger">日志记录器，可选</param>
    /// <param name="cancellationToken">取消令牌，用于取消操作</param>
    /// <returns>反序列化后的响应结果，如果响应内容为空则返回默认值</returns>
    /// <exception cref="ArgumentNullException">当client或httpRequestMessage为null时抛出</exception>
    /// <exception cref="HttpRequestException">当HTTP请求失败时抛出</exception>
    public static async Task<TResult?> SendRequestAsync<TResult>(
        this HttpClient client,
        HttpRequestMessage httpRequestMessage,
        JsonSerializerOptions? jsonSerializerOptions = null,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        ExceptionUtils.ThrowIfNull(client);
        ExceptionUtils.ThrowIfNull(httpRequestMessage);

        string? requestUri = httpRequestMessage.RequestUri?.ToString();

        try
        {
            using var response = await client.SendAsync(httpRequestMessage,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            await EnsureSuccessStatusCodeAsync(response, logger, cancellationToken);

            // 直接检查Content-Length头，避免不必要的流操作
            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength == 0)
            {
                logger?.LogDebug("响应内容为空，返回默认值: {Url}", requestUri);
                return default;
            }

            // 使用StreamReader包装流以提高性能
#if NETSTANDARD2_0
            using var stream = await response.Content.ReadAsStreamAsync();
#else
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
#endif

            // 使用默认的序列化选项如果未提供
            var options = jsonSerializerOptions ?? GetDefaultJsonSerializerOptions();

            return await JsonSerializer.DeserializeAsync<TResult>(stream, options, cancellationToken);
        }
        catch (Exception ex) when (ex is not HttpRequestException)
        {
            logger?.LogError(ex, "HTTP请求处理异常: {Url}", requestUri);
            throw new HttpRequestException($"HTTP请求处理失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 下载文件内容并以字节数组形式返回
    /// </summary>
    /// <param name="client">HttpClient实例</param>
    /// <param name="httpRequestMessage">HTTP请求消息</param>
    /// <param name="logger">日志记录器（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件内容的字节数组，如果请求失败则返回null</returns>
    /// <exception cref="ArgumentNullException">当client或httpRequestMessage为null时抛出</exception>
    /// <exception cref="HttpRequestException">当HTTP请求失败时抛出</exception>
    public static async Task<byte[]?> DownloadFileAsync(
       this HttpClient client,
       HttpRequestMessage httpRequestMessage,
       ILogger? logger = null,
       CancellationToken cancellationToken = default)
    {
        ExceptionUtils.ThrowIfNull(client);
        ExceptionUtils.ThrowIfNull(httpRequestMessage);

        string? requestUri = httpRequestMessage.RequestUri?.ToString();

        try
        {
            using var response = await client.SendAsync(httpRequestMessage,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            await EnsureSuccessStatusCodeAsync(response, logger, cancellationToken);

            // 检查Content-Length头，如果太大可以考虑使用流式处理
            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength > 10 * 1024 * 1024) // 10MB警告
            {
                logger?.LogWarning("下载文件较大: {Url}, 大小: {Size}MB",
                    requestUri, contentLength / (1024.0 * 1024.0));
            }
#if NETSTANDARD2_0
            return await response.Content.ReadAsByteArrayAsync();
#else
            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
#endif
        }
        catch (Exception ex) when (ex is not HttpRequestException)
        {
            logger?.LogError(ex, "文件下载异常: {Url}", requestUri);
            throw new HttpRequestException($"文件下载失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 下载大文件并保存到指定路径
    /// </summary>
    /// <param name="client">HttpClient实例</param>
    /// <param name="httpRequestMessage">HTTP请求消息</param>
    /// <param name="filePath">保存文件的完整路径</param>
    /// <param name="bufferSize">缓冲区大小（字节），可选</param>
    /// <param name="overwrite">是否覆盖已存在的文件，默认为true</param>
    /// <param name="logger">日志记录器（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下载的文件信息</returns>
    /// <exception cref="ArgumentNullException">当client或httpRequestMessage为null时抛出</exception>
    /// <exception cref="ArgumentException">当filePath无效时抛出</exception>
    /// <exception cref="HttpRequestException">当HTTP请求失败时抛出</exception>
    /// <exception cref="IOException">当文件操作失败时抛出</exception>
    public static async Task<FileInfo> DownloadLargeFileAsync(
      this HttpClient client,
      HttpRequestMessage httpRequestMessage,
      string filePath,
      int bufferSize = DefaultBufferSize,
      bool overwrite = true,
      ILogger? logger = null,
      CancellationToken cancellationToken = default)
    {
        ExceptionUtils.ThrowIfNull(client);
        ExceptionUtils.ThrowIfNull(httpRequestMessage);

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("文件路径不能为空", nameof(filePath));

        if (bufferSize <= 0)
            throw new ArgumentException("缓冲区大小必须大于0", nameof(bufferSize));

        string? requestUri = httpRequestMessage.RequestUri?.ToString();
        string directoryPath = Path.GetDirectoryName(filePath)!;

        try
        {
            // 确保目录存在
            if (!string.IsNullOrEmpty(directoryPath))
                Directory.CreateDirectory(directoryPath);

            // 检查文件是否已存在
            if (File.Exists(filePath))
            {
                if (overwrite)
                {
                    logger?.LogInformation("文件已存在，将被覆盖: {FilePath}", filePath);
                }
                else
                {
                    throw new IOException($"文件已存在: {filePath}");
                }
            }

            using var response = await client.SendAsync(httpRequestMessage,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            await EnsureSuccessStatusCodeAsync(response, logger, cancellationToken);

            // 获取文件大小信息
            var contentLength = response.Content.Headers.ContentLength;
            logger?.LogInformation("开始下载文件: {Url}, 大小: {Size}MB, 保存到: {FilePath}",
                requestUri,
                contentLength.HasValue ? contentLength.Value / (1024.0 * 1024.0) : "未知",
                filePath);


#if NETSTANDARD2_0
            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: bufferSize,
                useAsync: true);
#else
            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            // 使用FileStream异步模式，指定缓冲区大小
            await using var fileStream = new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: bufferSize,
                useAsync: true);
#endif
            // 使用CopyToAsync并指定缓冲区大小
            await contentStream.CopyToAsync(fileStream, bufferSize, cancellationToken);

            await fileStream.FlushAsync(cancellationToken);

            var fileInfo = new FileInfo(filePath);
            logger?.LogInformation("文件下载完成: {FilePath}, 大小: {Size}MB",
                filePath, fileInfo.Length / (1024.0 * 1024.0));

            return fileInfo;
        }
        catch (Exception ex) when (ex is not HttpRequestException and not ArgumentException)
        {
            // 清理部分下载的文件
            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch (Exception cleanupEx)
            {
                logger?.LogWarning(cleanupEx, "清理部分下载的文件失败: {FilePath}", filePath);
            }

            logger?.LogError(ex, "大文件下载异常: {Url}, 文件路径: {FilePath}", requestUri, filePath);
            throw new HttpRequestException($"大文件下载失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 确保HTTP响应状态码表示成功，否则抛出异常
    /// </summary>
    private static async Task EnsureSuccessStatusCodeAsync(
        HttpResponseMessage response,
        ILogger? logger,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var statusCode = (int)response.StatusCode;
        string errorContent = string.Empty;

        try
        {
#if NETSTANDARD2_0
            errorContent = await response.Content.ReadAsStringAsync();
#else
            errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
#endif
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "读取错误响应内容失败");
            errorContent = "[无法读取错误内容]";
        }

        string errorMessage = $"HTTP请求失败: {statusCode} {response.StatusCode} - {errorContent}";
        logger?.LogError("HTTP请求失败: {StatusCode}, 响应: {Response}", statusCode, errorContent);

        // 尝试释放响应内容
        response.Content.Dispose();
#if NETSTANDARD2_0
        throw new HttpRequestException(errorMessage, null);
#else
        throw new HttpRequestException(errorMessage, null, response.StatusCode);
#endif
    }

    /// <summary>
    /// 获取默认的JSON序列化选项
    /// </summary>
    private static JsonSerializerOptions GetDefaultJsonSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// 发送简单的GET请求并反序列化响应
    /// </summary>
    public static async Task<TResult?> GetAsync<TResult>(
        this HttpClient client,
        string requestUri,
        JsonSerializerOptions? jsonSerializerOptions = null,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        return await client.SendRequestAsync<TResult>(request, jsonSerializerOptions, logger, cancellationToken);
    }

    /// <summary>
    /// 发送JSON POST请求并反序列化响应
    /// </summary>
    public static async Task<TResult?> PostAsJsonAsync<TRequest, TResult>(
        this HttpClient client,
        string requestUri,
        TRequest requestData,
        JsonSerializerOptions? jsonSerializerOptions = null,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        var content = JsonSerializer.Serialize(requestData, jsonSerializerOptions ?? GetDefaultJsonSerializerOptions());
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        return await client.SendRequestAsync<TResult>(request, jsonSerializerOptions, logger, cancellationToken);
    }
}