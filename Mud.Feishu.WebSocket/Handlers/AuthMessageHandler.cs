// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.WebSocket.DataModels;

namespace Mud.Feishu.WebSocket.Handlers;

/// <summary>
/// 认证消息处理器
/// </summary>
public class AuthMessageHandler : JsonMessageHandler
{
    private readonly Action<bool> _onAuthResult;

    /// <inheritdoc/>
    public AuthMessageHandler(
        ILogger<AuthMessageHandler> logger,
        Action<bool> onAuthResult)
        : base(logger)
    {
        _onAuthResult = onAuthResult ?? throw new ArgumentNullException(nameof(onAuthResult));
    }
    /// <inheritdoc/>
    public override bool CanHandle(string messageType)
    {
        return messageType.ToLowerInvariant() == "auth";
    }
    /// <inheritdoc/>
    public override Task HandleAsync(string message, CancellationToken cancellationToken = default)
    {
        var authResponse = SafeDeserialize<AuthResponseMessage>(message);

        if (authResponse?.Code == 0)
        {
            _onAuthResult(true);
        }
        else
        {
            _logger.LogError("WebSocket认证失败: {Code} - {Message}", authResponse?.Code, authResponse?.Message);
            _onAuthResult(false);
        }

        return Task.CompletedTask;
    }
}
