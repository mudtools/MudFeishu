// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Tools;
using Mud.Feishu.AI.Tools.Hosting;

namespace Mud.Feishu.Agent.Demo;

/// <summary>
/// 装配自检演示（一条命令读「这个容器到底接对了没有」）——<see cref="IFeishuDiagnosticsProbe"/> 的 Demo 侧消费方。
/// </summary>
/// <remarks>
/// <para>
/// <b>零模型、零网络</b>：本模式不注册模型客户端，自检面也不发起任何调用（不换令牌、不实例化应用），
/// 因此凭证缺失照样跑完整条链；它自检的是 DI 接线与策略轴一致性，而不是平台连通性。
/// </para>
/// <para>
/// <b>容器恒为完整形态</b>：域客户端注册依赖应用管理器（<c>AddFeishuServices</c> 缺席
/// <c>IFeishuAppManager</c> 即 fail-fast），故未配置凭证时用占位凭证装配并报明——否则报告只剩
/// 「白名单含未注册工具」一条噪声，读不到真正的接线事实。
/// </para>
/// <para>
/// <b>报告与输出分离</b>（与 <see cref="SkillsDemo"/> 同构）：<see cref="BuildReport"/> 是纯函数，
/// 用例直接对 <see cref="CreateProvider"/> 造出的容器断言结论码，不靠抓控制台输出。
/// </para>
/// </remarks>
internal static class DoctorDemo
{
    private const string PlaceholderAppId = "cli_doctor_placeholder";

    private const string PlaceholderAppSecret = "doctor-placeholder-secret";

    /// <summary>运行装配自检演示。</summary>
    /// <param name="configuration">配置来源。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>任务。</returns>
    public static Task RunAsync(IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var settings = DoctorDemoSettings.FromConfiguration(configuration);
        settings.Validate();

        using var provider = CreateProvider(settings);
        var report = provider.GetRequiredService<IFeishuDiagnosticsProbe>().Probe();

        foreach (var line in BuildReport(report))
        {
            Console.WriteLine(line);
        }

        if (!settings.HasCredentials)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"提示：本次以「占位凭证」装配——未配置 {FeishuDemoSettings.SectionName}:{FeishuDemoSettings.KeyAppId}"
                + $" / {FeishuDemoSettings.KeyAppSecret}。本模式零网络调用，占位凭证不会被使用；"
                + "但令牌管理器可达性一类读数只反映占位应用，补齐真实凭证后重跑即得完整自检。");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 造出自检用的服务容器（真实宿主装配的最小子集：工具面 + 策略轴 + 可选的应用与域客户端）。
    /// </summary>
    /// <param name="settings">本模式配置。</param>
    /// <returns>已构建的容器（调用方负责释放）。</returns>
    internal static ServiceProvider CreateProvider(DoctorDemoSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        // 占位凭证只为把容器装配完整（自检零网络，不会用它换令牌）；真实凭证走统一节 FeishuDemo。
        var appId = settings.AppId ?? PlaceholderAppId;
        var appSecret = settings.AppSecret ?? PlaceholderAppSecret;

        var services = new ServiceCollection()
            .AddFeishuApp(
                DemoAppConfig.CreateSingleApp(DemoAgentDefaults.DefaultAppKey, appId, appSecret),
                "FeishuApps")
            // 白名单是全域只读，域注册面必须同宽——少注册一个域就有一枚工具不入表，装配期直接失败。
            .AddFeishuServices(static builder => builder.AddAllApis());

        // 受控下载出口（drive.download_file / drive.export_document / minutes.get_transcript）与宿主
        // 落盘器同生共死：落盘器缺席 ⇒ 整域软缺席 ⇒ 白名单含未注册工具（装配期 fail-fast）。
        services.AddHttpClient<DemoAttachmentStager>();
        services.AddSingleton<IFeishuAttachmentStager>(static sp => sp.GetRequiredService<DemoAttachmentStager>());

        services.AddSingleton(Options.Create(new FeishuAgentOptions
        {
            Instructions = "doctor",
            Tools = [.. DemoAgentDefaults.ReadonlyToolWhitelist],
            AllowedIdentities = ["tenant", "user"],
        }));

        return services.AddFeishuTools().BuildServiceProvider();
    }

    /// <summary>
    /// 构建报告正文（纯函数：输入报告，输出按打印顺序的行）。
    /// </summary>
    /// <param name="report">自检报告。</param>
    /// <returns>报告行。</returns>
    internal static IReadOnlyList<string> BuildReport(FeishuDiagnosticsReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var error = Count(report, FeishuDiagnosticsSeverities.Error);
        var warning = Count(report, FeishuDiagnosticsSeverities.Warning);
        var info = Count(report, FeishuDiagnosticsSeverities.Info);

        var lines = new List<string>
        {
            "=== 装配自检（doctor）==="
            + $" 结论 {report.FindingCount} 条（error {error} / warning {warning} / info {info}）"
            + $"，事实读数 {report.Facts.Count} 条",
        };

        lines.AddRange(report.ToText().Split(Environment.NewLine));

        lines.Add(string.Empty);
        lines.Add(report.HasErrors
            ? $"判定：存在 {error} 条 error 级结论——按当前配置，运行期必然失败或必然静默丢弃，须处置后再上线。"
            : warning > 0
                ? $"判定：无 error 级结论；{warning} 条 warning 是运行期逐请求暴露的隐患，建议按码处置。"
                : "判定：无 error / warning 级结论，当前装配自洽。");

        return lines;
    }

    private static int Count(FeishuDiagnosticsReport report, string severity)
        => report.Findings.Count(finding => string.Equals(finding.Severity, severity, StringComparison.Ordinal));
}
