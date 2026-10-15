// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Tests.Tools;

/// <summary>
/// 独立产品线纪律的机械哨兵：中立运行期（<c>Mud.AI.Agent</c>）不得持有任何<b>产品业务码</b>。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要它</b>：v2.3 P1-D 期间中立层自带飞书码表（<c>ForbiddenApiCodes</c> /
/// <c>RetryableApiCodes</c> / <c>ToolErrorCodeMeta</c>），使 P3 上游化（迁独立 Mud.AI 仓与包）
/// 无法成立。码表已搬到产品侧（经 <c>ToolApiFailure</c> 供给读数），本哨兵防"顺手加回来"。
/// </para>
/// <para>
/// <b>它是哨兵不是证明</b>：清单是<b>已知产品码</b>的枚举，不是"任意产品码"的判别式——
/// 新产品的码若未登记于此，本用例不会报红。这正是"哨兵"的定位（拦最常见的回潮路径），
/// 而不是"纪律已可机械证明"的宣称；真正的结构性保证是"读数由产品侧供给"这一形状本身。
/// </para>
/// <para>
/// <b>与 <c>AgentContractGuards.MudAiFamily_ShouldNotDeclareMudHttpUtilsComponent</c> 的分工</b>：
/// 那条管"包引用"（不得引 Mud 组件），本条管"码字面量"（不得抄产品码表）。
/// </para>
/// </remarks>
public class NeutralPackageProductFactGuards
{
    /// <summary>
    /// 已知的飞书业务码（哨兵清单）：本文件<b>允许</b>出现（它是对照集），实现侧不允许。
    /// </summary>
    private static readonly string[] KnownProductCodeLiterals =
        ["99991400", "99991672", "99991663", "99991661"];

    /// <summary>产品类型名与协议头（哨兵清单）：中立工具安全基元不得出现。</summary>
    private static readonly string[] ProductTypeTokens =
        ["ApiResult", "FeishuApiResultReader", "FeishuToolRisk", "FeishuToolBinding", "x-feishu"];

    /// <summary>中立运行期的源码里不得出现任何已知产品业务码（注释行除外——注释是记录，不是声明）。</summary>
    [Fact]
    public void NeutralRuntime_ShouldNotContainProductBusinessCodeLiterals()
    {
        var root = FindRepositoryRoot();
        var offenders = new List<string>();

        foreach (var file in Directory
            .EnumerateFiles(Path.Combine(root, "Mud.AI.Agent"), "*.cs", SearchOption.AllDirectories)
            .Where(static path => !IsBuildArtifact(path)))
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                if (ContainsProductCode(lines[i]))
                {
                    offenders.Add($"{Path.GetRelativePath(root, file)}:{i + 1}");
                }
            }
        }

        offenders.Should().BeEmpty(
            "Mud.AI.Agent 是可与 Mud 产品解耦发布的独立产品线：产品业务码表必须在产品侧"
            + "（经 ToolApiFailure 供给读数），不得在实现里复述。命中：{0}",
            string.Join(" | ", offenders));
    }

    /// <summary>判据自证：注释行豁免、普通数字不误伤、已知码命中。</summary>
    [Theory]
    [InlineData("var x = 99991400;", true)]
    [InlineData("if (code == 99991663) { }", true)]
    [InlineData("// 99991400（系统繁忙）——历史记录", false)]
    [InlineData("    // 99991661 权限不足", false)]
    [InlineData("var x = 12345;", false)]
    [InlineData("var x = \"upstream_error\";", false)]
    public void LiteralJudge_ShouldIgnoreComments_AndDetectKnownCodes(string line, bool expected)
        => ContainsProductCode(line).Should().Be(expected);

    /// <summary>
    /// 判据：行内含任一已知产品码即命中；整行注释豁免（本仓既有体例：注释里复述事实是<b>记录</b>，
    /// 不是声明——与源生成器守卫的"剔注释再匹配"同口径）。
    /// </summary>
    private static bool ContainsProductCode(string line)
    {
        if (line.TrimStart().StartsWith("//", StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var literal in KnownProductCodeLiterals)
        {
            if (line.Contains(literal, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// <b>阶段 A-1 哨兵</b>：迁入中立包的工具安全基元
    /// （<c>Mud.AI.Agent/Tools/{Args,Results,Security}</c>）不得持有产品<b>类型名</b>与产品<b>头前缀</b>。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>与上一条的分工</b>：上一条管"码字面量"，本条管"产品类型名/协议头"。两者都是<b>哨兵</b>——
    /// 拦已知的回潮路径（迁文件时把 <c>ApiResult</c> / <c>FeishuToolRisk</c> 一起带过来），
    /// 不是"任意产品耦合"的判别式（真正的结构保证是"中立侧只吃 <c>IReadOnlyDictionary</c>/<c>string</c>"）。
    /// </para>
    /// <para>
    /// <b>为什么只扫这三个目录</b>：它们是 A-1 的迁移落点；<c>Tools/</c> 其余子目录与 <c>Events/</c> 等
    /// 由各自守卫覆盖（例如 <c>TurnPipelineContractGuards</c> 管产品政策分支）。
    /// </para>
    /// </remarks>
    [Fact]
    public void NeutralToolSafetyPrimitives_ShouldStayProductFree()
    {
        var root = FindRepositoryRoot();
        var scanDirs = new[] { "Args", "Results", "Security" }
            .Select(dir => Path.Combine(root, "Mud.AI.Agent", "Tools", dir));

        var offenders = new List<string>();
        var scanned = 0;
        foreach (var dir in scanDirs)
        {
            Directory.Exists(dir).Should().BeTrue($"扫描目录必须存在（否则扫描面静默缩水 → 假绿）：{dir}");
            foreach (var file in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
                         .Where(static path => !IsBuildArtifact(path)))
            {
                scanned++;
                var lineNumber = 0;
                foreach (var line in File.ReadLines(file))
                {
                    lineNumber++;
                    if (ContainsProductTypeReference(line))
                    {
                        offenders.Add($"{Path.GetRelativePath(root, file)}:{lineNumber}");
                    }
                }
            }
        }

        scanned.Should().BePositive("扫描面为空是假绿（目录存在但枚举不到源码）");
        offenders.Should().BeEmpty(
            "中立工具安全基元不得引用产品类型/协议头（ApiResult / FeishuApiResultReader / FeishuToolRisk / "
            + "FeishuToolBinding / x-feishu）——它们只应处理 IReadOnlyDictionary/string 等中立形态。命中：{0}",
            string.Join(" | ", offenders));
    }

    /// <summary>判据自证：注释豁免、中立形态放行、产品类型名与协议头命中。</summary>
    [Theory]
    [InlineData("var result = new ApiResult();", true)]
    [InlineData("FeishuApiResultReader.Read(args);", true)]
    [InlineData("if (risk == FeishuToolRisk.Write) { }", true)]
    [InlineData("var header = \"x-feishu-tenant\";", true)]
    [InlineData("// 由 ApiResult 投影而来（注释是记录）", false)]
    [InlineData("var text = args[\"text\"]?.ToString();", false)]
    public void ProductTypeJudge_ShouldIgnoreComments_AndDetectProductCoupling(string line, bool expected)
        => ContainsProductTypeReference(line).Should().Be(expected);

    /// <summary>判据：行内含任一产品类型名/协议头即命中；整行注释豁免（与同族哨兵同口径）。</summary>
    private static bool ContainsProductTypeReference(string line)
    {
        if (line.TrimStart().StartsWith("//", StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var token in ProductTypeTokens)
        {
            if (line.Contains(token, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBuildArtifact(string path)
        => path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
        || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Mud.Feishu.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"未能从 {AppContext.BaseDirectory} 向上定位仓库根目录（Mud.Feishu.slnx）");
    }
}
