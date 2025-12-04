// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 消息处理器接口
/// </summary>
public interface IMessageHandler
{
    /// <summary>
    /// 是否可以处理指定类型的消息
    /// </summary>
    /// <param name="messageType">消息类型</param>
    /// <returns>是否可以处理</returns>
    bool CanHandle(string messageType);

    /// <summary>
    /// 处理消息
    /// </summary>
    /// <param name="message">消息内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理任务</returns>
    Task HandleAsync(string message, CancellationToken cancellationToken = default);
}

/// <summary>
/// JSON消息处理器基类
/// </summary>
public abstract class JsonMessageHandler : IMessageHandler
{
    protected readonly ILogger _logger;
    protected readonly JsonSerializerOptions _jsonOptions;

    protected JsonMessageHandler(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <inheritdoc/>
    public abstract bool CanHandle(string messageType);

    /// <inheritdoc/>
    public abstract Task HandleAsync(string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// 安全解析JSON消息
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="json">JSON字符串</param>
    /// <returns>解析结果</returns>
    protected T? SafeDeserialize<T>(string json) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "解析JSON消息失败: {Json}", json);
            return null;
        }
    }
}