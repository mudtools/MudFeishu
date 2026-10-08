// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;

namespace Mud.Feishu.AI.Tests.ContractGuards;

/// <summary>
/// HITL 续跑（R5-11）与坏值自愈收口（R5-4）的<b>结构守卫</b>。
/// </summary>
/// <remarks>
/// 两条纪律都属「机制缺失」而非「某处实现疏忽」，行为用例的覆盖面等于你想到的路径数；
/// 故与 <c>SilentCatchContractGuards</c> 同款，断言的是<b>结构</b>——新增代码自动被覆盖。
/// </remarks>
public class ApprovalContinuationContractGuards
{
    private static readonly string[] ScannedProjects =
    [
        "Mud.Feishu.AI",
        "Mud.Feishu.AI.Tools",
    ];

    /// <summary>
    /// R5-4（R4-7 纪律）：坏值自愈的 <c>catch</c> 过滤面必须<b>收口</b>为
    /// <c>when (ex is not OperationCanceledException)</c>，不得是「枚举白名单」。
    /// </summary>
    /// <remarks>
    /// 白名单形态的缺陷是<b>结构性</b>的：任何白名单之外的异常类型（<c>FormatException</c>、
    /// <c>KeyNotFoundException</c>、<c>NullReferenceException</c>…）会逃出守护区 ⇒ 幂等回滚 ⇒
    /// 重投递同点再抛 ⇒ 事件永久毒化循环。而"想全"是靠不住的（R4-7 的原始白名单即自称覆盖全部）。
    /// </remarks>
    [Fact]
    public void ExceptionFilters_ShouldNotEnumerateTypes()
    {
        var violations = new List<string>();
        var filterCount = 0;

        foreach (var project in ScannedProjects)
        {
            foreach (var file in EnumerateSources(project))
            {
                var source = File.ReadAllText(file);
                filterCount += EnumeratingExceptionFilterScanner.CountFilters(source);
                violations.AddRange(
                    EnumeratingExceptionFilterScanner.FindViolations(Path.GetFileName(file), source)
                        .Select(violation => $"{project}/{violation}"));
            }
        }

        filterCount.Should().BeGreaterThan(0, "扫描必须命中真实源码（扫描面为空说明路径解析坏了，那是假绿）");
        violations.Should().BeEmpty(
            "以下 catch 过滤面是「枚举类型白名单」——白名单外的异常会逃出坏值守护区并毒化事件循环。"
            + "请改为 `when (ex is not OperationCanceledException)`（取消必须原样传播）："
            + string.Join(" | ", violations));
    }

    /// <summary>检查器自证：枚举白名单形态必须报红。</summary>
    [Theory]
    [InlineData("catch (Exception ex) when (ex is JsonException or ArgumentException or NotSupportedException)")]
    [InlineData("catch (Exception ex) when (ex is JsonException)")]
    [InlineData("catch (Exception) when (ex is InvalidOperationException or FormatException)")]
    public void Scanner_ShouldReportViolation_ForEnumeratingFilter(string filter)
    {
        var source = $"class S {{ void M() {{ try {{ }} {filter} {{ }} }} }}";

        EnumeratingExceptionFilterScanner.FindViolations("S.cs", source)
            .Should().NotBeEmpty($"枚举白名单是缺陷原始形态（{filter}）——检查器必须报红，否则本守卫是假绿");
    }

    /// <summary>检查器自证：收口形态与其它合法过滤面都不得报红。</summary>
    [Theory]
    [InlineData("catch (Exception ex) when (ex is not OperationCanceledException)")]
    [InlineData("catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)")]
    [InlineData("catch (Exception ex)")]
    public void Scanner_ShouldNotReport_ForClosedFilter(string filter)
    {
        var source = $"class S {{ void M() {{ try {{ }} {filter} {{ }} }} }}";

        EnumeratingExceptionFilterScanner.FindViolations("S.cs", source).Should().BeEmpty();
    }

    /// <summary>
    /// R5-11：SDK 读取「框架记录的待审批请求」所用的状态键必须与 MAF 内部常量一致。
    /// </summary>
    /// <remarks>
    /// 该键是<b>跨程序集的隐性契约</b>（MAF 把它定为 <c>internal const</c>，SDK 只能以字面量对接）。
    /// 框架升级改名时，本守卫确保<b>测试期</b>就响亮失败，而不是把「HITL 续跑静默失效」带到生产。
    /// </remarks>
    [Fact]
    public void FrameworkPendingApprovalStateKey_ShouldMatchMachineAgentsAiConstant()
    {
        var type = typeof(ChatClientAgent).Assembly
            .GetType("Microsoft.Agents.AI.ApprovalResponseBindingChatClient");
        type.Should().NotBeNull(
            "框架绑定层类型必须存在（若 MAF 重命名/下沉该类型，请同步 Mud.Feishu.AI 的续跑桥实现）");

        var field = type!.GetField(
            "StateBagKey",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        field.Should().NotBeNull("框架绑定层的待审批状态键必须存在（R5-11 依赖它读取批准权威记录）");

        field!.GetRawConstantValue().Should().Be(
            FeishuPendingApprovalState.FrameworkStateKey,
            "SDK 的待审批状态键字面量必须与 MAF 内部常量逐字符一致——不一致会让 HITL 续跑静默失效");
    }

    private static IEnumerable<string> EnumerateSources(string project)
    {
        var root = Path.Combine(FindRepositoryRoot(), project);
        Directory.Exists(root).Should().BeTrue($"{project} 目录必须存在（扫描面不得静默为空）");

        return Directory
            .GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(static path => !path.Contains(
                $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    private static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory) && !File.Exists(Path.Combine(directory, "Mud.Feishu.slnx")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        directory.Should().NotBeNullOrEmpty("测试必须能定位仓库根目录（以 Mud.Feishu.slnx 为锚）");
        return directory!;
    }
}

/// <summary>
/// 「枚举类型白名单」<c>catch</c> 过滤器检查器（可被合成源码直接驱动，从而支持自证）。
/// </summary>
/// <remarks>
/// <b>判定口径（刻意保守、只看文本形态）</b>：<c>catch</c> 过滤面中若出现 <c>ex is</c> 且<b>不是</b>
/// <c>is not</c>，即视为枚举白名单（单类型 <c>ex is JsonException</c> 同属白名单形态）。
/// 不解析语义 ⇒ 不会漏报，可能误报（例如对某类型做"仅此一类可自愈"的刻意收敛）；
/// 误报的处置是改用 <c>is not</c> 形态并说明理由，成本极低。
/// </remarks>
internal static class EnumeratingExceptionFilterScanner
{
    /// <summary>统计源码中的 <c>catch</c> 过滤面数量（用于断言扫描面非空，防假绿）。</summary>
    public static int CountFilters(string source) => EnumerateCatchHeaders(source).Count();

    /// <summary>找出「枚举类型白名单」形态的过滤面（返回"文件名:行号"定位）。</summary>
    /// <param name="fileName">文件名（仅用于定位输出）。</param>
    /// <param name="source">源码全文。</param>
    /// <returns>违规定位列表。</returns>
    public static IReadOnlyList<string> FindViolations(string fileName, string source)
    {
        var violations = new List<string>();

        foreach (var (start, header) in EnumerateCatchHeaders(source))
        {
            var whenIndex = header.IndexOf("when", StringComparison.Ordinal);
            if (whenIndex < 0)
            {
                continue;
            }

            var filter = header[whenIndex..];
            if (filter.Contains(" is not ", StringComparison.Ordinal))
            {
                continue;
            }

            if (!filter.Contains(" is ", StringComparison.Ordinal))
            {
                continue;
            }

            violations.Add($"{fileName}:{LineOf(source, start)}");
        }

        return violations;
    }

    /// <summary>枚举全部 <c>catch</c> 头（<c>catch</c> 后必须是 <c>(</c>，避免命中注释里的「catch 块」表述）。</summary>
    private static IEnumerable<(int Start, string Header)> EnumerateCatchHeaders(string source)
    {
        var searchFrom = 0;
        while (searchFrom < source.Length)
        {
            var index = source.IndexOf("catch", searchFrom, StringComparison.Ordinal);
            if (index < 0)
            {
                yield break;
            }

            searchFrom = index + 5;

            if (index > 0 && (char.IsLetterOrDigit(source[index - 1]) || source[index - 1] == '_'))
            {
                continue;
            }

            var cursor = index + 5;
            while (cursor < source.Length && char.IsWhiteSpace(source[cursor]))
            {
                cursor++;
            }

            if (cursor >= source.Length || source[cursor] != '(')
            {
                // 注释/文档里的「catch 面」表述：不是真实 catch 子句。
                continue;
            }

            var bodyStart = source.IndexOf('{', cursor);
            if (bodyStart < 0)
            {
                yield break;
            }

            var header = source.Substring(index, bodyStart - index);
            if (header.Contains(';'))
            {
                continue;
            }

            yield return (index, header);
        }
    }

    private static int LineOf(string source, int index)
    {
        var line = 1;
        for (var i = 0; i < index && i < source.Length; i++)
        {
            if (source[i] == '\n')
            {
                line++;
            }
        }

        return line;
    }
}
