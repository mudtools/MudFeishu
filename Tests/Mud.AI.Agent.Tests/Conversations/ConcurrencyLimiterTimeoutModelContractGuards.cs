// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// 任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Tests.Conversations;

/// <summary>
/// 削峰引擎的<b>超时建模</b>哨兵：超时来源必须由 API 形状显式给出，
/// 不得退化为「<c>CancelAfter</c> ＋ 取消标志反推」的隐式建模。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么必须有</b>：旧实现的队列超时经 <c>CancellationTokenSource.CancelAfter</c> 触发，
/// 再用「外部令牌是否已取消」反推来源。两条路径因此共用同一个异常，靠标志区分——一旦两者
/// <b>并发</b>触发（外部取消恰好落在超时点附近），超时诉求会被取消路径静默吞掉，且观测者无法
/// 从异常本身分辨。这不是缺陷（现实现行为正确），而是<b>建模脆弱</b>：回潮后没有任何信号。
/// </para>
/// <para>
/// <b>判据</b>：引擎源码（剔注释后）不得出现 <c>CancelAfter</c>；也不得用 <c>Task.Delay</c> 自行
/// 造计时（<c>WhenAny</c> 抛下未完成的等待任务会带来"许可被无人认领地取走"的泄漏面）。
/// 行为侧的分界由同目录 <c>ConcurrencyLimiterTests</c> 的四条路径用例承担。
/// </para>
/// </remarks>
public class ConcurrencyLimiterTimeoutModelContractGuards
{
    /// <summary>隐式建模词汇（引擎源码出现即意味着超时来源又回到"靠标志反推"）。</summary>
    private static readonly string[] ImplicitModelTokens = ["CancelAfter", "Task.Delay"];

    /// <summary>引擎的超时建模必须是显式的。</summary>
    [Fact]
    public void ConcurrencyLimiter_ShouldModelTimeoutExplicitly()
    {
        var root = FindRepositoryRoot();
        var enginePath = Path.Combine(root, "Mud.AI.Agent", "Conversations", "ConcurrencyLimiter.cs");
        File.Exists(enginePath).Should().BeTrue($"引擎文件必须存在（否则本守卫退化为空扫描 → 假绿）：{enginePath}");

        var body = StripComments(File.ReadAllText(enginePath));
        body.Should().NotBeEmpty("剔除注释后源码不得为空（否则判据失效）");

        var offenders = ImplicitModelTokens
            .Where(token => body.Contains(token, StringComparison.Ordinal))
            .ToArray();

        offenders.Should().BeEmpty(
            "队列超时必须由 SemaphoreSlim 的超时等待重载显式表达（false 即超时）；"
            + "CancelAfter + 取消标志反推无法区分外部取消与队列超时，并发触发时会静默吞掉超时诉求。命中：{0}",
            string.Join(" | ", offenders));
    }

    /// <summary>判据自证：隐式建模命中、显式超时等待放行、注释豁免。</summary>
    [Theory]
    [InlineData("cts.CancelAfter(timeout);", true)]
    [InlineData("await Task.Delay(timeout, ct);", true)]
    [InlineData("await _semaphore.WaitAsync(timeout, ct);", false)]
    [InlineData("await _semaphore.WaitAsync(ct);", false)]
    [InlineData("// 旧实现曾用 CancelAfter 反推来源（注释是记录）", false)]
    public void ImplicitModelJudge_ShouldIgnoreComments_AndDetectFlagInference(string line, bool expected)
    {
        var body = StripComments(line);
        var detected = ImplicitModelTokens.Any(token => body.Contains(token, StringComparison.Ordinal));
        detected.Should().Be(expected);
    }

    /// <summary>剔除整行注释与行尾注释（注释是记录，不是声明）。</summary>
    private static string StripComments(string source)
    {
        var kept = new List<string>();
        foreach (var line in source.Split('\n'))
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            var commentAt = line.IndexOf("//", StringComparison.Ordinal);
            kept.Add(commentAt >= 0 ? line[..commentAt] : line);
        }

        return string.Join("\n", kept);
    }

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
