// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.Extensions;

namespace Mud.Feishu;

public interface IFeishuHttpClient
{
    Task<TResult?> SendFeishuRequest<TResult>(
       HttpRequestMessage request,
       CancellationToken cancellationToken = default)
       where TResult : class, new();
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

    public async Task<TResult?> SendFeishuRequest<TResult>(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
        where TResult : class, new()
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
}
