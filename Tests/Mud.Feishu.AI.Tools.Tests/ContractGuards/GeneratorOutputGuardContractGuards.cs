// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Xunit;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// G3（R2-09 / 根因 R-C）：生成器<b>每一条</b> <c>RegisterSourceOutput</c> 回调都必须经统一兜底包装。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么是结构断言而不是再补 5 条行为用例</b>：R1-WP5 为 <c>EmitToolSurface</c> 包了 try/catch 并上报
/// <c>MUDFT026</c>，其验收方式是"对该路径注入人为异常、断言产出 <c>MUDFT026</c> 而非 <c>CS8785</c>"。
/// 但 <c>Initialize</c> 中另有 5 条 <c>RegisterSourceOutput</c> 没有兜底——<b>行为用例的覆盖面
/// 等于你想到的路径数</b>，一旦机制应作用于 N 个同构位置，用例就必须穷举 N，漏项无声。
/// 结构断言的覆盖面等于代码里的实际路径数，<b>新增路径自动被覆盖</b>。
/// </para>
/// <para>
/// <b>自证报红</b>：<see cref="GuardChecker_ShouldReportUnwrappedRegistration_ForDefectShape"/>
/// 用合成源码直接驱动检查器并断言报红（缺陷原始形态 = 第二条实参是裸方法组/裸 lambda）。
/// 这取代了"临时改坏生产代码跑一次"的一次性自证——后者不会留存。
/// </para>
/// <para>
/// 本工程以普通库引用生成器（<c>ReferenceOutputAssembly=true</c>），但生成器<b>内部实现</b>
/// （<c>Guard&lt;T&gt;</c>）是 private，无法以符号方式断言；且断言对象是<b>源码结构</b>本身
/// （"每一处注册都经包装"）而非运行行为——故采用源码扫描，与 <c>SilentCatchContractGuards</c>
/// 同一体例。
/// </para>
/// </remarks>
public class GeneratorOutputGuardContractGuards
{
    private const string GeneratorFile = "Mud.Feishu.AI.Tools/FeishuToolSchemaGenerator.cs";

    /// <summary>生产生成器的每一处输出注册都必须经 <c>Guard(...)</c> 包装。</summary>
    [Fact]
    public void EverySourceOutput_ShouldBeWrappedByGuard()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), GeneratorFile));
        var registrations = GuardChecker.FindRegistrations(source);

        registrations.Should().HaveCountGreaterThanOrEqualTo(6,
            "FeishuToolSchemaGenerator 当前有 6 条输出路径；数量下降说明有路径被删除（须同批更新本守卫的下界与 R2-03 记录）");

        var unwrapped = registrations
            .Where(static r => !r.IsWrapped)
            .Select(static r => $"第 {r.Line} 行：{r.ArgumentPreview}")
            .ToArray();

        unwrapped.Should().BeEmpty(
            "以下 RegisterSourceOutput 的第二个实参未经 Guard(...) 包装——该路径的异常会退化为无定位的 CS8785"
            + "（MUDFT026 的故障隔离承诺将只兑现一部分）：" + string.Join(" | ", unwrapped));
    }

    /// <summary>
    /// 元守卫：<c>Guard</c> 必须放行 <see cref="OperationCanceledException"/>。
    /// </summary>
    /// <remarks>
    /// 把"用户取消构建"上报为 <c>MUDFT026</c>（Error 级 + 零容忍）会让取消变成构建失败——
    /// 这是本设计最容易写错的一行，故用源码断言钉住。
    /// </remarks>
    [Fact]
    public void Guard_ShouldRethrowOperationCanceledException()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), GeneratorFile));

        source.Should().Contain(
            "when (ex is not OperationCanceledException)",
            "Guard 的 catch 必须带 OperationCanceledException 例外过滤器（否则取消构建会变成构建失败）");
    }

    /// <summary>检查器自证：对缺陷原始形态（裸方法组 / 裸 lambda）必须报红。</summary>
    [Fact]
    public void GuardChecker_ShouldReportUnwrappedRegistration_ForDefectShape()
    {
        const string Defective = """
            public void Initialize(IncrementalGeneratorInitializationContext context)
            {
                context.RegisterSourceOutput(diagnostics, ReportPendingDiagnostics);
                context.RegisterSourceOutput(
                    models.Combine(assemblyName),
                    static (spc, input) => ToolArgsEmitter.Emit(spc, input.Left, input.Right));
            }
            """;

        var registrations = GuardChecker.FindRegistrations(Defective);
        registrations.Should().HaveCount(2, "检查器必须能定位两处注册（只找到 0 处说明解析坏了——那是假绿）");
        registrations.Should().OnlyContain(static r => !r.IsWrapped,
            "裸方法组与裸 lambda 都是 R1-WP5 遗漏的原始形态——检查器必须对二者报红");
    }

    /// <summary>检查器自证：合规形态（<c>Guard&lt;T&gt;(...)</c>）不得报红。</summary>
    [Fact]
    public void GuardChecker_ShouldNotReport_WrappedRegistration()
    {
        const string Compliant = """
            public void Initialize(IncrementalGeneratorInitializationContext context)
            {
                context.RegisterSourceOutput(
                    diagnostics,
                    Guard<ImmutableArray<PendingDiagnostic>>("诊断", ReportPendingDiagnostics));
            }
            """;

        GuardChecker.FindRegistrations(Compliant)
            .Should().OnlyContain(static r => r.IsWrapped);
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
/// <c>RegisterSourceOutput</c> 调用解析器（可被合成源码驱动 ⇒ 支持检查器自证）。
/// </summary>
/// <remarks>
/// <b>解析口径</b>：定位每一处 <c>RegisterSourceOutput(</c>，用括号配对找到调用结束，
/// 再取<b>顶层逗号</b>之后的实参文本——即"第二个实参"。逗号位于字符串/字符字面量内的情形
/// 在本文件里不存在（实参都是表达式），故不为此增加转义状态的复杂度。
/// </remarks>
internal static class GuardChecker
{
    /// <summary>一处输出注册的解析结果。</summary>
    /// <param name="Line">所在行号（1 起）。</param>
    /// <param name="IsWrapped">第二个实参是否形如 <c>Guard(...)</c> / <c>Guard&lt;T&gt;(...)</c>。</param>
    /// <param name="ArgumentPreview">第二个实参的预览（违规定位用）。</param>
    internal sealed record Registration(int Line, bool IsWrapped, string ArgumentPreview);

    /// <summary>解析源码中全部 <c>RegisterSourceOutput</c> 调用。</summary>
    /// <param name="source">源码全文。</param>
    /// <returns>按出现顺序的注册清单。</returns>
    public static IReadOnlyList<Registration> FindRegistrations(string source)
    {
        const string Marker = "RegisterSourceOutput(";
        var registrations = new List<Registration>();
        var searchFrom = 0;

        while (true)
        {
            var index = source.IndexOf(Marker, searchFrom, StringComparison.Ordinal);
            if (index < 0)
            {
                break;
            }

            var openParen = index + Marker.Length - 1;
            var closeParen = MatchParen(source, openParen);
            if (closeParen < 0)
            {
                break;
            }

            var argumentsStart = openParen + 1;
            var split = TopLevelComma(source, argumentsStart, closeParen);
            var secondArgument = split < 0
                ? string.Empty
                : source.Substring(split + 1, closeParen - split - 1).Trim();

            var preview = secondArgument.Length > 90 ? secondArgument.Substring(0, 90) + "…" : secondArgument;

            registrations.Add(new Registration(
                Line: LineOf(source, index),
                IsWrapped: secondArgument.StartsWith("Guard", StringComparison.Ordinal)
                           && secondArgument.Contains('('),
                ArgumentPreview: preview));

            searchFrom = closeParen + 1;
        }

        return registrations;
    }

    private static int MatchParen(string source, int openIndex)
    {
        var depth = 0;
        for (var i = openIndex; i < source.Length; i++)
        {
            if (source[i] == '(')
            {
                depth++;
            }
            else if (source[i] == ')')
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

    /// <summary>取 <c>[start, end)</c> 内第一处深度为 1 的逗号（即实参分隔符）。</summary>
    private static int TopLevelComma(string source, int start, int end)
    {
        var depth = 0;
        for (var i = start; i < end; i++)
        {
            var ch = source[i];
            if (ch is '(' or '[' or '{')
            {
                depth++;
            }
            else if (ch is ')' or ']' or '}')
            {
                depth--;
            }
            else if (ch == ',' && depth == 0)
            {
                return i;
            }
        }

        return -1;
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
