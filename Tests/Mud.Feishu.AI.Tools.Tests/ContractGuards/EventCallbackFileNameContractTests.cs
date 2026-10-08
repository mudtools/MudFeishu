// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// <b>R5 / B-7</b>：事件 DTO 的<b>文件名 == 首个公共类型名</b>守卫（<c>AGENTS.md</c> 文件头规范的自动化）。
/// </summary>
/// <remarks>
/// <para>
/// <b>缺陷背景（B-7）</b>：<c>Mud.Feishu.EventCallback/</c> 下有 <b>7 处</b>文件名与首个公共类型名不一致，
/// 且其中两处<b>文件名互相交叉</b>（<c>ChatMemberUserAddedResult.cs</c> 既是"成员新增"事件的
/// 文件名，又是"成员撤回"/"成员删除"事件的类名）⇒ 按文件名检索必然得到错误结果。
/// 本轮方案评审即被此缺陷误导过一次（一度得出"邮箱事件类命名混乱"的错误推断）。
/// </para>
/// <para>
/// <b>为什么需要自动化守卫</b>：<c>AGENTS.md</c> 的文件头规范早已要求"文件名 == 类型名"，
/// 但它<b>没有任何机械保障</b>，于是 7 处漂移能长期存活。新增事件类型时同样会复现。
/// </para>
/// <para>
/// <b>为什么这条守卫不会误伤</b>：只校验<b>文件名与首个公共类型名</b>，不校验类型命名规范本身
/// （后者由 <c>AGENTS.md</c> 与人工评审负责）；且允许一个文件内含多个类型
/// （如 <c>MailSubscriber</c> 与主类同文件），只取<b>首个</b>公共类型作比对基准。
/// </para>
/// </remarks>
public class EventCallbackFileNameContractTests
{
    private const string EventCallbackDirectory = "Mud.Feishu.EventCallback";

    /// <summary>
    /// 每个事件 DTO 文件的文件名必须等于其<b>首个公共类型名</b>。
    /// </summary>
    [Fact]
    public void EventDtos_FileName_ShouldMatchFirstPublicTypeName()
    {
        var violations = new List<string>();

        foreach (var file in EnumerateEventDtoFiles())
        {
            var expected = Path.GetFileNameWithoutExtension(file);
            var actual = FirstPublicTypeName(file);

            // 文件内没有任何公共类型（纯枚举/常量/record struct 文件）→ 无基准可比，跳过。
            if (actual is null)
            {
                continue;
            }

            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                violations.Add($"{Relative(file)}：文件名 '{expected}'≠ 首个公共类型 '{actual}'");
            }
        }

        violations.Should().BeEmpty(
            "事件 DTO 的文件名必须等于首个公共类型名（AGENTS.md 文件头规范；"
            + "文件名错误会直接误导按名检索——本轮方案评审即被误导过）：{0}",
            string.Join(" | ", violations));
    }

    /// <summary>
    /// 反向自证：扫描器必须真的能看到公共类型 —— 否则"扫不到任何东西"会被误当成全绿。
    /// </summary>
    [Fact]
    public void Scanner_ShouldDetectPublicTypes_OtherwiseTheGuardIsFalseGreen()
    {
        var files = EnumerateEventDtoFiles().ToArray();
        files.Should().NotBeEmpty("未找到任何事件 DTO 文件——扫描路径已失效（假绿）");

        var withType = files.Count(file => FirstPublicTypeName(file) is not null);
        withType.Should().BeGreaterThan(
            0,
            "所有事件 DTO 文件都没解析出公共类型——类型名正则已失效（假绿），请先修守卫");
    }

    // ────────── 扫描实现 ──────────

    /// <summary>枚举事件 DTO 源文件（排除 obj/bin 产物与 <c>Generated/</c> 生成物）。</summary>
    private static IEnumerable<string> EnumerateEventDtoFiles()
    {
        var root = Path.Combine(FindRepositoryRoot(), EventCallbackDirectory.Replace('/', Path.DirectorySeparatorChar));

        return Directory
            .EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(static path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}Generated{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    /// <summary>
    /// 取文件中<b>第一个公共类型</b>的名字（按源码出现顺序），无则返回 <see langword="null"/>。
    /// </summary>
    /// <remarks>
    /// 形如 <c>public class X</c> / <c>public sealed partial class X</c> / <c>public record X</c>。
    /// 刻意<b>不</b>匹配 <c>public static class</c> 与 <c>public enum</c> —— 文件名以主 DTO 类型为准。
    /// </remarks>
    private static string? FirstPublicTypeName(string filePath)
    {
        var match = Regex.Match(
            File.ReadAllText(filePath),
            @"^\s*public\s+(?:sealed\s+|abstract\s+|partial\s+)*(?:class|record(?:\s+class)?)\s+(?<name>\w+)",
            RegexOptions.Multiline);

        return match.Success ? match.Groups["name"].Value : null;
    }

    private static string Relative(string path)
        => path.Replace(FindRepositoryRoot() + Path.DirectorySeparatorChar, string.Empty);

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