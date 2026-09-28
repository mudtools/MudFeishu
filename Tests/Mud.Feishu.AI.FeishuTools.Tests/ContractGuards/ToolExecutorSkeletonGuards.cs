// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.FeishuTools.Tests.ContractGuards;

/// <summary>
/// 执行器骨架收敛守卫（WP3 / R-C 根因）：机械断言"机械骨架只存在于
/// <see cref="ToolExecutor"/> 一处"，防止扩域时样板回潮。
/// </summary>
/// <remarks>
/// <para>
/// 三条断言（源码扫描体例，与本目录既有守卫一致）：
/// ① <c>if (!outcome.Ok)</c> 在执行器目录（除 ToolExecutor 自身）计数 = 0——解包/回填骨架收拢于
/// <c>FromApi*</c>/<c>FromPlainText</c>；
/// ② <c>catch (ArgumentException</c> 计数 = 0——参数校验失败回填收拢于 <c>RunAsync</c>；
/// ③ 每个执行器方法内 <c>FeishuToolNames.X</c> 出现 ≤ 1 次（工具名经
/// <c>new ToolExecutor(FeishuToolNames.X, …)</c> 绑定一次，体内用 <c>executor.ToolName</c>）。
/// </para>
/// <para>
/// <b>有意保留</b>（不在断言范围）：WriteTools 的两处 <c>catch (JsonException)</c> 是
/// 裸 JSON 参数的<b>校验逻辑</b>（转带修复指引的 ArgumentException），属策展面而非骨架。
/// </para>
/// </remarks>
public class ToolExecutorSkeletonGuards
{
    private const string ExecutorDirectory = "Mud.Feishu.AI.FeishuTools/Internal";

    /// <summary>解包/回填骨架不得回潮到具体执行器。</summary>
    [Fact]
    public void OutcomeUnwrapSkeleton_ShouldLiveOnlyInToolExecutor()
    {
        var violations = ExecutorSources()
            .Where(kv => Regex.IsMatch(kv.Value, @"if\s*\(\s*!outcome\.Ok\s*\)"))
            .Select(static kv => kv.Key)
            .ToArray();

        violations.Should().BeEmpty(
            "以下执行器出现了 if (!outcome.Ok) 手写骨架——解包/回填/截断应经 ToolExecutor.FromApi*/FromPlainText（WP3 骨架收敛）：{0}",
            string.Join(", ", violations));
    }

    /// <summary>参数校验失败回填骨架不得回潮到具体执行器。</summary>
    [Fact]
    public void ArgumentValidationCatch_ShouldLiveOnlyInToolExecutor()
    {
        var violations = ExecutorSources()
            .Where(kv => kv.Value.Contains("catch (ArgumentException", StringComparison.Ordinal))
            .Select(static kv => kv.Key)
            .ToArray();

        violations.Should().BeEmpty(
            "以下执行器出现了 catch (ArgumentException) 手写骨架——参数校验失败回填应经 ToolExecutor.RunAsync（WP3 骨架收敛）：{0}",
            string.Join(", ", violations));
    }

    /// <summary>同一工具名在单个执行器方法内最多出现 1 次（其余经 executor.ToolName）。</summary>
    [Fact]
    public void ToolName_ShouldAppearAtMostOncePerExecutorMethod()
    {
        var violations = new List<string>();
        foreach (var (fileName, source) in ExecutorSources())
        {
            var currentMethod = string.Empty;
            var count = 0;
            foreach (var line in File.ReadLines(fileName))
            {
                var methodMatch = Regex.Match(line, @"public\s+(?:async\s+)?Task<FeishuToolResult>\s+(\w+)");
                if (methodMatch.Success)
                {
                    if (count > 1)
                    {
                        violations.Add($"{Path.GetFileName(fileName)}::{currentMethod}（{count} 处）");
                    }

                    currentMethod = methodMatch.Groups[1].Value;
                    count = 0;
                }

                if (line.Contains("FeishuToolNames.", StringComparison.Ordinal))
                {
                    count++;
                }
            }

            if (count > 1)
            {
                violations.Add($"{Path.GetFileName(fileName)}::{currentMethod}（{count} 处）");
            }
        }

        violations.Should().BeEmpty(
            "以下执行器方法内 FeishuToolNames.X 出现超过 1 次——工具名应经 new ToolExecutor(FeishuToolNames.X, …) 绑定一次，体内用 executor.ToolName（防漂移面）：{0}",
            string.Join(", ", violations));
    }

    /// <summary>读取执行器目录全部源码（排除 ToolExecutor 自身与 obj/bin 产物）。</summary>
    private static Dictionary<string, string> ExecutorSources()
    {
        var directory = Path.Combine(FindRepositoryRoot(), ExecutorDirectory.Replace('/', Path.DirectorySeparatorChar));
        return Directory.GetFiles(directory, "*Tools.cs", SearchOption.TopDirectoryOnly)
            .Where(static path => !path.EndsWith("ToolExecutor.cs", StringComparison.Ordinal))
            .ToDictionary(static p => p, File.ReadAllText, StringComparer.Ordinal);
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
