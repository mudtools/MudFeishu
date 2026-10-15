// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.Tests.ContractGuards;

/// <summary>
/// <b>NeutralPackages_ShouldNotContainProductVocabulary</b>（N6 上线，方案 v2 §6.4 / v2.1 Q2 定稿）：
/// <c>Mud.AI.*</c> 源码的<b>非注释行</b>不得出现产品词汇——「共享结构、不共享语义」的机械化。
/// </summary>
/// <remarks>
/// <para>
/// <b>判据（v2.1 设计订正 Q2）</b>：规则式注释行豁免（行首 <c>//</c> 跳过，覆盖 <c>///</c> XML 文档）
/// + 文件头版权块豁免（版权块里的 "Mud.Feishu 项目" 字样是法定文本，不是语义污染）；
/// 扫描词：<c>feishu</c>（忽略大小写）、<c>x-feishu</c>、<c>appKey</c>（驼峰形态——中立侧应统一
/// <c>tenantKey</c>）。不采用"全文扫描 + 逐条 XML 注释豁免"（豁免清单比守卫更难维护）。
/// </para>
/// <para>
/// <b>显式白名单（唯一一条）</b>：<c>ConversationRequest.AppKey</c>——通用会话请求的属性名
/// （PascalCase <c>AppKey</c>），19 处下游引用 + 中立副本注释已论证，刻意保留。
/// 故 <c>AppKey</c>（PascalCase）不扫描；<c>appKey</c>（驼峰）禁止。
/// </para>
/// <para>
/// 逃逸形态兜底：多行注释（<c>/* */</c>）内出现词汇不豁免（本仓现行无此形态；
/// 字符串字面量中出现产品 URL 也算违规——中立包不应硬编码产品事实）。
/// </para>
/// </remarks>
public class NeutralVocabularyContractGuards
{
    /// <summary>扫描目标：中立包族源码（<c>obj</c>/<c>bin</c> 排除）。</summary>
    private static readonly string[] NeutralProjects = ["Mud.AI.Abstractions", "Mud.AI.Agent"];

    /// <summary>产品词汇（正则，忽略大小写的 feishu 由模式内标志实现）。</summary>
    private static readonly Regex ProductVocabularyPattern = new(
        @"feishu|appKey",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// <c>AppKey</c>（PascalCase）白名单：仅 <see cref="ConversationRequest.AppKey"/> 属性名
    /// （唯一刻意保留的产品事实属性名；PascalCase 形态与被禁的驼峰 <c>appKey</c> 互斥）。
    /// </summary>
    private static readonly Regex AllowedAppKeyPattern = new(@"^[^""]*\bAppKey\b[^""]*$", RegexOptions.Compiled);

    [Fact]
    public void NeutralPackages_ShouldNotContainProductVocabulary()
    {
        var violations = new List<string>();
        foreach (var project in NeutralProjects)
        {
            var projectDirectory = Path.Combine(FindRepositoryRoot(), project);
            Directory.Exists(projectDirectory).Should().BeTrue($"扫描目标工程目录必须存在：{projectDirectory}");

            foreach (var file in Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
                         .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")))
            {
                var headerLines = CountCopyrightHeaderLines(file);
                var lineNumber = 0;
                foreach (var line in File.ReadLines(file))
                {
                    lineNumber++;
                    // 版权块豁免（法定文本）。
                    if (lineNumber <= headerLines)
                    {
                        continue;
                    }

                    // 规则式注释行豁免：行首 //（覆盖 /// XML 文档）。
                    var trimmed = line.TrimStart();
                    if (trimmed.StartsWith("//", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (!ProductVocabularyPattern.IsMatch(line))
                    {
                        continue;
                    }

                    // 唯一白名单：AppKey（PascalCase）属性名，且不在字符串字面量内。
                    if (AllowedAppKeyPattern.IsMatch(line) && ProductVocabularyPattern.Matches(line)
                            .All(match => match.Value == "AppKey"))
                    {
                        continue;
                    }

                    violations.Add($"{project}{file.Substring(file.LastIndexOf(project, StringComparison.Ordinal) + project.Length)}:{lineNumber}: {trimmed}");
                }
            }
        }

        violations.Should().BeEmpty(
            "中立包源码不得出现产品词汇（feishu / appKey——「共享结构、不共享语义」的机械化；"
            + "唯一白名单 = ConversationRequest.AppKey 属性名）。违规：\n{0}",
            string.Join("\n", violations));
    }

    /// <summary>
    /// 守卫自证：过滤器对<b>确知违规</b>的样本必须报红、对白名单形态必须放行——
    /// 没有"过滤器恒返回空"的假绿。
    /// </summary>
    [Fact]
    public void Scanner_ShouldFlagProductVocabulary_AndAllowAppKeyProperty()
    {
        ProductVocabularyPattern.IsMatch("var tenantKey = request.AppKey; // 举例").Should().BeTrue();
        ProductVocabularyPattern.Matches("var tenantKey = request.AppKey;")
            .All(static match => match.Value == "AppKey").Should().BeTrue("纯 AppKey 属性读写应被白名单放行");

        // 产品词汇本体（字符串形态）必须被点名。
        var sample = "var url = \"https://open.feishu.cn/x-feishu/xx\";";
        ProductVocabularyPattern.IsMatch(sample).Should().BeTrue();

        // 驼峰 appKey 必须被点名（中立侧应统一 tenantKey）。
        ProductVocabularyPattern.IsMatch("public string appKey { get; }").Should().BeTrue();
    }

    /// <summary>版权块行数（首行以 <c>// ------</c> 开始、以第二段 <c>-------</c> 结束的固定 6 行块）。</summary>
    private static int CountCopyrightHeaderLines(string file)
    {
        using var reader = new StreamReader(file);
        var count = 0;
        while (reader.ReadLine() is { } line)
        {
            count++;
            if (line.StartsWith("// ------", StringComparison.Ordinal) && count > 1)
            {
                return count;
            }

            if (count > 10)
            {
                return 0;
            }
        }

        return 0;
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
