// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// 文档位置注册表守卫（<c>Tests/Shared/Docs/FeishuDocs.cs</c>）：登记的每条路径都必须真实存在，
/// 且表内不得有重复/空条目。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要它</b>：注册表把"搬文档"收敛为改一行，但注册表本身会腐化——
/// 文档被移走而注册表没改（守卫静默验证一个不存在的文件）、登记了重复路径、
/// 或新增文档忘了登记（位置重新散落回字面量）。本守卫拦这三种形态。
/// </para>
/// <para>
/// <b>反射而非手写清单</b>：条目经反射读取 <c>FeishuDocs</c> 的 public const string——
/// 新增登记自动进入校验面，不会出现"注册表加了、守卫清单忘了加"的第二真相源。
/// </para>
/// </remarks>
public class FeishuDocsRegistryContractGuards
{
    /// <summary>注册表不为空（空表 = 没有任何文档受位置治理，本条恒绿）。</summary>
    [Fact]
    public void Registry_ShouldNotBeEmpty()
    {
        RegisteredPaths().Should().NotBeEmpty(
            "FeishuDocs 注册表为空——受治理文档的位置失去单一真相源");
    }

    /// <summary>每条登记路径都必须真实存在（文档被移走/改名时唯一需要同步的地方就是注册表）。</summary>
    [Fact]
    public void EveryRegisteredDoc_ShouldExist_AtItsRegisteredPath()
    {
        var root = FindRepositoryRoot();
        var missing = RegisteredPaths()
            .Where(path => !ExistsAt(root, path))
            .ToArray();

        missing.Should().BeEmpty(
            "以下受治理文档未出现在注册表登记的路径上——文档被移动/改名时请同步 "
            + "Tests/Shared/Docs/FeishuDocs.cs（唯一需要改的地方）：{0}",
            string.Join(", ", missing));
    }

    /// <summary>表内不得有重复路径或空/空白条目（重复 = 两个真相源；空条目 = 登记了空气）。</summary>
    [Fact]
    public void Registry_ShouldNotContainDuplicates_OrBlankEntries()
    {
        var paths = RegisteredPaths();

        paths.Should().OnlyContain(path => !string.IsNullOrWhiteSpace(path),
            "注册表不得登记空路径（空条目会让 ExistsAt 判定恒假且无意义）");

        paths.Should().OnlyHaveUniqueItems(
            "同一路径被登记两次 = 位置出现两个真相源，搬文档时必然漏改一处");
    }

    /// <summary>
    /// 判据自证：存在性检查必须真的能区分"存在"与"不存在"——
    /// 否则上面那条在路径判据坏掉时恒绿（假门禁）。
    /// </summary>
    [Fact]
    public void ExistenceCheck_ShouldDistinguishExistingFromMissing_OtherwiseGuardIsFalseGreen()
    {
        var root = FindRepositoryRoot();

        // 已登记的真实文档必须判存在。
        ExistsAt(root, FeishuDocs.ToolErrorContract).Should().BeTrue(
            "已登记且真实存在的文档必须判存在（否则本守卫对全表恒红，属误报）");

        // 合成一个必然不存在的路径，必须判不存在。
        ExistsAt(root, "documents/__definitely_missing_doc__.md").Should().BeFalse(
            "判据必须真的能报出缺失路径（否则上面那条恒绿）");
    }

    // ────────── 实现 ──────────

    /// <summary>反射读取注册表（public const string）——新增登记自动进入校验面。</summary>
    private static string[] RegisteredPaths() => typeof(FeishuDocs)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(static field => field.FieldType == typeof(string))
        .Select(static field => (string?)field.GetValue(null))
        .Where(static value => !string.IsNullOrEmpty(value))
        .Select(static value => value!)
        .ToArray();

    /// <summary>仓库相对路径（<c>/</c> 分隔）在磁盘上是否存在。</summary>
    private static bool ExistsAt(string repositoryRoot, string relativePath)
        => File.Exists(Path.Combine(
            repositoryRoot,
            relativePath.Replace('/', Path.DirectorySeparatorChar)));

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
