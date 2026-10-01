// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.ContractGuards;

/// <summary>
/// R-E1 事件解析单一真源契约守卫（源码扫描模式）
/// </summary>
/// <remarks>
/// R-E1（AD-1/AD-3）：事件 v1.0/v2.0 解析收敛到 <c>FeishuEventDataParser</c> 单一真源，
/// <see cref="EventData.Event"/> 写侧统一为 JSON 原文字符串。本守卫阻止两类回归：
/// ① 通道项目内再出现平行 ParseV2Event/ParseV1Event 实现（曾导致 E-P0-1/E-P0-2）；
/// ② Event 载荷赋值逃逸出「字符串化」契约（曾导致 JsonElement 双轨，E-P1-2）。
/// </remarks>
public class EventParserSingleSourceGuards
{
    private const string SolutionFileName = "Mud.Feishu.slnx";

    private static readonly string[] ScannedModuleDirectories =
    {
        "Mud.Feishu.WebSocket",
        "Mud.Feishu.Webhook",
        "Mud.Feishu.Abstractions",
    };

    [Fact]
    public void ModuleSources_ShouldNotContainParallelEventParseImplementations()
    {
        // E-P0-1/E-P0-2 回归：通道项目内禁止再出现私有的 ParseV2Event/ParseV1Event 实现
        //（标识符级别扫描——方法声明与调用一并禁止，解析必须经 FeishuEventDataParser）
        var forbidden = new[] { "ParseV2Event", "ParseV1Event" };

        foreach (var (path, source) in ReadAllModuleSources())
        {
            var code = StripComments(source);

            foreach (var identifier in forbidden)
            {
                code.Should().NotContain(identifier,
                    $"事件解析必须经 FeishuEventDataParser 单一真源（AD-1），禁止在 {path} 中复活平行实现 " +
                    $"『{identifier}』——该模式曾导致 v1.0 事件字段映射错误/整帧丢弃（E-P0-1/E-P0-2）");
            }
        }
    }

    [Fact]
    public void EventDataEventAssignments_ShouldOnlyAssignRawJsonStringOrNull()
    {
        // E-P1-2 回归：EventData.Event 赋值点仅允许 GetRawText() 结果（字符串化）、
        // url_verification 整包字符串（decryptedJson）或 null——阻止 JsonElement 双轨复活
        var assignmentPattern = new Regex(
            @"\.Event\s*=(?!=)\s*(?<rhs>[^;]+);",
            RegexOptions.Compiled);

        foreach (var (path, source) in ReadAllModuleSources())
        {
            var code = StripComments(source);

            foreach (System.Text.RegularExpressions.Match match in assignmentPattern.Matches(code))
            {
                var rhs = match.Groups["rhs"].Value.Trim();

                var isAllowed =
                    rhs.EndsWith("GetRawText()", StringComparison.Ordinal) ||
                    rhs.Equals("decryptedJson", StringComparison.Ordinal) ||
                    rhs.Equals("null", StringComparison.Ordinal);

                isAllowed.Should().BeTrue(
                    $"{path} 中的 `.Event = {rhs}` 违反 AD-3 写侧字符串化契约——" +
                    "赋值必须是 GetRawText() 结果、url_verification 整包字符串（decryptedJson）或 null");
            }
        }
    }

    // ===== 源码读取辅助 =====

    /// <summary>剥离行注释与块注释（守卫只判定代码本身，文档性提及不算实现）</summary>
    private static string StripComments(string source)
    {
        var blockComments = new Regex(@"/\*.*?\*/", RegexOptions.Compiled | RegexOptions.Singleline);
        var lineComments = new Regex(@"//[^\r\n]*", RegexOptions.Compiled);
        return lineComments.Replace(blockComments.Replace(source, string.Empty), string.Empty);
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"未能从 {AppContext.BaseDirectory} 向上定位仓库根目录（{SolutionFileName}）——守卫无法工作，宁可失败也不要假绿");
    }

    private static IEnumerable<(string Path, string Source)> ReadAllModuleSources()
    {
        var root = GetRepositoryRoot();
        var separator = Path.DirectorySeparatorChar;

        foreach (var moduleDirectory in ScannedModuleDirectories)
        {
            var moduleRoot = Path.Combine(root, moduleDirectory);
            Directory.Exists(moduleRoot).Should().BeTrue($"被扫描的模块目录必须存在：{moduleDirectory}");

            foreach (var path in Directory.EnumerateFiles(moduleRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (path.Contains($"{separator}obj{separator}", StringComparison.OrdinalIgnoreCase) ||
                    path.Contains($"{separator}bin{separator}", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return (Path.GetRelativePath(root, path), File.ReadAllText(path));
            }
        }
    }
}
