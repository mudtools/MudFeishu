// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mud.AI.Agent.Guidance;
using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Tools;
using Mud.Feishu.AI.Tools.Catalog;
namespace Mud.Feishu.Agent.Demo;

/// <summary>
/// R-3 / F-8：<b>Skills 产物消费演示</b>——把随包分发的 <c>skills/**</c> 读出来并注入域 guidance。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么是这个模式（F-8 的"零消费方"判定）</b>：产物树此前只有"导出"没有"读取"，
/// "包内产物 == 读取内容"没有任何路径验证过。本模式是它的<b>首个真实消费方</b>，
/// 用最小代价闭环验证：① 产物能被发现（枚举）；② 能被按域读取；③ 能进入模型的指令面（guidance 注入），
/// 且预算事实（截断 / 丢弃域）在输出里可见。
/// </para>
/// <para>
/// <b>不连飞书、不需要模型</b>：本模式只装配工具面基础设施（<c>AddFeishuTools</c> 的 DI 部分），
/// 因此它可以在任何环境下（含 CI 沙箱）作为"产物可用性"冒烟。
/// </para>
/// <para>
/// <b>报告与输出分离</b>：<see cref="BuildReport"/> 是纯函数（输入读取接缝，输出行），
/// <see cref="RunAsync"/> 只负责打印——这样"产物可被消费"是可断言的，而不是靠抓控制台输出。
/// </para>
/// </remarks>
internal static class SkillsDemo
{
    /// <summary>运行 Skills 产物消费演示。</summary>
    /// <param name="configuration">配置来源。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>任务。</returns>
    public static Task RunAsync(IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var settings = SkillsDemoSettings.FromConfiguration(configuration);
        settings.Validate();

        // 只装配工具面基础设施：本模式不连飞书（无 AppId/AppSecret、无模型），
        // 演示的是"读产物"这件事本身——这也是它能在 CI 里跑的前提。
        using var provider = new ServiceCollection()
            .AddSingleton(Options.Create(new FeishuAgentOptions { Instructions = "skills-demo" }))
            .AddFeishuTools()
            .BuildServiceProvider();

        foreach (var line in BuildReport(
                     provider.GetRequiredService<ISkillDocumentReader>(),
                     settings.Domain,
                     cancellationToken))
        {
            Console.WriteLine(line);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 构建演示报告（纯函数：便于用例断言"产物可被消费且能进入指令面"）。
    /// </summary>
    /// <param name="reader">Skills 产物读取接缝。</param>
    /// <param name="domain">要展开的域。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>报告行（按打印顺序）。</returns>
    internal static IReadOnlyList<string> BuildReport(
        ISkillDocumentReader reader,
        string domain,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var lines = new List<string>
        {
            $"Skills 产物包版本（package_version）：{reader.PackageVersion}",
            $"产物条目：{reader.Paths.Count} 个",
        };

        lines.AddRange(reader.Paths.Select(static path => $"  - {path}"));

        var domains = reader.Paths
            .Where(static p => p.EndsWith("/SKILL.md", StringComparison.Ordinal))
            .Select(static p => p.Split('/')[1].Substring("feishu-".Length))
            .OrderBy(static d => d, StringComparer.Ordinal)
            .ToArray();

        cancellationToken.ThrowIfCancellationRequested();

        var skill = reader.ReadSkill(domain);
        if (skill is null)
        {
            lines.Add(string.Empty);
            lines.Add($"[warn] 域 '{domain}' 没有 SKILL.md——可用域：{string.Join("、", domains)}");
            return lines;
        }

        lines.Add(string.Empty);
        lines.Add($"=== 域 {domain} 的 SKILL.md（{skill.Length} 字符）===");
        lines.Add(skill);

        // 闭环：产物 → guidance 注入。**只注入所选域**——SKILL.md 正文含 frontmatter 与命令清单，
        // 规模大于域级 guidance 本体，把全部 25 个域一次性塞进指令面本就该触达预算；
        // 演示要回答的是"这个产物能不能作为 guidance 被消费"，而不是"能不能全塞进一次 prompt"。
        var composed = GuidanceComposer.Compose(
            "（宿主指令占位）",
            [new GuidanceBlock(domain, skill)],
            FeishuGuidanceBudget.MaxGuidanceLength);

        lines.Add(string.Empty);
        lines.Add(
            $"guidance 注入：域 {domain} → {composed.Instructions.Length} 字符"
            + $"（预算 {FeishuGuidanceBudget.MaxGuidanceLength}；截断={composed.Truncated}）");

        // 全量产物规模只作**信息**输出（不作注入、不告警）：SKILL.md 是给外部 Agent 读的，
        // 它本来就不该被整批塞进指令面——把它当"预算超限"会让演示输出给出错误的行动建议。
        var totalProducts = reader.Paths.Sum(p => reader.Read(p)?.Length ?? 0);
        lines.Add(
            $"全量产物规模：{reader.Paths.Count} 个文件 / {totalProducts} 字符"
            + "（供外部 Agent 按需读取；不参与域级 guidance 预算）");

        if (composed.Truncated)
        {
            lines.Add(
                "[warn] 单域产物已触达 guidance 预算——按域裁剪内容，或与 FeishuGuidanceBudget.MaxGuidanceLength 同批评审调整。");
        }

        return lines;
    }
}
