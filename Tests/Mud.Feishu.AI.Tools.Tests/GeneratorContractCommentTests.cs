// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace Mud.Feishu.AI.Tools.Tests;

/// <summary>
/// §2.2「测试即契约」守卫：生成器源码<b>注释里引用的测试类型必须真实存在</b>。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要它</b>：R4-2 的原始缺陷不是代码错，而是
/// <c>FeishuToolSchemaGenerator</c> 的注释<b>声明</b>"以 GeneratorDriver 用例锁定行为（见
/// <c>GeneratorIncrementalBehaviorTests</c>）"——而该测试类**在仓库中从未存在**。
/// 一条"已锁定的安全声明"被写进源码注释，读者会据此认为该风险已被覆盖。
/// </para>
/// <para>
/// <b>判据为什么是"源码注释里的 `*Tests` 标识符"</b>：这是唯一可机械校验的形式——
/// 注释没有类型系统，只有<b>名字</b>；把名字与真实类名对齐即可消除"声明大于事实"。
/// 生成器源码引用测试类型时必须写真实类名，本就是本仓库既有体例（如
/// <c>GeneratorOutputGuardContractGuards</c> 对应的守卫用例）。
/// </para>
/// </remarks>
public class GeneratorContractCommentTests
{
    /// <summary>生成器源码注释中引用的每一个 <c>*Tests</c> 类都必须在测试工程内真实存在。</summary>
    [Fact]
    public void GeneratorComments_ReferencedTestTypes_ShouldExist()
    {
        var root = FindRepositoryRoot();
        var generatorDirectory = Path.Combine(root, "Mud.Feishu.AI.Tools");
        var testDirectory = Path.Combine(root, "Tests", "Mud.Feishu.AI.Tools.Tests");

        var referenced = Directory
            .EnumerateFiles(generatorDirectory, "*.cs", SearchOption.AllDirectories)
            .SelectMany(file => Regex
                .Matches(File.ReadAllText(file), @"\b(?<name>[A-Z][A-Za-z0-9]*Tests)\b")
                .Select(static match => match.Groups["name"].Value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        referenced.Should().NotBeEmpty(
            "生成器必须至少引用一个锁定用例——本守卫若无可校验对象即形同虚设（R4-2 的原缺陷正是零对象）");

        var testSources = string.Join(
            "\n",
            Directory.EnumerateFiles(testDirectory, "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        foreach (var typeName in referenced)
        {
            testSources.Should().Contain(
                "class " + typeName,
                $"生成器源码引用了 {typeName}，它必须真实存在（否则就是 R4-2 那类『声明大于事实』）");
        }
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
