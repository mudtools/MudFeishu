// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;

namespace Mud.Feishu.Webhook.Tests.ContractGuards;

/// <summary>
/// W-02：热更回调"零异常外泄"契约守卫（源码扫描型）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要它</b>：<c>IOptionsMonitor.OnChange</c> 与 <c>System.Threading.Timer</c> 的回调
/// 抛出的异常**不会被框架捕获**——前者沿配置重载通知链上抛到文件监视线程，后者落到线程池，
/// 两者在 .NET Core 上都会<b>终止进程</b>。历史实例：<c>FeishuMultiAppMiddleware</c> 的
/// <c>OnChange</c> 首行读 <c>CurrentValue</c>，于是"把配置热更成非法值"就等于"杀掉进程"。
/// </para>
/// <para>
/// <b>为什么断言"调用形态"而不是"回调体里有 catch"</b>：解析 lambda 体判断有无 catch 既脆弱
/// （多行/嵌套/表达式体）又容易被 <c>#pragma</c> 架空。本仓的纪律已收敛到唯一实现点
/// <c>SafeOptionsChangeExtensions.OnChangeSafe</c>，因此"模块内不得出现裸 <c>.OnChange(</c>"
/// 是**可判定**且无歧义的断言；新回调只要走该扩展点就自动获得护栏。
/// </para>
/// </remarks>
public class HotReloadCallbackContractGuards
{
    private const string ModuleDirectoryName = "Mud.Feishu.Webhook";

    /// <summary>
    /// 模块内所有 <c>.OnChange(</c> 调用点必须经 <c>OnChangeSafe(</c>。
    /// </summary>
    [Fact]
    public void OptionsChangeCallbacks_ShouldAllGoThroughOnChangeSafe()
    {
        var offenders = new List<string>();

        foreach (var (path, source) in ReadAllModuleSources())
        {
            // 扩展实现自身是唯一允许直接调用 OnChange 的位置
            if (path.EndsWith("SafeOptionsChangeExtensions.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var lines = source.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // 跳过注释行（本模块的注释里大量出现 "OnChange(" 这一名词）
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("//", StringComparison.Ordinal) ||
                    trimmed.StartsWith("///", StringComparison.Ordinal) ||
                    trimmed.StartsWith("*", StringComparison.Ordinal))
                    continue;

                if (!line.Contains(".OnChange(", StringComparison.Ordinal))
                    continue;

                // audit-allow: 扩展点自身的实现（文件名已跳过）；此处仅防"同文件内另起裸调用"
                if (line.Contains(".OnChangeSafe(", StringComparison.Ordinal))
                    continue;

                offenders.Add($"{path}:{i + 1}: {trimmed.Trim()}");
            }
        }

        offenders.Should().BeEmpty(
            "配置变更回调必须经 SafeOptionsChangeExtensions.OnChangeSafe 注册——裸 OnChange 的回调体" +
            "一旦抛异常（最常见的来源是回调内读 CurrentValue）会沿配置重载线程传播并终止进程（W-02）。" +
            $"违规点：{string.Join(" | ", offenders)}");
    }

    /// <summary>
    /// 限流中间件不得再引入 <c>System.Threading.Timer</c>（W-02/F-3：改为请求路径上的机会式清扫）。
    /// </summary>
    /// <remarks>
    /// 定时器回调是第二条"异常落到线程池 ⇒ 进程终止"的通道，且它一旦引入就必然带来
    /// "实例必须可 Dispose"的连锁要求（而 <c>UseMiddleware</c> 的管道实例并不由容器释放）。
    /// </remarks>
    [Fact]
    public void RateLimitMiddleware_ShouldNotUseTimer()
    {
        var source = ReadModuleSource("Middleware/FeishuRateLimitMiddleware.cs");

        source.Should().NotContain("new Timer(",
            "限流窗口清理必须走请求路径上的机会式清扫（TrySweepExpiredWindows），不得恢复进程级定时器");
        source.Should().NotContain(": IDisposable",
            "无定时器即无 IDisposable 资源；恢复 IDisposable 通常意味着又引入了需释放的进程级资源");
    }

    /// <summary>
    /// 配置变更回调体内不得读 <c>CurrentValue</c>（那正是"新配置非法时必抛"的路径）。
    /// </summary>
    [Fact]
    public void MiddlewareOnChangeCallback_ShouldNotReadCurrentValue()
    {
        var source = ReadModuleSource("Middleware/FeishuMultiAppMiddleware.cs");

        // 回调块：从 OnChangeSafe 到构造结束；用"快照"替代 CurrentValue 是本条的可判定形态
        source.Should().Contain("Interlocked.Exchange(ref _lastKnownOptions",
            "配置变更回调必须与快照比较（Interlocked 原子换入），而不是回读 CurrentValue");

        var callbackStart = source.IndexOf("OnChangeSafe(", StringComparison.Ordinal);
        callbackStart.Should().BeGreaterThan(0, "中间件必须通过 OnChangeSafe 注册回调");

        var callbackBlock = source.Substring(callbackStart, Math.Min(3000, source.Length - callbackStart));
        var endIndex = callbackBlock.IndexOf("\n    }", StringComparison.Ordinal);
        if (endIndex > 0)
            callbackBlock = callbackBlock.Substring(0, endIndex);

        // 注意必须用词边界：`oldOptions.` / `newOptions.` 是合法的局部快照变量，
        // 只有**属性访问** `Options.`（= _options.CurrentValue）才是被禁止的形态。
        System.Text.RegularExpressions.Regex.IsMatch(callbackBlock, @"(?<![A-Za-z0-9_])Options\.")
            .Should().BeFalse("回调体内不得读 Options（CurrentValue）：热更成非法配置时读它会抛异常并终止进程（W-02）");
    }

    // ────────────────────────────────────────────────────────────────────
    // 源码读取（与 WebSocketContractGuards 同形）
    // ────────────────────────────────────────────────────────────────────

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Mud.Feishu.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"未能从 {AppContext.BaseDirectory} 向上定位仓库根目录（Mud.Feishu.slnx）——守卫无法工作，宁可失败也不要假绿");
    }

    private static string ReadModuleSource(string relativePath)
    {
        var path = Path.Combine(GetRepositoryRoot(), ModuleDirectoryName,
            relativePath.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(path).Should().BeTrue($"模块源码必须存在：{relativePath}");
        return File.ReadAllText(path);
    }

    private static IEnumerable<(string Path, string Source)> ReadAllModuleSources()
    {
        var root = Path.Combine(GetRepositoryRoot(), ModuleDirectoryName);
        var separator = Path.DirectorySeparatorChar;

        return Directory
            .EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{separator}obj{separator}", StringComparison.OrdinalIgnoreCase)
                        && !path.Contains($"{separator}bin{separator}", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => (Path.GetRelativePath(root, path), File.ReadAllText(path)));
    }
}
