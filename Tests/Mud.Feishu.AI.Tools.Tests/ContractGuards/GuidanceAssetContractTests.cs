// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// R5 / F-9（DoD 第①项）：<b>每个 L1 域资产都必须含三个固定小节</b>——
/// <c>## 前置链</c> / <c>## 避坑</c> / <c>## 示例</c>。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么必须是"固定小节"而不是自由散文</b>：guidance 是模型的行为依据。
/// 散文里"前置步骤、哪些坑、怎么用"混在一起时，模型难以定位；而固定小节让
/// ① 编写有统一契约（新增域不会漏项）、② 守卫可机械校验、③ 模型可按需跳读。
/// 对标官方 <c>affordance/*.md</c> 的固定小节集（<c># domain</c> / <c>### Avoid when</c> /
/// <c>### Prerequisites</c> / <c>### Tips</c> / <c>### Examples</c>）。
/// </para>
/// <para>
/// <b>为什么守卫要扫源文件而不是生成产物</b>：生成产物 <c>FeishuToolGuidance.ByDomain</c>
/// 是编译期常量，测试工程引用不到；扫 <c>Guidance/*.md</c> 源文件才能在内容改动当次就报红。
/// </para>
/// </remarks>
public class GuidanceStructureContractTests
{
    /// <summary>必须存在的小节标题（Markdown 二级标题）。</summary>
    private static readonly string[] RequiredSections =
    [
        "## 前置链",
        "## 避坑",
        "## 示例",
    ];

    [Fact]
    public void EveryL1DomainAsset_ShouldHaveTheThreeFixedSections()
    {
        var directory = Path.Combine(
            FindRepositoryRoot(),
            "Mud.Feishu.AI.Tools", "Guidance");

        var files = Directory
            .GetFiles(directory, "*.md", SearchOption.TopDirectoryOnly)
            .OrderBy(static f => f, StringComparer.Ordinal)
            .ToArray();

        files.Should().NotBeEmpty("未扫到任何 Guidance/*.md——路径错了（假绿），请先修守卫");

        var violations = new List<string>();
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            foreach (var section in RequiredSections)
            {
                if (!text.Contains(section, StringComparison.Ordinal))
                {
                    violations.Add(
                        $"{Path.GetFileName(file)} 缺少小节「{section}」"
                        + "（R5/F-9 DoD①：14 个域必须统一含 前置链 / 避坑 / 示例 三节）");
                }
            }

            // 小节必须非空：只写标题不写内容等于没写。
            foreach (var section in RequiredSections)
            {
                var index = text.IndexOf(section, StringComparison.Ordinal);
                if (index < 0)
                {
                    continue;
                }

                var body = text[(index + section.Length)..];
                var next = body.IndexOf("\n## ", StringComparison.Ordinal);
                if (next >= 0)
                {
                    body = body[..next];
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    violations.Add($"{Path.GetFileName(file)} 的小节「{section}」是空的");
                }
            }
        }

        violations.Should().BeEmpty(
            "L1 guidance 必须含三个固定小节且非空：{0}",
            string.Join(" | ", violations));
    }

    /// <summary>反向自证：三个小节标题必须真的能被匹配到，否则上例会因"扫不到"而假绿。</summary>
    [Fact]
    public void Guard_ShouldActuallyMatchTheSectionHeadings()
    {
        var directory = Path.Combine(
            FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "Guidance");

        var sample = File.ReadAllText(Path.Combine(directory, "im.md"));
        foreach (var section in RequiredSections)
        {
            sample.Should().Contain(section, $"样本 im.md 应含「{section}」，判据或标题形态变了");
        }
    }

    /// <summary>
    /// L2 资产同样必须结构化：<c>Guidance/{domain}/{topic}.md</c> 是模型按需拉取的避坑文本，
    /// 无结构会让"该域怎么用"这一问题仍需通读全文。
    /// </summary>
    [Fact]
    public void EveryL2ReferenceAsset_ShouldAlsoBeStructured()
    {
        var root = Path.Combine(FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "Guidance");

        var referenceFiles = Directory.GetFiles(root, "*.md", SearchOption.AllDirectories);
        var references = referenceFiles
            .Where(p => !string.Equals(Path.GetDirectoryName(p), root, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        references.Should().NotBeEmpty(
            "未扫到任何 L2 资产（Guidance/{domain}/{topic}.md）——R5/F-9 的渐进式披露没有素材可读");

        var violations = new List<string>();
        foreach (var file in references)
        {
            var text = File.ReadAllText(file);
            foreach (var section in RequiredSections)
            {
                if (!text.Contains(section, StringComparison.Ordinal))
                {
                    violations.Add($"{Path.GetFileName(file)} 缺少小节「{section}」");
                }
            }
        }

        violations.Should().BeEmpty("L2 资产同样必须含三节：{0}", string.Join(" | ", violations));
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