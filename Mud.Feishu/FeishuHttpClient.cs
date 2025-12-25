// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mud.Feishu.Extensions;

namespace Mud.Feishu;


internal class FeishuHttpClient : IEnhancedHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FeishuHttpClient> _logger;
    private readonly FeishuOptions _feishuOptions;

    public FeishuHttpClient(
        HttpClient httpClient,
        ILogger<FeishuHttpClient> logger,
        IOptions<FeishuOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _feishuOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));

        // 配置HttpClient的基础地址（如果有）
        if (!string.IsNullOrEmpty(_feishuOptions.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_feishuOptions.BaseUrl);
        }
    }

    public async Task<TResult?> SendAsync<TResult>(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var uri = request.RequestUri?.ToString() ?? "[No URI]";

        try
        {
            LogRequestStart("发送请求", uri);

            var result = await _httpClient.SendRequestAsync<TResult>(
                request,
                logger: _feishuOptions.EnableLogging ? _logger : null,
                cancellationToken: cancellationToken);

            LogRequestComplete("请求完成", uri);
            return result;
        }
        catch (Exception ex) when (LogRequestError("发送请求失败", uri, ex))
        {
            throw;
        }
    }

    public async Task<byte[]?> DownloadAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var uri = request.RequestUri?.ToString() ?? "[No URI]";

        try
        {
            LogRequestStart("下载文件", uri);

            var result = await _httpClient.DownloadFileAsync(
                request,
                logger: _feishuOptions.EnableLogging ? _logger : null,
                cancellationToken: cancellationToken);

            LogRequestComplete("文件下载完成", uri);
            return result;
        }
        catch (Exception ex) when (LogRequestError("文件下载失败", uri, ex))
        {
            throw;
        }
    }

    public async Task<FileInfo> DownloadLargeAsync(
        HttpRequestMessage request,
        string filePath,
        bool overwrite = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("文件路径不能为空", nameof(filePath));

        var uri = request.RequestUri?.ToString() ?? "[No URI]";

        try
        {
            LogRequestStart($"下载大文件到: {filePath}", uri);

            var fileInfo = await _httpClient.DownloadLargeFileAsync(
                request,
                filePath,
                overwrite: overwrite,
                logger: _feishuOptions.EnableLogging ? _logger : null,
                cancellationToken: cancellationToken);

            LogRequestComplete($"大文件下载完成: {filePath}", uri);
            return fileInfo;
        }
        catch (Exception ex) when (LogRequestError($"大文件下载失败: {filePath}", uri, ex))
        {
            throw;
        }
    }

    #region 私有辅助方法

    private void LogRequestStart(string operation, string uri)
    {
        if (_feishuOptions.EnableLogging && _logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("[Feishu] {Operation}: {Uri}", operation, uri);
        }
    }

    private void LogRequestComplete(string operation, string uri)
    {
        if (_feishuOptions.EnableLogging && _logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("[Feishu] {Operation}: {Uri}", operation, uri);
        }
    }

    private bool LogRequestError(string errorMessage, string uri, Exception ex)
    {
        if (_feishuOptions.EnableLogging && _logger.IsEnabled(LogLevel.Error))
        {
            _logger.LogError(ex, "[Feishu] {ErrorMessage}: {Uri}", errorMessage, uri);
        }
        return false; // 始终返回false，异常会被重新抛出
    }

    #endregion
}