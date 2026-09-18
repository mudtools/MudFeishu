// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.EventHandlers;

namespace Mud.Feishu.Webhook.Demo;

/// <summary>
/// 诊断端点，用于检查事件处理器注册情况
/// </summary>
public static class DiagnosticsEndpoint
{
    /// <summary>
    /// 注册诊断端点
    /// </summary>
    public static void MapDiagnostics(this WebApplication app)
    {
        app.MapGet("/diagnostics/handlers", (
            [FromServices] IFeishuEventHandlerFactory factory,
            [FromServices] IEnumerable<IFeishuEventHandler> handlers) =>
        {
            var registeredTypes = factory.GetRegisteredEventTypes();

            // 手动构建处理器信息
            var handlerInfo = new Dictionary<string, List<string>>();
            var concreteFactory = factory as DefaultFeishuEventHandlerFactory;
            if (concreteFactory != null)
            {
                handlerInfo = concreteFactory.GetHandlerInfo();
            }

            var allHandlers = handlers.Select(h => new HandlerInfo
            {
                Type = h.GetType().Name,
                FullType = h.GetType().FullName ?? string.Empty,
                SupportedEventType = h.SupportedEventType
            }).ToList();

            return Results.Ok(new HandlersDiagnosticsResponse
            {
                RegisteredEventTypes = registeredTypes,
                HandlerInfo = handlerInfo,
                AllHandlers = allHandlers,
                Summary = new HandlersSummary
                {
                    TotalEventTypes = registeredTypes.Count,
                    TotalHandlers = allHandlers.Count,
                    Message = registeredTypes.Count == 0
                        ? "❌ 没有注册任何事件处理器！"
                        : $"✅ 成功注册 {registeredTypes.Count} 种事件类型"
                }
            });
        }).WithName("GetHandlerDiagnostics").WithTags("Diagnostics");
    }
}

/// <summary>
/// 处理器诊断响应
/// </summary>
internal class HandlersDiagnosticsResponse
{
    public IReadOnlyList<string> RegisteredEventTypes { get; set; } = Array.Empty<string>();
    public Dictionary<string, List<string>> HandlerInfo { get; set; } = new();
    public List<HandlerInfo> AllHandlers { get; set; } = new();
    public HandlersSummary Summary { get; set; } = new();
}

/// <summary>
/// 处理器信息
/// </summary>
internal class HandlerInfo
{
    public string Type { get; set; } = string.Empty;
    public string FullType { get; set; } = string.Empty;
    public string SupportedEventType { get; set; } = string.Empty;
}

/// <summary>
/// 处理器摘要
/// </summary>
internal class HandlersSummary
{
    public int TotalEventTypes { get; set; }
    public int TotalHandlers { get; set; }
    public string Message { get; set; } = string.Empty;
}
