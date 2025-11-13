
using Mud.Feishu;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 飞书相关服务注册扩展方法
/// </summary>
public static class FeishuServiceCollectionExtensions
{
    /// <summary>
    /// 注册飞书 API 服务
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddFeishuApiService(this IServiceCollection services)
    {
        return services.AddWebApiHttpClient();
    }
}
