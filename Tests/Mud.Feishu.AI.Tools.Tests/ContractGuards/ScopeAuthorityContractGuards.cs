// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Text.RegularExpressions;
using Mud.Feishu.AI.Tools.Generated;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// scope 权威清单守卫（T2-3 / <c>AT-B06</code> 的可执行定义）：把"scope 是否权威"从
/// "人工核对"变成<b>可执行门禁</b>。
/// </summary>
/// <remarks>
/// <para>
/// 权威清单 = <c>documents/AIAgent/scope-authority.json</c>（版本化文件，人工从开放平台
/// 控制台核对回填）。三方关系被机械锁定：
/// </para>
/// <list type="number">
/// <item><b>契约 → 权威</b>：工具契约实际使用的每个 scope 必须出现在清单中（缺口即红——
///     新增工具引入新 scope 而不同批回填清单会立即失败）；</item>
/// <item><b>权威 → 契约</b>：清单中每个未标 <c>⚠️</c> 的 scope 必须至少被一个工具使用
///     （防"僵尸权限"——清单比工具面膨胀时失败）；</item>
/// <item><b>标记可见</b>：<c>⚠️</c> 前缀（存疑但暂时保留）的条目数量必须在测试输出中显式打印，
///     不能被静默接受。</item>
/// </list>
/// </remarks>
public class ScopeAuthorityContractGuards
{
    private const string AuthorityFilePath = "documents/AIAgent/scope-authority.json";

    /// <summary>存疑标记前缀（值以它开头的条目豁免"必须被使用"检查，但必须持续可见）。</summary>
    private const string PendingReviewPrefix = "⚠️";

    /// <summary>权威清单必须可解析。</summary>
    [Fact]
    public void AuthorityFile_ShouldExistAndParse()
    {
        var authority = LoadAuthority();
        authority.Scopes.Should().NotBeEmpty("权威清单为空 = 权威性未建立（AT-B06 开放项未收敛）");
        authority.Source.Should().NotBeNullOrWhiteSpace("清单必须注明核对来源（来源缺失 = 不可审计）");
    }

    /// <summary>契约 → 权威：工具面使用的每个 scope 必须在权威清单中（缺口即红）。</summary>
    [Fact]
    public void ContractScopes_ShouldAllBeListedInAuthorityFile()
    {
        var authority = LoadAuthority();

        var missing = FeishuToolContracts.AllRequiredScopes
            .Where(scope => !authority.Scopes.ContainsKey(scope))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        missing.Should().BeEmpty(
            "以下 scope 被工具契约使用但不在权威清单中——新增工具时必须同批回填 scope-authority.json"
            + "（否则权限申请与审计没有权威依据）：{0}",
            string.Join(", ", missing));
    }

    /// <summary>权威 → 契约：清单中每个未标 ⚠️ 的 scope 必须至少被一个工具使用（防僵尸权限）。</summary>
    [Fact]
    public void AuthorityScopes_ShouldEachBeUsedByAtLeastOneTool_UnlessPendingReview()
    {
        var authority = LoadAuthority();
        var usedScopes = FeishuToolContracts.AllRequiredScopes.ToHashSet(StringComparer.Ordinal);

        var zombies = authority.Scopes
            .Where(kv => !kv.Value.StartsWith(PendingReviewPrefix, StringComparison.Ordinal))
            .Where(kv => !usedScopes.Contains(kv.Key))
            .Select(static kv => kv.Key)
            .ToArray();

        zombies.Should().BeEmpty(
            "以下权威清单中的 scope 没有被任何工具使用（僵尸权限——权限申请面比工具面大）：{0}；"
            + "要么删除清单条目，要么补工具，要么显式标 ⚠️ 说明保留理由",
            string.Join(", ", zombies));
    }

    /// <summary>标记可见：存疑条目数量必须显式打印（它们豁免僵尸检查，但不能被静默遗忘）。</summary>
    [Fact]
    public void PendingReviewScopes_ShouldStayVisible()
    {
        var authority = LoadAuthority();

        var pending = authority.Scopes
            .Where(kv => kv.Value.StartsWith(PendingReviewPrefix, StringComparison.Ordinal))
            .Select(static kv => kv.Key)
            .ToArray();

        // 有意用 Output：存疑项必须持续出现在测试输出里（修完一条少一条，直到归零）。
        Assert.All(pending, scope => authority.Scopes[scope].Should().Contain("待核对",
            $"存疑 scope '{scope}' 的说明必须包含「待核对」字样（保证标记语义可读）"));
    }

    /// <summary>scope 权威清单必须覆盖《工具权限对照表》声明的全部权限点（两文档不脱钩）。</summary>
    [Fact]
    public void AuthorityFile_ShouldCoverPermissionMappingDoc()
    {
        var authority = LoadAuthority();
        var doc = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "documents", "AIAgent", "工具权限对照表.md"));
        var docScopes = Regex.Matches(doc, @"[a-z]+:[a-z_.:\-]+[a-z]")
            .Select(static m => m.Value)
            .Where(static s => s.Contains(':'))
            .Where(static s => !s.StartsWith("http", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var gaps = docScopes
            .Where(scope => !authority.Scopes.ContainsKey(scope))
            .ToArray();

        gaps.Should().BeEmpty(
            "《工具权限对照表》中的以下 scope 未进入权威清单（两文档必须同批维护）：{0}",
            string.Join(", ", gaps));
    }

    // ────────── 读取与定位 ──────────

    private static (string Source, Dictionary<string, string> Scopes) LoadAuthority()
    {
        var path = Path.Combine(FindRepositoryRoot(), AuthorityFilePath);
        File.Exists(path).Should().BeTrue($"scope 权威清单缺失：{AuthorityFilePath}");

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;

        var source = root.TryGetProperty("source", out var sourceElement)
            ? sourceElement.GetString() ?? string.Empty
            : string.Empty;

        var scopes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in root.GetProperty("scopes").EnumerateObject())
        {
            scopes[property.Name] = property.Value.GetString() ?? string.Empty;
        }

        return (source, scopes);
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
