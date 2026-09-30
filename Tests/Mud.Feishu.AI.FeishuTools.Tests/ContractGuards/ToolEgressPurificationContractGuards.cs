// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// 任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.FeishuTools.Tests.ContractGuards;

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
    private const string BindingFile = "Mud.Feishu.AI.FeishuTools/Tools/FeishuToolBinding.cs";

    /// <summary>
    /// FeishuToolBinding 内所有 FeishuToolResult 出口必须经过净化或使用本地构造文案。
    /// </summary>
    [Fact]
    public void ToolEgress_ShouldAlwaysPassThroughSanitizer()
    {
        var source = ReadBindingSource();

        // 匹配 ExecuteAsync 方法体中的所有 return FeishuToolResult.From*(...) 语句。
        var exits = Regex.Matches(source, @"return\s+(?:await\s+)?FeishuToolResult\.From(?:Text|Error)\(([^;]*)\);");

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
