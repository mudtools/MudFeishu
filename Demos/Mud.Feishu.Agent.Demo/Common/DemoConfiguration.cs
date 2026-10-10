// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;

namespace Mud.Feishu.Agent.Demo;

/// <summary>
/// 已装配的配置来源：配置根 + **实际存在**的配置文件清单（全部运行模式共用）。
/// </summary>
/// <param name="Configuration">配置根（文件层；<c>appsettings.json</c> / <c>appsettings.local.json</c>）。</param>
/// <param name="Files">实际加载的配置文件名（用于横幅的"配置来源"事实行；文件不存在时不列出）。</param>
public sealed record DemoConfigurationSources(IConfigurationRoot Configuration, IReadOnlyList<string> Files);

/// <summary>
/// 运行模式的公共配置装配（原 <c>DocAgentDemo.BuildConfiguration</c>，提升为四个模式共用）：
/// <c>appsettings.json</c> → <c>appsettings.local.json</c>（后者覆盖前者）。
/// </summary>
/// <remarks>
/// 环境变量不参与参数解析（R4 配置面约定：参数与模式开关全部来自配置文件）。
/// 仅两层文件：<c>appsettings.json</c>（随仓库提交的模板，必填）＋
/// <c>appsettings.local.json</c>（本地覆盖，已被 <c>.gitignore</c> 忽略，可选）。
/// </remarks>
internal static class DemoConfiguration
{
    /// <summary>
    /// 装配配置来源。
    /// </summary>
    /// <param name="baseDirectory">配置根目录（生产路径为 <see cref="AppContext.BaseDirectory"/>，
    /// 与工作目录无关 —— <c>dotnet run</c> / 直接跑产物 / 任意 cwd 行为一致）。</param>
    /// <returns>配置根与**实际存在**的配置文件清单（用于横幅的"配置来源"事实行）。</returns>
    /// <exception cref="FileNotFoundException"><c>appsettings.json</c> 缺失（必填模板，fail-fast 而非静默空配置）。</exception>
    public static DemoConfigurationSources Build(string baseDirectory)
    {
        var files = new List<string>();

        var builder = new ConfigurationBuilder();
        AddJsonFile(DocAgentSettings.AppSettingsFile, optional: false);
        AddJsonFile(DocAgentSettings.LocalAppSettingsFile, optional: true);

        return new DemoConfigurationSources(builder.Build(), files);

        void AddJsonFile(string fileName, bool optional)
        {
            var path = Path.Combine(baseDirectory, fileName);
            if (File.Exists(path))
            {
                files.Add(fileName);
            }

            builder.AddJsonFile(path, optional: optional, reloadOnChange: false);
        }
    }
}
