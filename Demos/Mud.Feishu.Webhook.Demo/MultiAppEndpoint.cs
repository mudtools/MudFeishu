// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook;

namespace Mud.Feishu.Webhook.Demo;

/// <summary>
/// 多应用信息端点扩展
/// </summary>
public static class MultiAppEndpointExtensions
{
    /// <summary>
    /// 映射多应用信息端点
    /// </summary>
    public static IEndpointRouteBuilder MapMultiAppInfo(this IEndpointRouteBuilder app)
    {
        app.MapGet("/multi-app/info", () =>
        {
            return Results.Ok(new MultiAppInfoResponse
            {
                Message = "多应用飞书Webhook服务",
                Mode = "MultiApp",
                Description = "支持多个飞书应用同时接入，每个应用有独立的事件处理器",
                Features = new[]
                {
                    "独立的事件处理器配置",
                    "共享的全局拦截器",
                    "应用特定的拦截器",
                    "独立的配置和路由",
                    "完整的事件隔离"
                },
                Examples = new MultiAppInfoExamples
                {
                    WebhookEndpoints = new WebhookEndpointsInfo
                    {
                        App1 = "/feishu/app1",
                        App2 = "/feishu/app2"
                    },
                    ConfigurationSection = "FeishuWebhook:Apps:app1/app2",
                    Documentation = "参见 appsettings.MultiApp.example.json"
                }
            });
        })
        .WithName("GetMultiAppInfo")
        .WithTags("Multi-App");

        app.MapGet("/multi-app/routes", () =>
        {
            return Results.Ok(new MultiAppRoutesResponse
            {
                Message = "多应用路由映射",
                Routes = new[]
                {
                    new AppRouteInfo
                    {
                        AppKey = "app1",
                        RoutePrefix = "/feishu/app1",
                        Description = "组织架构相关事件",
                        EventTypes = new[] { "department.user.created_v4", "department.deleted_v3", "department.updated_v3" }
                    },
                    new AppRouteInfo
                    {
                        AppKey = "app2",
                        RoutePrefix = "/feishu/app2",
                        Description = "用户和审批相关事件",
                        EventTypes = new[] { "user.user.created_v1", "user.user.deleted_v3", "approval.passed", "approval.rejected" }
                    }
                }
            });
        })
        .WithName("GetMultiAppRoutes")
        .WithTags("Multi-App");

        return app;
    }
}

/// <summary>
/// 多应用信息响应
/// </summary>
internal class MultiAppInfoResponse
{
    public string Message { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[] Features { get; set; } = Array.Empty<string>();
    public MultiAppInfoExamples Examples { get; set; } = new();
}

/// <summary>
/// 多应用信息示例
/// </summary>
internal class MultiAppInfoExamples
{
    public WebhookEndpointsInfo WebhookEndpoints { get; set; } = new();
    public string ConfigurationSection { get; set; } = string.Empty;
    public string Documentation { get; set; } = string.Empty;
}

/// <summary>
/// Webhook 端点信息
/// </summary>
internal class WebhookEndpointsInfo
{
    public string App1 { get; set; } = string.Empty;
    public string App2 { get; set; } = string.Empty;
}

/// <summary>
/// 多应用路由映射响应
/// </summary>
internal class MultiAppRoutesResponse
{
    public string Message { get; set; } = string.Empty;
    public AppRouteInfo[] Routes { get; set; } = Array.Empty<AppRouteInfo>();
}

/// <summary>
/// 应用路由信息
/// </summary>
internal class AppRouteInfo
{
    public string AppKey { get; set; } = string.Empty;
    public string RoutePrefix { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[] EventTypes { get; set; } = Array.Empty<string>();
}
