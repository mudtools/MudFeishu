// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

#if NET8_0_OR_GREATER
using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.EventCallback;  // EventCallbackJsonContext 所在命名空间

namespace Mud.Feishu.EventCallback.Extensions;

/// <summary>
/// EventCallback JSON解析器扩展。
/// 模块自治方案：复用Webhook项目的模式，直接调用ConfigureUserResolver。
/// </summary>
public static class FeishuEventCallbackJsonResolverExtensions
{
    /// <summary>
    /// 配置EventCallback解析器。
    /// 将EventCallbackJsonContext注入到FeishuJsonDefaults累加resolver链。
    /// 必须在应用程序启动时调用。
    /// </summary>
    public static void ConfigureEventCallbackResolver()
    {
        // 复用 Webhook 项目的模块自治模式
        FeishuJsonDefaults.ConfigureUserResolver(EventCallbackJsonContext.Default);
    }
}
#endif