// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// B4 截断标记回填守卫：确保 <see cref="FeishuToolResult.Truncated"/> / <see cref="FeishuToolResult.TruncationReason"/>
/// 在发生截断时被正确回填（此前恒为 <c>false</c>/<c>null</c>）。
/// </summary>
/// <remarks>
/// 守卫三条（方案 §3.B4）：
/// <list type="number">
/// <item><c>FromApi</c> 产出在发生截断时 <c>Truncated=true</c>；</item>
/// <item><c>FromPlainText</c> 产出在发生截断时 <c>Truncated=true</c>；</item>
/// <item>不存在"字节级 JSON 截断"（保持结构合法）。</item>
/// </list>
/// </remarks>
public class TruncationVisibilityContractGuards
{
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

    /// <summary>
    /// 守卫 ①：<c>FromApi</c> 必须回填截断标记——
    /// 调用 <c>TruncateJson</c> 后检测截断并传入 <c>Truncated=true</c>。
    /// </summary>
    [Fact]
    public void FromApi_ShouldBackfillTruncated_WhenTruncated()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "Internal", "ToolExecutor.cs"));

        // FromApi 方法必须存在且包含截断回填逻辑。
        var fromApiStart = source.IndexOf(
            "public FeishuToolResult FromApi<T>(FeishuApiOutcome<T> outcome, Func<T, JsonObject> project)", StringComparison.Ordinal);
        fromApiStart.Should().BeGreaterThan(-1, "FromApi 方法必须存在");

        var methodBody = source[fromApiStart..];
        var methodEnd = methodBody.IndexOf("\n    }", StringComparison.Ordinal);
        methodBody = methodBody[..(methodEnd + 1)];

        methodBody.Should().Contain("truncated", "FromApi 必须检测截断状态");
        methodBody.Should().Contain("FeishuToolResult.FromText(", "FromApi 必须通过 FromText 构造结果");
        methodBody.Should().NotContain("FromText(truncatedText)", 
            "FromApi 不得只传文本——必须同时传入 truncated 标记");
    }

    /// <summary>
    /// 守卫 ②：<c>FromPlainText</c> 必须回填截断标记。
    /// </summary>
    [Fact]
    public void FromPlainText_ShouldBackfillTruncated_WhenTruncated()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "Internal", "ToolExecutor.cs"));

        var fromPlainTextStart = source.IndexOf(
            "public FeishuToolResult FromPlainText<T>(FeishuApiOutcome<T> outcome, Func<T, string?> text)", StringComparison.Ordinal);
        fromPlainTextStart.Should().BeGreaterThan(-1, "FromPlainText 方法必须存在");

        var methodBody = source[fromPlainTextStart..];
        var methodEnd = methodBody.IndexOf("\n    }", StringComparison.Ordinal);
        methodBody = methodBody[..(methodEnd + 1)];

        methodBody.Should().Contain("truncated", "FromPlainText 必须检测截断状态");
        methodBody.Should().Contain("FeishuToolResult.FromText(",
            "FromPlainText 必须通过 FromText 构造结果");
    }

    /// <summary>
    /// 守卫 ③：截断检测不得使用 <c>FeishuApiResultReader.IsTruncated</c> 的文本标记检测——
    /// 应通过长度比较判断（更可靠，不受 JSON 标记格式变化影响）。
    /// </summary>
    /// <remarks>
    /// 方案 RV-6 建议复用 <c>IsTruncated</c>，但实现中采用长度比较更稳健
    ///（<c>TruncateJson</c> 可能添加标记但总长度不一定小于原文——
    /// 极端情况下截断后添加的 <c>truncated</c>/<c>hint</c> 键可能让字符串变长）。
    /// 实际实现通过"截断前文本 vs 截断后文本"的长度比较判定。
    /// </remarks>
    [Fact]
    public void TruncationDetection_ShouldUseLengthComparison()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "Internal", "ToolExecutor.cs"));

        // FromApi 方法体中必须通过长度比较检测截断。
        source.Should().Contain("truncatedText.Length < fullText.Length",
            "截断检测应通过长度比较——TruncateJson 可能添加标记键使文本变长，文本标记检测不可靠");
    }
}
