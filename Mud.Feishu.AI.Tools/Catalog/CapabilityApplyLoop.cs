// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Generated;

namespace Mud.Feishu.AI.Tools.Catalog;

/// <summary>
/// 未策展能力的「建议策展名 + 申请通道」投影（把"本工具集没有它"转化为"宿主可以把它策展出来"）。
/// </summary>
/// <remarks>
/// <para>
/// <b>建议名是候选，不是契约</b>：策展时人会给工具取**语义名**（<c>GetApplicationsPageListAsync</c>
/// 实际落成 <c>application.list_apps</c> 而非机械投影 <c>application.list_applications</c>）。
/// 实测本规则对既有策展面的**模块前缀一致率 95.6%、全名一致率 23.1%**（342 条策展条目，口径见
/// <c>documents/AIAgent/decisions/ADR-0006-capability-apply-loop.md</c>），故输出必须与
/// "可直接调用"在文案上严格区分；守卫 <c>CapabilityApplyLoopContractGuards</c> 锁的是
/// 模块前缀一致率下限与"建议名不得命中既有工具名"，**不**锁全名。
/// </para>
/// <para>
/// <b>不得变成第二条调用路径</b>：本类只做命名投影，不产出 HTTP/路由/参数形状，
/// 也不提供任何执行入口（R-12：不恢复 <c>feishu.api_call</c>）。宿主采纳建议名的唯一路径是
/// 走与既有工具完全相同的策展流水线（声明 + 执行器 + 权限登记 + golden/skills/affordance）。
/// </para>
/// </remarks>
internal static class CapabilityApplyLoop
{
    /// <summary>单个能力分组一次返回的建议名上限（分组可达 71 个方法，全量会挤爆结果预算）。</summary>
    public const int MaxSuggestionsPerGroup = 10;

    /// <summary>申请通道文案（两个元工具共用同一份，避免同一事实两处表述漂移）。</summary>
    public const string ApplyChannel =
        "申请通道：建议名是宿主策展的候选名（实际命名由策展人按域语义调整），宿主可把它补进工具面——"
        + "策展 = 声明 [AgentTool] 接口 + [ToolHandler] 执行器 + 权限对照表/scope 登记 + golden/skills/affordance 同批刷新。"
        + "模型不得把建议名当作可调用工具，也不得因它存在而臆造调用；未策展期间的正确处置仍是如实告知用户。";

    private static readonly Dictionary<string, string> VerbMap = new(StringComparer.Ordinal)
    {
        ["Get"] = "get", ["Fetch"] = "get", ["Load"] = "get", ["Read"] = "get",
        ["List"] = "list", ["Query"] = "query", ["Search"] = "search",
        ["Create"] = "create", ["Add"] = "create", ["Insert"] = "create",
        ["Update"] = "update", ["Patch"] = "update", ["Edit"] = "update", ["Modify"] = "update", ["Set"] = "update",
        ["Delete"] = "delete", ["Remove"] = "delete", ["Purge"] = "delete", ["Trash"] = "delete",
    };

    /// <summary>分页/列表后缀词（命中即把 get 提升为 list，并从资源段剥除）。</summary>
    private static readonly HashSet<string> PageSuffixes = new(StringComparer.OrdinalIgnoreCase)
    { "Page", "Pages", "List", "Array" };

    /// <summary>按 id 取数的尾缀（<c>GetMenuById</c> 的资源就是 menu，不是 menu_by_id）。</summary>
    private static readonly HashSet<string> ByIdTail = new(StringComparer.OrdinalIgnoreCase)
    { "By", "Id", "Ids" };

    /// <summary>
    /// 投影单个 SDK 方法的候选工具名；<c>Unparsed</c> 分组（非 SDK 接口派生）无候选。
    /// </summary>
    public static string? SuggestToolName(FeishuToolMethodCatalog.Entry entry)
    {
        if (entry.Module.Length == 0 || string.Equals(entry.Module, "Unparsed", StringComparison.Ordinal))
        {
            return null;
        }

        var module = CapabilityModuleAliases
            .ResolveModules(LeadingPascalSegment(entry.Module))[0]
            .ToLower(CultureInfo.InvariantCulture);

        var words = SplitPascalWords(entry.Method);
        if (words.Count == 0)
        {
            return null;
        }

        var verb = VerbMap.TryGetValue(words[0], out var mapped) ? mapped : words[0].ToLower(CultureInfo.InvariantCulture);
        var resource = new List<string>(words.Count - 1);
        for (var i = 1; i < words.Count; i++)
        {
            resource.Add(words[i].ToLower(CultureInfo.InvariantCulture));
        }

        // 列表语义优先于读取语义：...PageList / ...List 一律投影为 list.*
        if (string.Equals(verb, "get", StringComparison.Ordinal) && words.Any(PageSuffixes.Contains))
        {
            verb = "list";
        }

        // 分页词在动词定型后就是噪声：...PageList / ...List 无论出现在资源段哪里都剥除
        resource.RemoveAll(PageSuffixes.Contains);

        // 按 id 取数不改变资源本身：GetMenuById 的资源是 menu，不是 menu_by_id
        while (resource.Count > 0 && ByIdTail.Contains(resource[resource.Count - 1]))
        {
            resource.RemoveAt(resource.Count - 1);
        }

        return resource.Count == 0
            ? module + "." + verb
            : module + "." + verb + "_" + string.Join("_", resource);
    }

    /// <summary>取分组键的首个 Pascal 段（<c>BitableAppTable</c> → <c>Bitable</c>）。</summary>
    private static string LeadingPascalSegment(string groupName)
    {
        var end = 1;
        while (end < groupName.Length && char.IsLower(groupName[end]))
        {
            end++;
        }

        return groupName.Substring(0, end);
    }

    /// <summary>
    /// Pascal 分词并剥去尾部 <c>Async</c>（连续大写按缩写词归入同一词，<c>QuerySSEvents</c> → Query/SSEvents）。
    /// </summary>
    private static List<string> SplitPascalWords(string methodName)
    {
        var name = methodName.EndsWith("Async", StringComparison.Ordinal)
            ? methodName.Substring(0, methodName.Length - "Async".Length)
            : methodName;

        var words = new List<string>();
        var start = 0;
        for (var i = 1; i < name.Length; i++)
        {
            if (!char.IsUpper(name[i]))
            {
                continue;
            }

            // 大写只在「前一个小写/数字」或「后一个小写」处断词，缩写词（Url/Id/SSE）不被切碎
            var prevLower = char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1]);
            var nextLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
            if (prevLower || nextLower)
            {
                words.Add(name.Substring(start, i - start));
                start = i;
            }
        }

        if (start < name.Length)
        {
            words.Add(name.Substring(start));
        }

        return words;
    }
}
