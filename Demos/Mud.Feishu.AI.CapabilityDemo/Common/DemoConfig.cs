// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.CapabilityDemo.Common;

/// <summary>已装配的配置来源：配置根 + 实际存在的配置文件清单。</summary>
internal sealed record DemoConfigurationSources(IConfigurationRoot Configuration, IReadOnlyList<string> Files);

/// <summary>
/// 配置装配：<c>appsettings.json</c> → <c>appsettings.local.json</c>（后者覆盖前者）；
/// 单应用幕由 <see cref="DemoSettings"/> 凭证合成 <c>FeishuApps</c>（不落盘）。
/// </summary>
internal static class DemoConfig
{
    public static DemoConfigurationSources Build(string baseDirectory)
    {
        var files = new List<string>();
        var builder = new ConfigurationBuilder();
        AddJsonFile(DemoSettings.AppSettingsFile, optional: false);
        AddJsonFile(DemoSettings.LocalAppSettingsFile, optional: true);
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

    /// <summary>
    /// 保证 <c>FeishuApps</c> 节存在：配置显式提供则原样使用，否则用单应用凭证合成。
    /// </summary>
    public static IConfigurationRoot EnsureAppSection(IConfiguration configuration, DemoSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(configuration[$"{DemoSettings.AppSectionName}:0:AppId"]))
        {
            return configuration as IConfigurationRoot
                   ?? new ConfigurationBuilder().AddConfiguration(configuration).Build();
        }

        if (string.IsNullOrWhiteSpace(settings.AppId) || string.IsNullOrWhiteSpace(settings.AppSecret))
        {
            throw new InvalidOperationException(
                $"缺少飞书应用凭证：请在 appsettings.local.json 填 {DemoSettings.SectionName}:AppId / AppSecret，"
                + $"或提供 {DemoSettings.AppSectionName} 节（多应用幕 A8 必须用后者）。");
        }

        return new ConfigurationBuilder()
            .AddConfiguration(configuration)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{DemoSettings.AppSectionName}:0:AppKey"] = settings.AppKey,
                [$"{DemoSettings.AppSectionName}:0:AppId"] = settings.AppId,
                [$"{DemoSettings.AppSectionName}:0:AppSecret"] = settings.AppSecret,
                [$"{DemoSettings.AppSectionName}:0:IsDefault"] = "true",
            })
            .Build();
    }

    /// <summary>
    /// 幕用：有真实凭证就用真实凭证，否则退回占位应用（A4 的四个用例都在下游调用前被拒，
    /// 不会发 HTTP）。
    /// </summary>
    public static IConfigurationRoot EnsureAppSectionOrVerify(IConfiguration configuration, DemoSettings settings)
        => string.IsNullOrWhiteSpace(settings.AppId) || string.IsNullOrWhiteSpace(settings.AppSecret)
            ? CreateVerifyConfiguration()
            : EnsureAppSection(configuration, settings);

    /// <summary>
    /// <c>--verify</c> 专用：内存合成的占位应用（格式合法即可，<b>不发任何 HTTP</b>）。
    /// </summary>
    public static IConfigurationRoot CreateVerifyConfiguration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{DemoSettings.AppSectionName}:0:AppKey"] = "cap-verify",
                [$"{DemoSettings.AppSectionName}:0:AppId"] = "cli_a1b2c3d4e5f6a7b8",
                [$"{DemoSettings.AppSectionName}:0:AppSecret"] = "verify-only-placeholder-secret-0001",
                [$"{DemoSettings.AppSectionName}:0:IsDefault"] = "true",
                [$"{DemoSettings.SectionName}:{DemoSettings.KeyAppKey}"] = "cap-verify",
                [$"{DemoSettings.SectionName}:{DemoSettings.KeyUserId}"] = "ou_cap_demo_user",
                [$"{DemoSettings.SectionName}:{DemoSettings.KeySkillDomain}"] = "bitable",
            })
            .Build();

    /// <summary>取配置里的应用键列表（A8 用；按出现顺序）。</summary>
    public static IReadOnlyList<string> ReadAppKeys(IConfiguration configuration)
        => configuration.GetSection(DemoSettings.AppSectionName)
            .GetChildren()
            .Select(static c => c["AppKey"])
            .Where(static key => !string.IsNullOrWhiteSpace(key))
            .Select(static key => key!)
            .ToArray();
}
