// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Serilog;

namespace Mud.Feishu.Agent.Demo;

/// <summary>
/// 运行模式的入口：<b>进入哪个演示模块由用户在启动菜单里选择决定</b>
/// （模式目录见 <see cref="DemoModeCatalog"/>，菜单与读取见 <see cref="DemoModeSelector"/>）。
/// </summary>
/// <remarks>
/// <para>
/// 配置文件仍然只负责**参数**（模型三项与飞书凭证在统一节 <c>FeishuDemo</c>，模式专属参数在各自的模式节）；
/// 模式节里的 <c>Enabled</c> 键保留一个作用：决定菜单的默认项（回车采用的项，见 <see cref="DemoModeCatalog.Default"/>）。
/// </para>
/// <para>
/// 具体实现按功能归档：<c>Modes/</c>（裸模型、工具冒烟、IM 接入、领域事件、Skills 产物）与
/// <c>DocAgent/</c>（文档业务智能体）。
/// </para>
/// <para>
/// 非交互输入（stdin 被重定向 / 管道 / CI）跳过菜单、直接采用配置默认项，避免进程挂起。
/// 运行时日志由 <see cref="DemoLogging"/> 提供（Serilog：控制台 Warning / 文件全量 Information），
/// 进程退出前统一 <see cref="Log.CloseAndFlush"/>。
/// </para>
/// </remarks>
public static class Program
{
    public static async Task Main()
    {
        // 配置根取输出目录（AppContext.BaseDirectory），appsettings.json → appsettings.local.json。
        var sources = DemoConfiguration.Build(AppContext.BaseDirectory);

        // 运行时日志先行：模式分发前的 fail-fast 错误也能落日志。
        DemoLogging.ConfigureRootLogger(sources.Configuration);

        try
        {
            // 用户选择模式（非交互输入时按配置默认项）；q 取消则直接退出。
            var mode = DemoModeSelector.Select(sources, new ConsoleRenderer());
            if (mode is null)
            {
                return;
            }

            // 分发到模式入口：新增模式只改 DemoModeCatalog，本方法不需要改动。
            await mode.Run(sources);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
