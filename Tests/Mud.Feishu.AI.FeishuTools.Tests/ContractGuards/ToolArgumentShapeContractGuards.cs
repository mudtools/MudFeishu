// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// 任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.FeishuTools.Tests.ContractGuards;

/// <summary>
/// 参数形态认知契约守卫（WP7 / WP1）：断言 <c>JsonValueKind.String</c> 的参数值形态认知
/// 只允许出现在归一化中心（<c>ToolArgumentNormalizer</c>）与既有的摘要/值类型转换文件中——
/// 防止 S1（入站净化被 JsonElement 绕过）的根因（四处各自实现形态认知、漏一处即静默失效）复现。
/// </summary>
/// <remarks>
/// <para>
/// <b>判据（R2 评审修订）</b>：守卫只覆盖「净化与摘要遍历」场景——<c>ToolArgs</c> 的值类型转换
/// （提取标量）职责不同，不纳入禁止范围。允许的文件白名单：
/// <list type="bullet">
/// <item><c>ToolArgumentNormalizer.cs</c> — 形态认知的唯一中心（WP1 落地）；</item>
/// <item><c>ToolArgumentSanitizer.cs</c> — 净化入口，经 <c>ToolArgumentNormalizer.EnumerateTexts</c> 遍历；</item>
/// <item><c>ToolArgsDigester.cs</c> — 审计摘要的值形态描述（区分 str/array/json）；</item>
/// <item><c>FeishuApiResultReader.cs</c> — <c>ToolArgs</c> 值类型转换（提取标量，不涉及净化/摘要遍历）。</item>
/// </list>
/// </para>
/// <para>
/// 新增文件若需处理 <c>JsonElement</c> 的文本值，必须经 <c>ToolArgumentNormalizer.EnumerateTexts</c>——
/// 否则在本守卫报红。
/// </para>
/// </remarks>
public class ToolArgumentShapeContractGuards
{
    /// <summary>
    /// <c>JsonValueKind.String</c> 的参数值形态认知只允许出现在白名单文件中（S1 根因治理）。
    /// </summary>
    [Fact]
    public void JsonValueKindString_ShouldOnlyAppearInNormalizerAndAllowedFiles()
    {
        var root = FindRepositoryRoot();
        var toolsDir = Path.Combine(root, "Mud.Feishu.AI.FeishuTools", "Tools");

        // 白名单：允许处理 JsonValueKind.String 的文件（R2 评审修订后的范围）。
        var allowedFiles = new HashSet<string>(StringComparer.Ordinal)
        {
            "ToolArgumentNormalizer.cs",
            "ToolArgumentSanitizer.cs",
            "ToolArgsDigester.cs",
            "FeishuApiResultReader.cs", // ToolArgs 值类型转换
        };

        var offenders = Directory
            .EnumerateFiles(toolsDir, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(p => !allowedFiles.Contains(Path.GetFileName(p)))
            .Where(p => File.ReadAllText(p).Contains("JsonValueKind.String", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToList();

        offenders.Should().BeEmpty(
            "JsonValueKind.String 的参数值形态认知必须收敛到 ToolArgumentNormalizer（净化/摘要入口）"
            + "或白名单文件（ToolArgs 值类型转换）；各自 switch 是 S1 的根因（漏一处即静默失效）"
            + "——违规文件: " + string.Join(", ", offenders));
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
