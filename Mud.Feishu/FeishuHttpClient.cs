// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.Extensions;

namespace Mud.Feishu;

/// <summary>
/// 飞书HTTP客户端接口，用于发送飞书相关的HTTP请求
/// </summary>
public interface IFeishuHttpClient
{
    /// <summary>
    /// 发送飞书请求并返回指定类型的结果
    /// </summary>
    /// <typeparam name="TResult">期望的返回结果类型，必须是类类型并具有无参构造函数</typeparam>
    /// <param name="request">要发送的HTTP请求消息</param>
    /// <param name="cancellationToken">用于取消操作的取消令牌，默认为default</param>
    /// <returns>返回类型为TResult的异步任务，可能为null</returns>
    Task<TResult?> SendFeishuRequestAsync<TResult>(HttpRequestMessage request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送飞书请求并返回指定类型的结果
    /// </summary>
    /// <typeparam name="TResult">期望的返回结果类型，必须是类类型并具有无参构造函数</typeparam>
    /// <param name="request">要发送的HTTP请求消息</param>
    /// <returns>返回类型为TResult的异步任务，可能为null</returns>
    Task<TResult?> SendFeishuRequestAsync<TResult>(HttpRequestMessage request);

    /// <summary>
    /// 下载文件内容并以字节数组形式返回
    /// </summary>
    /// <param name="request">要发送的HTTP请求消息</param>
    /// <param name="cancellationToken">用于取消操作的取消令牌，默认为default</param>
    /// <returns>返回字节数组的异步任务，可能为null</returns>
    Task<byte[]?> DownloadFileRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 下载文件内容并以字节数组形式返回（不带取消令牌的重载方法）
    /// </summary>
    /// <param name="request">要发送的HTTP请求消息</param>
    /// <returns>返回字节数组的异步任务，可能为null</returns>
    Task<byte[]?> DownloadFileRequestAsync(HttpRequestMessage request);

    /// <summary>
    /// 异步下载大文件并保存到指定路径
    /// </summary>
    /// <param name="request">要发送的HTTP请求消息</param>
    /// <param name="largeFile">用于保存大文件的本地路径</param>
    /// <param name="cancellationToken">用于取消操作的取消令牌，默认为default</param>
    /// <returns>表示异步操作的任务</returns>
    Task DownloadLargeFileRequestAsync(HttpRequestMessage request, string largeFile, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步下载大文件并保存到指定路径（不带取消令牌的重载方法）
    /// </summary>
    /// <param name="request">要发送的HTTP请求消息</param>
    /// <param name="largeFile">用于保存大文件的本地路径</param>
    /// <returns>表示异步操作的任务</returns>
    Task DownloadLargeFileRequestAsync(HttpRequestMessage request, string largeFile);
}

internal class FeishuHttpClient : IFeishuHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly FeishuOptions _options;

    public FeishuHttpClient(HttpClient httpClient, ILogger<FeishuHttpClient> logger, FeishuOptions options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options;
    }

    public async Task<TResult?> SendFeishuRequestAsync<TResult>(HttpRequestMessage request) => await SendFeishuRequestAsync<TResult>(request, default);

    public async Task<TResult?> SendFeishuRequestAsync<TResult>(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        if (_options.EnableLogging)
        {
            _logger.LogDebug("开始发送飞书请求: {Url}", request.RequestUri);
        }

        var result = await _httpClient.SendRequest<TResult>(
            request,
            logger: _options.EnableLogging ? _logger : null,
            cancellationToken: cancellationToken);

        if (_options.EnableLogging)
        {
            _logger.LogDebug("飞书请求完成: {Url}", request.RequestUri);
        }
        return result;
    }

    public async Task<byte[]?> DownloadFileRequestAsync(HttpRequestMessage request) => await DownloadFileRequestAsync(request, default);

    public async Task<byte[]?> DownloadFileRequestAsync(
     HttpRequestMessage request,
     CancellationToken cancellationToken = default)
    {
        if (_options.EnableLogging)
        {
            _logger.LogDebug("开始发送飞书请求: {Url}", request.RequestUri);
        }

        var result = await _httpClient.DownloadFile(
                    request,
                    logger: _options.EnableLogging ? _logger : null,
                    cancellationToken: cancellationToken);

        if (_options.EnableLogging)
        {
            _logger.LogDebug("飞书请求完成: {Url}", request.RequestUri);
        }
        return result;
    }

    public async Task DownloadLargeFileRequestAsync(HttpRequestMessage request, string largeFile) => await DownloadLargeFileRequestAsync(request, largeFile, default);


    public async Task DownloadLargeFileRequestAsync(HttpRequestMessage request, string largeFile, CancellationToken cancellationToken = default)
    {
        if (_options.EnableLogging)
        {
            _logger.LogDebug("开始发送飞书请求: {Url}", request.RequestUri);
        }

        await _httpClient.DownloadLargeFile(
                   request,
                   largeFile,
                   logger: _options.EnableLogging ? _logger : null,
                   cancellationToken: cancellationToken);

        if (_options.EnableLogging)
        {
            _logger.LogDebug("飞书请求完成: {Url}", request.RequestUri);
        }
    }
}
