// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// DP-B1-0 守卫：<c>page_size</c> 不得作为模型可见参数出现在任何工具的 Schema 中。
/// </summary>
/// <remarks>
/// <para>
/// <b>背景</b>：<c>page_size</c> 是运维参数（影响限流与延迟），由绑定层 <c>PageSizes</c> 常量钳制。
/// 曾有 13 处泄漏进 Schema（OKR 7 / VC 3 / IM 3），导致模型可设置页大小——
/// 模型设 0/10000 会触发 400 或超时。本守卫机械断言该参数已从全部 Curation 接口中移除。
/// </para>
/// <para>
/// <b>金丝雀</b>：在任意 Curation 文件中加回 <c>[ToolParameter("page_size", ...)]</c> → 本守卫即红。
/// </para>
/// </remarks>
public class PageSizeSchemaLeakContractGuards
{
    /// <summary>
    /// Curation 目录下所有 <c>[FeishuTool]</c> 接口文件中，不得出现 <c>ToolParameter("page_size"</c>。
    /// </summary>
    [Fact]
    public void Curation_ShouldNotContainPageSizeToolParameter()
    {
        var curationDir = Path.Combine(
            FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "Curation");

        Directory.Exists(curationDir).Should().BeTrue(
            $"Curation 目录应存在于 {curationDir}");

        var leaks = new List<string>();
        foreach (var file in Directory.EnumerateFiles(curationDir, "*.cs", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(file);
            if (content.Contains("[ToolParameter(\"page_size\"", StringComparison.Ordinal))
            {
                leaks.Add(Path.GetRelativePath(FindRepositoryRoot(), file));
            }
        }

        leaks.Should().BeEmpty(
            "page_size 不得作为模型可见参数出现在工具 Schema 中（DP-B1-0 口径统一：页不进、预算进）。" +
            "泄漏文件: " + string.Join(", ", leaks));
    }

    /// <summary>
    /// Golden 快照中不得出现 <c>"page_size"</c> 属性（防生成器回归）。
    /// </summary>
    [Fact]
    public void GoldenSnapshot_ShouldNotContainPageSizeProperty()
    {
        var goldenPath = Path.Combine(
            FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "FeishuToolSchemas.golden.txt");

        File.Exists(goldenPath).Should().BeTrue("golden 快照必须存在");

        var content = File.ReadAllText(goldenPath);
        var matches = Regex.Matches(content, @"""page_size""");

        matches.Count.Should().Be(0,
            "golden 快照中不得出现 page_size 属性——它是运维参数，不应暴露给模型（DP-B1-0）。");
    }

    private static string FindRepositoryRoot()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir is not null)
        {
            if (Directory.GetFiles(dir, "*.slnx").Length > 0 ||
                Directory.GetFiles(dir, "*.sln").Length > 0)
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException("无法找到仓库根目录（缺少 .slnx/.sln）");
    }
}
