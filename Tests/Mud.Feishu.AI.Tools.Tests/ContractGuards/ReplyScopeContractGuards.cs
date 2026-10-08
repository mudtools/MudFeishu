// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// R3-1/R3-2（P0）防复发守卫：<b>回复路径必须收口到基类租户作用域</b>。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么是元守卫</b>：缺陷形态是"某个会话式事件器覆写了 <c>ReplyAsync</c> 却忘了切租户上下文"——
/// 行为用例的覆盖面等于你想到的处理器数量，而本守卫断言的是<b>结构</b>：
/// 「每一个 <c>ReplyAsync</c> 覆写体内都必须出现 <c>BeginAppScope(</c>」，因此<b>新增的处理器自动被覆盖</b>，
/// 不需要任何人记得回来补用例（与 <see cref="SilentCatchContractGuards"/> 同款思路）。
/// </para>
/// <para>
/// <b>判据为什么是"文本里出现 BeginAppScope("而不是"分析调用位置"</b>：保守但不会漏报——
/// 忘了切的处理器必然不含该调用（报红）；含该调用的处理器即便位置不理想，也有
/// <c>FeishuToolBindingTests</c> / 事件器行为用例（作用域先于下游调用）兜底。
/// </para>
/// </remarks>
public class ReplyScopeContractGuards
{
    private static readonly string[] ScannedProjects =
    [
        "Mud.Feishu.AI",
        "Mud.Feishu.AI.Tools",
    ];

    /// <summary>T11：所有 <c>ReplyAsync</c> 覆写体必须先建立 SDK 租户作用域。</summary>
    [Fact]
    public void EveryReplyAsyncOverride_ShouldCallBeginAppScope()
    {
        var violations = new List<string>();
        var overrideCount = 0;

        foreach (var project in ScannedProjects)
        {
            foreach (var file in EnumerateSources(project))
            {
                var source = File.ReadAllText(file);
                var bodies = ReplyScopeScanner.FindReplyAsyncOverrideBodies(source);
                overrideCount += bodies.Count;
                violations.AddRange(
                    bodies.Where(static body => !body.Text.Contains("BeginAppScope(", StringComparison.Ordinal))
                        .Select(body => $"{project}/{Path.GetFileName(file)}:{ReplyScopeScanner.LineOf(source, body.Start)}"));
            }
        }

        overrideCount.Should().BeGreaterThanOrEqualTo(3, "扫描必须命中真实覆写（命中数为 0 说明路径解析坏了，那是假绿）");
        violations.Should().BeEmpty(
            "以下 ReplyAsync 覆写未建立租户作用域——多应用宿主下会以默认应用身份把回复发给别的租户（TMA2-20）。"
            + $"请在发出任何下游调用之前 `using var appScope = BeginAppScope(request.AppKey);`：{string.Join(" | ", violations)}");
    }

    /// <summary>T12：所有会话式事件器必须把作用域工厂传给基类（否则基类模板方法恒为 no-op/恒抛）。</summary>
    [Fact]
    public void ConversationalHandlers_ShouldInjectScopeFactory()
    {
        var violations = new List<string>();
        var handlerCount = 0;

        foreach (var project in ScannedProjects)
        {
            foreach (var file in EnumerateSources(project))
            {
                var source = File.ReadAllText(file);
                foreach (var handler in ReplyScopeScanner.FindConversationalHandlers(source))
                {
                    handlerCount++;
                    if (!handler.CtorParameters.Contains("IFeishuAppContextScopeFactory", StringComparison.Ordinal))
                    {
                        violations.Add($"{project}/{Path.GetFileName(file)}:{handler.Line} 构造签名未接收 {nameof(IFeishuAppContextScopeFactory)}");
                    }
                    else if (!handler.BaseArguments.Contains("appContextScopeFactory:", StringComparison.Ordinal))
                    {
                        violations.Add($"{project}/{Path.GetFileName(file)}:{handler.Line} 未把工厂转发给基类（appContextScopeFactory: …）");
                    }
                }
            }
        }

        handlerCount.Should().BeGreaterThanOrEqualTo(3, "扫描必须命中真实处理器（命中数为 0 说明路径解析坏了，那是假绿）");
        violations.Should().BeEmpty(
            "会话式事件器必须接收并把 IFeishuAppContextScopeFactory 转发给基类，否则基类 BeginAppScope 无法建立租户作用域："
            + string.Join(" | ", violations));
    }

    /// <summary>
    /// 检查器自证：对<b>缺陷原始形态</b>（覆写 <c>ReplyAsync</c> 但未切租户）必须报红。
    /// </summary>
    [Fact]
    public void Scanner_ShouldReportViolation_ForReplyWithoutAppScope()
    {
        const string Defective = """
            public sealed class LegacyHandler : ConversationalFeishuEventHandler<X>
            {
                protected override async Task ReplyAsync(ConversationRequest request, string text, CancellationToken ct)
                {
                    await _messageClient.ReplyMessageAsync(request.MessageId, text, ct);
                }
            }
            """;

        var bodies = ReplyScopeScanner.FindReplyAsyncOverrideBodies(Defective);

        bodies.Should().ContainSingle("原始形态必须被识别为 ReplyAsync 覆写，否则守卫是假绿");
        bodies[0].Text.Should().NotContain("BeginAppScope(", "原始形态不含作用域建立调用——检查器对它必须报红");
    }

    /// <summary>检查器自证：合规形态（覆写体内建立作用域）不得报红。</summary>
    [Fact]
    public void Scanner_ShouldNotReport_ForReplyWithAppScope()
    {
        const string Compliant = """
            public sealed class FixedHandler : ConversationalFeishuEventHandler<X>
            {
                protected override async Task ReplyAsync(ConversationRequest request, string text, CancellationToken ct)
                {
                    using var appScope = BeginAppScope(request.AppKey);
                    await _messageClient.ReplyMessageAsync(request.MessageId, text, ct);
                }
            }
            """;

        var bodies = ReplyScopeScanner.FindReplyAsyncOverrideBodies(Compliant);

        bodies.Should().ContainSingle();
        bodies[0].Text.Should().Contain("BeginAppScope(");
    }

    private static IEnumerable<string> EnumerateSources(string project)
    {
        var root = Path.Combine(FindRepositoryRoot(), project);
        Directory.Exists(root).Should().BeTrue($"{project} 目录必须存在（扫描面不得静默为空）");

        return Directory
            .GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(static path => !IsBuildOutput(path));
    }

    private static bool IsBuildOutput(string path)
        => path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
           || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

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
/// 「回复作用域」源码检查器（可被合成源码直接驱动，从而支持检查器自证）。
/// </summary>
/// <remarks>
/// 只看文本形态、不做语义分析：<c>override</c> 与 <c>ReplyAsync</c> 之间不得跨过语句边界（<c>;</c>）或块边界（<c>{</c>/<c>}</c>），
/// 命中后再做大括号配对取方法体。保守口径 ⇒ 不漏报，可能误报（误报处置是就地补齐调用，成本极低）。
/// </remarks>
internal static class ReplyScopeScanner
{
    /// <summary>方法体（<paramref name="Start"/> 为大括号位置）与所属类型声明定位。</summary>
    /// <param name="Start">方法体左大括号在源码中的偏移。</param>
    /// <param name="Text">方法体全文（含大括号）。</param>
    public readonly record struct MethodBody(int Start, string Text);

    /// <summary>会话式事件器：类型声明的构造参数、基类初始化实参、声明行号。</summary>
    /// <param name="CtorParameters">主构造参数表（显式构造函数形态下为空串）。</param>
    /// <param name="BaseArguments">基类初始化实参表。</param>
    /// <param name="Line">类型声明所在行号。</param>
    public readonly record struct ConversationalHandler(string CtorParameters, string BaseArguments, int Line);

    /// <summary>找出全部 <c>ReplyAsync</c> 覆写的方法体。</summary>
    public static IReadOnlyList<MethodBody> FindReplyAsyncOverrideBodies(string source)
    {
        var results = new List<MethodBody>();
        var searchFrom = 0;

        while (searchFrom < source.Length)
        {
            var index = source.IndexOf("override", searchFrom, StringComparison.Ordinal);
            if (index < 0)
            {
                break;
            }

            searchFrom = index + 8;

            if ((index > 0 && (char.IsLetterOrDigit(source[index - 1]) || source[index - 1] == '_'))
                || SilentCatchScanner.IsInsideCommentOrString(source, index))
            {
                continue;
            }

            var replyIndex = source.IndexOf("ReplyAsync", index + 8, StringComparison.Ordinal);
            if (replyIndex < 0)
            {
                break;
            }

            // 声明形态：override 与 ReplyAsync 之间只能是修饰符/返回类型（跨过语句或块边界即说明是别的方法）。
            var between = source.AsSpan(index, replyIndex - index);
            if (between.Contains(';') || between.Contains('{') || between.Contains('}'))
            {
                continue;
            }

            var parenStart = source.IndexOf('(', replyIndex);
            var braceStart = parenStart < 0 ? -1 : source.IndexOf('{', parenStart);
            if (braceStart < 0)
            {
                continue;
            }

            // 表达式体（=>）形态不取块：交由后续守卫显式拒绝，而不是静默放过。
            var braceEnd = MatchDelimiter(source, braceStart, '{', '}');
            if (braceEnd < 0)
            {
                break;
            }

            results.Add(new MethodBody(braceStart, source.Substring(braceStart, braceEnd - braceStart + 1)));
            searchFrom = braceEnd + 1;
        }

        return results;
    }

    /// <summary>找出全部继承 <c>ConversationalFeishuEventHandler&lt;T&gt;</c> 的类型声明。</summary>
    public static IReadOnlyList<ConversationalHandler> FindConversationalHandlers(string source)
    {
        const string BaseMarker = ": ConversationalFeishuEventHandler<";
        var results = new List<ConversationalHandler>();
        var searchFrom = 0;

        while (searchFrom < source.Length)
        {
            var index = source.IndexOf(BaseMarker, searchFrom, StringComparison.Ordinal);
            if (index < 0)
            {
                break;
            }

            searchFrom = index + BaseMarker.Length;

            if (SilentCatchScanner.IsInsideCommentOrString(source, index))
            {
                continue;
            }

            var baseArgsStart = source.IndexOf('(', index + BaseMarker.Length);
            if (baseArgsStart < 0)
            {
                break;
            }

            var baseArgsEnd = MatchDelimiter(source, baseArgsStart, '(', ')');
            if (baseArgsEnd < 0)
            {
                break;
            }

            var baseArguments = source.Substring(baseArgsStart, baseArgsEnd - baseArgsStart + 1);
            results.Add(new ConversationalHandler(
                FindPrimaryCtorParameters(source, index),
                baseArguments,
                LineOf(source, index)));
        }

        return results;
    }

    /// <summary>取类型声明的主构造参数表（自基类列表向前回溯到最近的 <c>class</c> 声明）。</summary>
    private static string FindPrimaryCtorParameters(string source, int baseListIndex)
    {
        var classIndex = source.LastIndexOf("class ", baseListIndex, StringComparison.Ordinal);
        if (classIndex < 0)
        {
            return string.Empty;
        }

        var parenStart = source.IndexOf('(', classIndex);
        if (parenStart < 0 || parenStart > baseListIndex)
        {
            return string.Empty; // 显式构造函数形态：参数表在类型体内，由守卫的另一条断言覆盖。
        }

        var parenEnd = MatchDelimiter(source, parenStart, '(', ')');
        return parenEnd < 0 ? string.Empty : source.Substring(parenStart, parenEnd - parenStart + 1);
    }

    /// <summary>匹配成对分隔符（不区分泛型尖括号；调用方只对 <c>()</c>/<c>{}</c> 使用）。</summary>
    private static int MatchDelimiter(string source, int openIndex, char open, char close)
    {
        var depth = 0;
        for (var i = openIndex; i < source.Length; i++)
        {
            if (source[i] == open)
            {
                depth++;
            }
            else if (source[i] == close)
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    /// <summary>源码偏移 → 行号（1 起）。</summary>
    public static int LineOf(string source, int index)
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