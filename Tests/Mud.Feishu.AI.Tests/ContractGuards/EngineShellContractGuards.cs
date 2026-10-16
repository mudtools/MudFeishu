// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tests.ContractGuards;

/// <summary>
/// 「三段分离」的壳段守卫（第七式）：产品薄壳只做契约适配，不得重新长出引擎语义
/// （信号量 / 并发字典 / 等待与回收实现）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么必须机械锁定</b>：三段分离（引擎 / 适配 / 壳）的价值在于"引擎有引擎的用例、壳只有壳的行数"。
/// 但退化方向是<b>单向</b>的：壳里顺手写回一行 <c>_entries.TryAdd</c> 或 <c>WaitAsync</c>，
/// 编译通过、行为看上去也对，机械语义却重新分裂成两份（此后修引擎漏改壳）——而<b>没有任何编译期或运行期信号</b>。
/// </para>
/// <para>
/// <b>判据</b>：断言壳源码不含引擎词汇且真出现引擎类型名（委托而非重写）。
/// </para>
/// <para>
/// <b>迁移拆分</b>：源守卫是跨仓事实（壳 ↔ 引擎配对）。迁移后引擎段（第（b）条判据：引擎必须住在中立
/// 工程目录、且不含产品词汇）随 <c>Mud.AI.Agent</c> 迁出，由新仓 <c>EngineContractGuards</c> 持有；
/// 本文件只保留<b>壳侧</b>断言。事实表 <see cref="ShellEngineFacts"/> 仍记录跨仓配对，供壳侧定位引擎类型名。
/// </para>
/// </remarks>
public class EngineShellContractGuards
{
    /// <summary>引擎词汇（壳里出现即意味着壳在自持实现，而不是委托）。</summary>
    private static readonly string[] EngineVocabulary =
    [
        "SemaphoreSlim", "ConcurrentDictionary", "Interlocked.Exchange", "WaitAsync(", "Task.Delay",
    ];

    /// <summary>产品词汇（引擎里出现即意味着中立引擎被拉回产品契约 —— P3 上游化的反向破坏）。</summary>
    /// <remarks>
    /// <c>Mud.Feishu</c> 是程序集/命名空间前缀：它一旦出现在引擎源码（注释除外），
    /// 就意味着引擎引用了产品类型，中立性不再成立。
    /// </remarks>
    private static readonly string[] ProductVocabulary =
    [
        "IConversationGate", "ConversationBusyException", "BusyModes", "Mud.Feishu",
    ];

    /// <summary>壳 ↔ 引擎事实表（阶段 A-3 起；阶段 B 追加 GlobalConcurrencyGate ↔ ConcurrencyLimiter 行）。</summary>
    private static readonly (string Shell, string Engine, string EngineType)[] ShellEngineFacts =
    [
        (
            Path.Combine("Mud.Feishu.AI", "Conversations", "KeyedConversationGate.cs"),
            Path.Combine("Mud.AI.Agent", "Conversations", "KeyedAsyncGate.cs"),
            "KeyedAsyncGate"),
        (
            Path.Combine("Mud.Feishu.AI", "Conversations", "GlobalConcurrencyGate.cs"),
            Path.Combine("Mud.AI.Agent", "Conversations", "ConcurrencyLimiter.cs"),
            "ConcurrencyLimiter"),
    ];

    [Fact]
    public void Shell_ShouldDelegateToEngine_WithoutReimplementingEngineSemantics()
    {
        var root = FindRepositoryRoot();
        var offenders = new List<string>();
        var scannedShells = 0;

        foreach (var (shell, _, engineType) in ShellEngineFacts)
        {
            var shellPath = Path.Combine(root, shell);
            File.Exists(shellPath).Should().BeTrue($"壳文件必须存在（否则本守卫退化为空扫描 → 假绿）：{shell}");

            scannedShells++;
            var shellSource = File.ReadAllText(shellPath);
            if (!shellSource.Contains(engineType, StringComparison.Ordinal))
            {
                offenders.Add($"{shell} 未出现引擎类型名 {engineType}（壳必须委托引擎）");
            }

            foreach (var token in FindVocabularyOffenders(shellSource, EngineVocabulary))
            {
                offenders.Add($"{shell} 含引擎词汇「{token}」（壳里自持引擎语义 ⇒ 三段分离失效）");
            }
        }

        scannedShells.Should().BePositive("扫描面为空是假绿（事实表为空 ⇒ 恒真）");
        offenders.Should().BeEmpty(
            "产品薄壳只做契约适配：串行/并发语义必须留在中立引擎内（否则修引擎漏改壳，语义重新分裂）。命中：{0}",
            string.Join(" | ", offenders));
    }

    /// <summary>判据自证：两个方向的词汇判定各自独立（剔注释后匹配）。</summary>
    /// <param name="line">待判行。</param>
    /// <param name="engineInShell">期望「壳里含引擎词汇」。</param>
    /// <param name="productInEngine">期望「引擎里含产品词汇」。</param>
    [Theory]
    [InlineData("private readonly SemaphoreSlim _gate = new(1, 1);", true, false)]
    [InlineData("return await _engine.AcquireAsync(key, ct);", false, false)]
    [InlineData("// 串行语义在 SemaphoreSlim 引擎内（注释是记录）", false, false)]
    [InlineData("public sealed class X : IConversationGate", false, true)]
    [InlineData("throw new ConversationBusyException(key, msg);", false, true)]
    [InlineData("using Mud.Feishu.Abstractions.Conversations;", false, true)]
    [InlineData("// Mud.Feishu 只在文件头注释里（注释是记录）", false, false)]
    [InlineData("var lease = await limit.WaitAsync(timeout, ct);", true, false)]
    public void VocabularyJudges_ShouldIgnoreComments_AndDetectDirectionalViolations(
        string line, bool engineInShell, bool productInEngine)
    {
        FindVocabularyOffenders(line, EngineVocabulary).Length.Should().Be(engineInShell ? 1 : 0);
        FindVocabularyOffenders(line, ProductVocabulary).Length.Should().Be(productInEngine ? 1 : 0);
    }

    /// <summary>判据：源码剔除注释后含任一词汇 token 即命中（注释是记录，不是声明）。</summary>
    private static string[] FindVocabularyOffenders(string source, string[] vocabulary)
        => vocabulary
            .Where(token => StripComments(source).Contains(token, StringComparison.Ordinal))
            .ToArray();

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
