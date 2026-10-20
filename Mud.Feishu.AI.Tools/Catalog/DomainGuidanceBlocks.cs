// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Mud.Feishu.AI.Tools.Catalog;

/// <summary>
/// 已启用域 guidance 块装配——装配链（<c>FeishuToolAIFunction.GetGuidance</c>）与自检面的<b>唯一实现</b>。
/// </summary>
/// <remarks>
/// <para>
/// 排序即<b>丢弃优先级</b>：按「该域已启用工具数」降序、同数按域名字母序。形态由
/// <c>FeishuAgent:GuidanceMode</c> 决定（<c>routing</c> 每域一行摘要 / <c>full</c> 每域 L1 全文），
/// 两种形态共用同一排序，故「预算被调小丢的是长尾域」这一语义不随形态切换而变。
/// 本方法<b>不裁剪</b>：总长上限与整域丢弃由调用方交给中性装配器执行。
/// </para>
/// <para>
/// <b>为什么两条链共用</b>：自检面若另写一份排序与形态判据，它报出的预算余量与被丢弃的域
/// 就不是运行期真实下发的那一份——诊断结论与执行事实分离即失去诊断价值。
/// </para>
/// </remarks>
internal static class DomainGuidanceBlocks
{
    /// <summary>取已启用工具所属域的 guidance 块（注册表缺席时返回空集，不抛）。</summary>
    /// <param name="serviceProvider">服务提供者。</param>
    /// <returns>按丢弃优先级排序的 guidance 块。</returns>
    public static IReadOnlyList<GuidanceBlock> Create(IServiceProvider serviceProvider)
        => Create(serviceProvider, serviceProvider.GetService<FeishuToolRegistry>());

    /// <summary>同上，注册表由调用方提供。</summary>
    /// <remarks>
    /// 自检面必须在<b>注册表构建失败时</b>仍给出 guidance 读数：若它自行解析，失败容器会把
    /// 同一条装配异常抛第二次，诊断结论从「注册表构建失败」退化成一个无信息量的重复报错。
    /// </remarks>
    /// <param name="serviceProvider">服务提供者（仅用于读取下发形态 <c>GuidanceMode</c>）。</param>
    /// <param name="registry">已解析的注册表（<see langword="null"/> = 缺席）。</param>
    /// <returns>按丢弃优先级排序的 guidance 块。</returns>
    public static IReadOnlyList<GuidanceBlock> Create(IServiceProvider serviceProvider, FeishuToolRegistry? registry)
    {
        if (registry is null)
        {
            return [];
        }

        // 域 = 工具名首个"." 之前的部分；按域聚合已启用工具数 → 丢弃优先级（工具数降序，同数按域名字母序）。
        var domainToolCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var tool in registry.EnabledTools)
        {
            var domain = DomainOf(tool.Name);
            if (domain.Length == 0)
            {
                continue;
            }

            domainToolCounts[domain] = domainToolCounts.TryGetValue(domain, out var count)
                ? count + 1
                : 1;
        }

        // OrderByDescending(工具数) → OrderBy(域名)：前者定重要性，后者保证同重要性的顺序确定（可 golden 比对）。
        var domains = domainToolCounts
            .OrderByDescending(static pair => pair.Value)
            .ThenBy(static pair => pair.Key, StringComparer.Ordinal);

        // 下发形态（routing = 每域一行路由摘要，全文按需读；full = 既有全文拼接）。
        // 未解析到 Options 时按**默认值** routing 处理（与 FeishuAgentOptions 的默认一致，
        // 不因"装配缺失"悄悄退回全文拼接——那正是域扩容后整域丢弃的失败形态）。
        var mode = serviceProvider.GetService<IOptions<FeishuAgentOptions>>()?.Value?.GuidanceMode;
        var routing = !string.Equals(mode, GuidanceModes.Full, StringComparison.Ordinal);

        var blocks = new List<GuidanceBlock>();
        foreach (var domain in domains)
        {
            if (FeishuToolGuidance.ByDomain.TryGetValue(domain.Key, out var content))
            {
                blocks.Add(new GuidanceBlock(
                    domain.Key,
                    routing ? GuidanceRoutingSummary.Create(domain.Key, content) : content));
            }
        }

        return blocks;
    }

    /// <summary>取工具名所属域（首个 <c>.</c> 之前的部分）。</summary>
    private static string DomainOf(string toolName)
    {
        // 注：netstandard2.0 没有 IndexOf(char, StringComparison)，用字符串重载。
        var index = toolName.IndexOf(".", StringComparison.Ordinal);
        return index > 0 ? toolName.Substring(0, index) : toolName;
    }
}
