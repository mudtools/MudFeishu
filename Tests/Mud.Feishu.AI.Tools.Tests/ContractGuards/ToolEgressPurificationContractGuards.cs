// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// 任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// 出站净化穷尽守卫（WP2 / S3）：断言 <c>FeishuToolBinding</c> 内所有返回模型文本的出口
/// 均经过净化（<c>ToolResultSanitizer.Sanitize</c>），防止异常路径绕过净化。
/// </summary>
/// <remarks>
/// <para>
/// <b>守卫判据</b>：在 <c>FeishuToolBinding.ExecuteAsync</c> 方法体内，
/// 所有 <c>return FeishuToolResult.FromText(...)</c> 和 <c>return FeishuToolResult.FromError(...)</c>
/// 的出口表达式必须包含以下标记之一：
/// <list type="bullet">
/// <item><c>SanitizeResult</c>——正常路径经出站净化（L224）；</item>
/// <item><c>bounded</c>——catch 分支经出站净化后的截断结果（WP2 修复）；</item>
/// <item><c>StructuredError</c>——DenyAsync / 本地构造文案（不含外部数据）；</item>
/// <item><c>ShapeWithIsolationAsync</c>——整形钩子出口（入参已净化）。</item>
/// </list>
/// </para>
/// <para>
/// <b>自证报红</b>：若把 catch 分支改回直接 <c>return FeishuToolResult.FromError(errorText)</c>（未经净化），
/// 该守卫必须失败——否则守卫无效（"假绿"）。
/// </para>
/// </remarks>
public class ToolEgressPurificationContractGuards
{
    private const string BindingFile = "Mud.Feishu.AI.Tools/Tools/FeishuToolBinding.cs";

    /// <summary>
    /// FeishuToolBinding 内所有 FeishuToolResult 出口必须经过净化或使用本地构造文案。
    /// </summary>
    [Fact]
    public void ToolEgress_ShouldAlwaysPassThroughSanitizer()
    {
        var source = ReadBindingSource();

        // R3-4：模型可见出口的唯一闸门 EgressResult(string message) 自身即净化点——
        // 其内部的 return FeishuToolResult.FromError(safe) 就是「净化后的出口」本身，
        // 不应对本次穷尽扫描重复判红（否则收口反而触发假红）。闸门自身的净化 + 截断
        // 由 ModelVisibleEgress_ShouldBeFunneledThroughSanitizingConstructor 断言。
        // B2：带 ToolErrorCategory 的分类出口同理——它是登记的净化点（自净化由
        // EveryToolErrorCategory_ShouldHavePurifiedEgress ③ 断言），不参与穷尽扫描。
        var coreSink = Regex.Match(
            source,
            @"private\s+FeishuToolResult\s+EgressResult\(string\s+message\)\s*\{[^{}]*\}",
            RegexOptions.Singleline);
        coreSink.Success.Should().BeTrue(
            "R3-4：必须存在模型可见出口的唯一闸门 EgressResult(string message)");
        var classifiedSink = Regex.Match(
            source,
            @"private\s+FeishuToolResult\s+EgressResult\(string\s+toolName,\s*ToolErrorCategory\s+category[^)]*\)\s*\{[^{}]*\}",
            RegexOptions.Singleline);
        classifiedSink.Success.Should().BeTrue(
            "B2：必须存在带 ToolErrorCategory 的分类出口（首行 JSON 载荷 + 净化正文）");
        var scanned = source;
        foreach (var (index, length) in new[] { (coreSink.Index, coreSink.Length), (classifiedSink.Index, classifiedSink.Length) }
                     .OrderByDescending(static sink => sink.Item1))
        {
            scanned = scanned.Remove(index, length);
        }

        // 匹配所有 return FeishuToolResult.From*(...) 语句（闸门自身已剔除）。
        var exits = Regex.Matches(scanned, @"return\s+(?:await\s+)?FeishuToolResult\.From(?:Text|Error)\(([^;]*)\);");

        var violations = new List<string>();
        foreach (System.Text.RegularExpressions.Match m in exits)
        {
            var expr = m.Groups[1].Value;

            // 允许的出口形态：
            // 1. SanitizeResult —— 正常路径经出站净化
            // 2. bounded —— catch 分支经净化+截断后的结果（WP2 修复）
            // 3. StructuredError —— DenyAsync / 本地构造文案（不含外部数据）
            // 4. ShapeWithIsolationAsync —— 整形钩子出口（入参已净化）
            var isClean = expr.Contains("SanitizeResult", StringComparison.Ordinal)
                         || expr.Contains("bounded", StringComparison.Ordinal)
                         || expr.Contains("StructuredError", StringComparison.Ordinal)
                         || expr.Contains("ShapeWithIsolationAsync", StringComparison.Ordinal);

            if (!isClean)
            {
                violations.Add(expr.Trim());
            }
        }

        violations.Should().BeEmpty(
            "FeishuToolBinding 内以下 FeishuToolResult 出口未经过出站净化——所有返回模型的文本出口必须经 SanitizeResult/bounded/StructuredError（WP2/S3）：{0}",
            string.Join(" | ", violations));
    }

    /// <summary>
    /// catch 分支必须经出站净化（S3 核心回归守卫）。
    /// </summary>
    /// <remarks>
    /// 如果 catch 分支改为直接 <c>return FeishuToolResult.FromError(raw)</c> 而不经
    /// <c>ToolResultSanitizer.Sanitize</c>，该守卫必须失败。
    /// </remarks>
    [Fact]
    public void CatchBranch_ShouldSanitizeErrorText()
    {
        var source = ReadBindingSource();

        // 定位 catch 块内的 return 语句，断言它使用 bounded（已净化）变量。
        var catchIdx = source.IndexOf("catch (Exception ex)", StringComparison.Ordinal);
        catchIdx.Should().BeGreaterThan(-1, "FeishuToolBinding 必须有 catch (Exception ex) 分支");

        var catchSegment = source[catchIdx..];
        // 取 catch 块到下一个 finally 或方法结尾的范围。
        var finallyIdx = catchSegment.IndexOf("\n        finally", StringComparison.Ordinal);
        if (finallyIdx > 0)
        {
            catchSegment = catchSegment[..finallyIdx];
        }

        catchSegment.Should().Contain("ToolResultSanitizer.Sanitize",
            "catch 分支必须经出站净化（WP2/S3 修复）——异常消息可能携带 URL/响应体片段");
        catchSegment.Should().Contain("bounded",
            "catch 分支的返回值必须使用净化后的 bounded 变量");
    }

    /// <summary>
    /// R3-4：模型可见出口必须收口到**唯一构造器** <c>EgressResult(string message)</c>，
    /// 且该构造器自身必须净化 + 截断。
    /// </summary>
    /// <remarks>
    /// <b>为什么必须新增这条</b>：原守卫只校验「<c>return FeishuToolResult.From*(...)</c> 的表达式含
    /// <c>StructuredError</c>」——而拒绝文案（<c>invalid_args</c> 含模型可控键名）正是走
    /// <c>StructuredError</c> 却<b>未</b>经净化，属守卫盲区。收口后判据改为「一切模型可见出口都必须
    /// 经过携带净化/截断的 <c>EgressResult</c>」。
    /// </remarks>
    [Fact]
    public void ModelVisibleEgress_ShouldBeFunneledThroughSanitizingConstructor()
    {
        var source = ReadBindingSource();

        // ① 唯一闸门自身必须净化 + 截断（否则"收口"只是换了个名字）。
        var core = Regex.Match(
            source,
            @"private\s+FeishuToolResult\s+EgressResult\(string\s+message\)\s*\{(?<body>[^{}]*)\}",
            RegexOptions.Singleline);
        core.Success.Should().BeTrue(
            "R3-4：必须存在模型可见出口的唯一构造器 EgressResult(string message)");
        core.Groups["body"].Value.Should().Contain(
            "ToolResultSanitizer.Sanitize", "出口闸门必须净化（与成功路径同源）");
        core.Groups["body"].Value.Should().Contain(
            "ToolResultText.Truncate", "出口闸门必须按 MaxToolResultLength 截断（预算约束）");

        // ② 拒绝出口必须共用它（DenyAsync 的模型可见返回值）。
        var denyIdx = source.IndexOf("private async Task<FeishuToolResult> DenyAsync(", StringComparison.Ordinal);
        denyIdx.Should().BeGreaterThan(-1, "FeishuToolBinding 必须有统一拒绝出口 DenyAsync");
        var denyTail = source[denyIdx..];
        var nextMemberIdx = denyTail.IndexOf("\n    /// <summary>", StringComparison.Ordinal);
        (nextMemberIdx > 0 ? denyTail[..nextMemberIdx] : denyTail).Should().Contain(
            "EgressResult(", "拒绝文案同属外部数据出口，必须经唯一闸门净化（R3-4 消除双标）");

        // ③ 不得再出现"绕过闸门的裸结构化错误出口"。
        Regex.Matches(source, @"return\s+FeishuToolResult\.FromError\(StructuredError\(")
            .Should().BeEmpty(
                "所有 return 的结构化错误文案都必须经 EgressResult 净化——"
                + "裸 FromError(StructuredError(...)) 会让新出口（拒绝/HITL/上下文缺失）再次漏掉净化");
    }

    /// <summary>
    /// R3-4 / T10：模型可见出口面必须**封闭**——<c>EgressResult</c> 重载恰好是被登记的 3 个，
    /// 且每个重载要么自身净化（唯一核心闸门）要么委派给核心闸门；带 <c>ToolErrorCategory</c>
    /// 的分类出口唯一。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>与既有守卫的分工（不重复）</b>：<see cref="ModelVisibleEgress_ShouldBeFunneledThroughSanitizingConstructor"/>
    /// 断言的是「<b>现有</b>出口都经过净化」；它无法阻止**将来新增**一个重载并直接
    /// <c>return FeishuToolResult.FromError(...)</c> —— 那处新出口不在旧断言的扫描清单里。
    /// 本守卫把「重载面」钉死：新增出口必然改动本文件的重载数，从而**同时**触发本断言失败，
    /// 逼迫作者回到此处登记净化判据（守卫自维护，而非依赖评审者记忆）。
    /// </para>
    /// </remarks>
    [Fact]
    public void EveryToolErrorCategory_ShouldHavePurifiedEgress()
    {
        var source = ReadBindingSource();

        // ① 出口面封闭：EgressResult 重载恰好 3 个（核心净化 / 带分类 / 无分类）。
        var overloads = Regex.Matches(
            source,
            @"private\s+FeishuToolResult\s+EgressResult\([^)]*\)");
        overloads.Count.Should().Be(3,
            "模型可见出口面必须封闭为 3 个已登记重载（核心净化 / 带 ToolErrorCategory / 无分类）——"
            + "新增重载即新增一条模型可见路径，必须同步在此登记其净化判据");

        // ② 每个重载要么自身是净化核心，要么委派给核心闸门（不允许"自己拼文案直接返回"）。
        // 判据窗口 = 从签名尾到下一个成员声明（而非固定 200 字符）：B2 错误契约落地后
        // 分类出口方法体变长（ToolError 载荷构造），固定窗口会把 Sanitize 调用点切在窗外，
        // 使本判据对真实的未净化出口漏检。
        var selfContained = new List<string>();
        foreach (System.Text.RegularExpressions.Match overload in overloads)
        {
            var bodyStart = overload.Index + overload.Length;
            var nextMember = Regex.Match(
                source[bodyStart..],
                @"\n    (?:/// <summary>|private |public |internal )",
                RegexOptions.Singleline);
            var tailEnd = nextMember.Success ? bodyStart + nextMember.Index : source.Length;
            var tail = source[bodyStart..tailEnd];
            var delegates = tail.Contains("EgressResult(", StringComparison.Ordinal);
            var sanitizes = tail.Contains("ToolResultSanitizer.Sanitize", StringComparison.Ordinal);
            if (!delegates && !sanitizes)
            {
                selfContained.Add(overload.Value.Trim());
            }
        }

        selfContained.Should().BeEmpty(
            "以下 EgressResult 重载既不自净化也不委派核心闸门——它是一条绕过净化的模型可见出口：{0}",
            string.Join(" | ", selfContained));

        // ③ 带分类的出口唯一：ToolErrorCategory → 模型文本只有一条路径，且它必须自净化。
        var classified = Regex.Match(
            source,
            @"private\s+FeishuToolResult\s+EgressResult\(string\s+toolName,\s*ToolErrorCategory\s+category,\s*string\s+subtype,\s*string\s+reason[^)]*\)\s*(?<body>=>[^;]*;|\{.*?\})",
            RegexOptions.Singleline);
        classified.Success.Should().BeTrue(
            "带 ToolErrorCategory 的分类出口必须存在（拒绝路径与 catch 分支共用，R3-4 收口）");
        classified.Groups["body"].Value.Should().Contain(
            "ToolResultSanitizer.Sanitize", "分类出口必须自净化（B2 错误契约：首行 JSON + 正文经净化 + 截断）");
    }

    private static string ReadBindingSource()
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(root, BindingFile.Replace('/', Path.DirectorySeparatorChar));
        return File.ReadAllText(path);
    }

    private static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory) && !File.Exists(Path.Combine(directory, "Mud.Feishu.slnx")))
        {
            directory = Path.GetDirectoryName(directory)!;
        }

        directory.Should().NotBeNullOrEmpty("测试必须能定位仓库根目录（以 Mud.Feishu.slnx 为锚）");
        return directory!;
    }
}
