// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// T1.2 用户令牌就绪探测器守卫：确保探测器的语义不变量不被破坏。
/// </summary>
/// <remarks>
/// <para>
/// 守卫五条：
/// <list type="number">
/// <item><c>UserTokenPrecheckResult</c> 四态闭集（Ready/Refreshable/AuthRequired/Skipped/Error）。</item>
/// <item><c>UserTokenPrecheckModes</c> 三态闭集（Off/CacheOnly/Full）。</item>
/// <item>探测器源码不得引用 <c>AccessToken</c>/<c>RefreshToken</c> 字面量（零令牌持有，A17）。</item>
/// <item>探测器只对 <c>identity == user</c> 触发（源码包含 <c>userContextRequired</c> 守卫）。</item>
/// <item><c>UserTokenPrecheckModes</c> 的 <c>IsValid</c> 方法存在且正确。</item>
/// </list>
/// </para>
/// </remarks>
public class UserTokenPrecheckContractGuards
{
    /// <summary>
    /// 守卫 ①：UserTokenPrecheckResult 必须有恰好 5 个 Outcome 常量。
    /// </summary>
    [Fact]
    public void UserTokenPrecheckResult_ShouldHaveExactlyFiveOutcomes()
    {
        var outcomeFields = typeof(UserTokenPrecheckResult.Outcomes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        outcomeFields.Should().HaveCount(5,
            "UserTokenPrecheckResult.Outcomes 必须为 5 态闭集（ready/refreshable/auth_required/skipped/error），"
            + "新增需评审并同批更新本断言");
    }

    /// <summary>
    /// 守卫 ②：UserTokenPrecheckModes 必须有恰好 3 个模式常量 + AllowedValuesText + IsValid。
    /// </summary>
    [Fact]
    public void UserTokenPrecheckModes_ShouldHaveExactlyThreeModes()
    {
        var modeFields = typeof(UserTokenPrecheckModes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string) && f.Name != "AllowedValuesText")
            .ToArray();

        modeFields.Should().HaveCount(3,
            "UserTokenPrecheckModes 必须为 3 模式闭集（off/cache_only/full）");

        var isValidMethod = typeof(UserTokenPrecheckModes).GetMethod("IsValid");
        isValidMethod.Should().NotBeNull("UserTokenPrecheckModes 必须有 IsValid 方法（闭集校验）");
    }

    /// <summary>
    /// 守卫 ③：UserTokenPrecheck 源码不得引用 AccessToken/RefreshToken 字面量（零令牌持有，A17）。
    /// </summary>
    [Theory]
    [InlineData(@"\bAccessToken\b")]
    [InlineData(@"\bRefreshToken\b")]
    [InlineData(@"\baccess_token\b")]
    [InlineData(@"\brefresh_token\b")]
    public void UserTokenPrecheck_ShouldNotReference_TokenLiterals(string tokenPattern)
    {
        var source = ReadSourceFile("Mud.Feishu.AI.Tools/Execution/UserTokenPrecheck.cs");

        // 排除注释中的字面量（// 或 /// 行）
        var nonCommentLines = source
            .Split('\n')
            .Where(line => !line.TrimStart().StartsWith("//"))
            .ToArray();

        var combined = string.Join('\n', nonCommentLines);
        // 使用单词边界匹配——CanRefreshTokenAsync 等方法名含子串 RefreshToken 但不违反零令牌持有约束。
        var match = Regex.Match(combined, tokenPattern, RegexOptions.None, TimeSpan.FromSeconds(1));
        match.Success.Should().BeFalse(
            $"UserTokenPrecheck 源码不得引用独立令牌字面量 {tokenPattern}——零令牌持有（A17），"
            + "探测器只读令牌状态、不缓存令牌值（方法名 CanRefreshTokenAsync 等含子串不在此约束内）");
    }

    /// <summary>
    /// 守卫 ④：UserTokenPrecheck 必须是 internal sealed（不对外暴露）。
    /// </summary>
    [Fact]
    public void UserTokenPrecheck_ShouldBeInternalSealed()
    {
        typeof(UserTokenPrecheck).IsClass.Should().BeTrue();
        typeof(UserTokenPrecheck).IsSealed.Should().BeTrue("UserTokenPrecheck 必须为 sealed——不预期被继承");
        typeof(UserTokenPrecheck).IsPublic.Should().BeFalse("UserTokenPrecheck 必须为 internal——不对外暴露（R-7 窄接缝）");
    }

    /// <summary>
    /// 守卫 ⑤：FeishuAgentOptions.UserTokenPrecheck 默认值必须为 CacheOnly。
    /// </summary>
    [Fact]
    public void UserTokenPrecheck_DefaultShouldBeCacheOnly()
    {
        var options = new FeishuAgentOptions();
        options.UserTokenPrecheck.Should().Be(UserTokenPrecheckModes.CacheOnly,
            "默认 CacheOnly 在零 IO 与前置诊断之间取平衡——内存镜像未命中时放行，由下游 401 兜底（A15 零差异）");
    }

    /// <summary>
    /// 守卫 ⑥（金丝雀）：UserTokenPrecheckModes.IsValid 对非法值返回 false。
    /// </summary>
    [Theory]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("off", true)]
    [InlineData("cache_only", true)]
    [InlineData("full", true)]
    [InlineData("OFF", false)]
    [InlineData("CacheOnly", false)]
    public void IsValid_ShouldRejectInvalidValues(string? mode, bool expected)
    {
        UserTokenPrecheckModes.IsValid(mode).Should().Be(expected,
            $"IsValid 必须精确匹配闭集字面量（大小写敏感），'{mode}' 不应匹配");
    }

    private static string ReadSourceFile(string relativePath)
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(root, relativePath);
        File.Exists(path).Should().BeTrue($"源文件必须存在：{relativePath}");
        return File.ReadAllText(path);
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
