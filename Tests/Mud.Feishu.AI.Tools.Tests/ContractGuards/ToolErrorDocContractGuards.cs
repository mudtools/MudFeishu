// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// <b>E-4：对外错误契约文档守卫</b>——<c>.docs/AI/工具错误契约.md</c> 的闭集清单必须与代码闭集
/// <b>逐一相等</b>（防文档漂移：这是对外承诺，不是内部笔记）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要它</b>：错误契约的消费者是<b>宿主与模型作者</b>，他们读的是文档而不是源码。
/// 文档少写一个 subtype ⇒ 消费者漏写一条分支；文档多写一个 ⇒ 消费者写了一条永不进入的分支
/// （与"幽灵子类"同罪）。两种漂移都不会被编译发现，故必须机械比对。
/// </para>
/// <para>
/// <b>解析约定</b>（文档侧必须遵守，否则本守卫报红）：
/// 在形如 <c>### Category 闭集</c> / <c>### Subtype 闭集</c> 的小节后，第一个 <c>```text</c>
/// 代码块内<b>每行一个字面量</b>；以 <c>#</c> 开头的行是注释（忽略），空行忽略。
/// </para>
/// </remarks>
public class ToolErrorDocContractGuards
{
    private const string DocRelativePath = ".docs/AI/工具错误契约.md";

    /// <summary>文档必须存在且含两个闭集小节（缺失 = 对外契约面消失，不得降级为跳过）。</summary>
    [Fact]
    public void ContractDocument_ShouldExist_WithBothClosedSetSections()
    {
        var content = ReadDocument();

        content.Should().Contain("Category 闭集", "文档必须含 Category 闭集小节（守卫按标题定位代码块）");
        content.Should().Contain("Subtype 闭集", "文档必须含 Subtype 闭集小节");

        ExtractLiterals(content, "Category 闭集").Should().NotBeEmpty("Category 代码块不得为空（空块会让比对恒真）");
        ExtractLiterals(content, "Subtype 闭集").Should().NotBeEmpty("Subtype 代码块不得为空");
    }

    /// <summary><b>闭集相等</b>：文档列出的 category 字面量集合 == 代码枚举经 <c>CategoryLiteral</c> 映射的集合。</summary>
    [Fact]
    public void DocumentCategories_ShouldEqual_CodeClosedSet()
    {
        var documented = ExtractLiterals(ReadDocument(), "Category 闭集");
        var code = Enum.GetValues<ToolErrorCategory>()
            .Select(FeishuToolBinding.CategoryLiteral)
            .ToArray();

        documented.Should().BeEquivalentTo(
            code,
            "《工具错误契约》的 Category 清单必须与 ToolErrorCategory 闭集逐一相等——"
            + "文档少写⇒消费者漏分支，多写⇒消费者写死分支");
    }

    /// <summary><b>闭集相等</b>：文档列出的 subtype 字面量集合 == <c>ToolErrorSubtype</c> 的全部常量值。</summary>
    [Fact]
    public void DocumentSubtypes_ShouldEqual_CodeClosedSet()
    {
        var documented = ExtractLiterals(ReadDocument(), "Subtype 闭集");
        var code = typeof(ToolErrorSubtype)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(field => (string?)field.GetValue(null))
            .Where(static value => !string.IsNullOrEmpty(value))
            .Select(static value => value!)
            .ToArray();

        code.Should().NotBeEmpty("代码侧闭集不得为空（否则本守卫恒绿）");

        documented.Should().BeEquivalentTo(
            code,
            "《工具错误契约》的 Subtype 清单必须与 ToolErrorSubtype 常量全等；"
            + "刻意不存在的子类（missing_scope 等）只能写在 N/A 表格里，不得混入闭集代码块");
    }

    /// <summary>决策树必须覆盖全部 8 类（防"契约有了、处置指引只写一半"）。</summary>
    [Fact]
    public void DecisionTree_ShouldCoverEveryCategory()
    {
        var content = ReadDocument();

        foreach (var category in Enum.GetValues<ToolErrorCategory>().Select(FeishuToolBinding.CategoryLiteral))
        {
            content.Should().Contain(
                $"category = {category}",
                $"消费者决策树必须给出 {category} 的处置（缺一个类别 = 该类错误没有消费指引）");
        }
    }

    /// <summary>解析器自证：注释行被忽略、空块被识别（否则上面的"相等"可能因解析坏掉而假绿）。</summary>
    [Fact]
    public void Extractor_ShouldIgnoreComments_AndDetectEmptyBlock()
    {
        const string sample = """
            ### Category 闭集

            ```text
            # 这是注释，不是字面量
            validation

            policy
            ```
            """;

        ExtractLiterals(sample, "Category 闭集").Should().Equal(["validation", "policy"]);

        const string emptyBlock = """
            ### Category 闭集

            ```text
            # 只有注释
            ```
            """;

        ExtractLiterals(emptyBlock, "Category 闭集").Should().BeEmpty("只有注释 ⇒ 空集合（上层用例据此报红）");

        ExtractLiterals("没有代码块", "Category 闭集").Should().BeEmpty();

        // 标题锚定自证：正文里提到过同名字样、但**没有** `### ` 小节时不得命中。
        const string mentionOnly = """
            本文的 Category 闭集清单见下文。

            ```text
            not_a_subtype
            ```
            """;

        ExtractLiterals(mentionOnly, "Category 闭集").Should().BeEmpty(
            "只有 `### ` 小节标题才算定位锚点——正文提及同名字样不得让解析器跑偏");
    }

    // ────────── 实现 ──────────

    /// <summary>
    /// 取指定小节后的第一个 <c>```text</c> 代码块内的字面量（忽略空行与 <c>#</c> 注释行）。
    /// </summary>
    /// <param name="content">文档全文。</param>
    /// <param name="sectionTitle">小节标题片段（如 <c>Category 闭集</c>）。</param>
    private static string[] ExtractLiterals(string content, string sectionTitle)
    {
        // 锚定**小节标题**（`### …`）而不是裸片段：文档头部也提到过"Category / Subtype 闭集清单"，
        // 裸片段会命中那里并把第一节的示例代码块当成闭集清单（本守卫的首版就这样假红过）。
        var sectionIndex = content.IndexOf("### " + sectionTitle, StringComparison.Ordinal);
        if (sectionIndex < 0)
        {
            return [];
        }

        var fence = content.IndexOf("```text", sectionIndex, StringComparison.Ordinal);
        if (fence < 0)
        {
            return [];
        }

        var bodyStart = content.IndexOf('\n', fence);
        if (bodyStart < 0)
        {
            return [];
        }

        var fenceEnd = content.IndexOf("```", bodyStart, StringComparison.Ordinal);
        if (fenceEnd < 0)
        {
            return [];
        }

        var body = content.Substring(bodyStart, fenceEnd - bodyStart);

        return body
            .Replace("\r\n", "\n")
            .Split('\n')
            .Select(static line => line.Trim())
            .Where(static line => line.Length > 0 && !line.StartsWith('#'))
            .ToArray();
    }

    private static string ReadDocument()
    {
        var path = Path.Combine(FindRepositoryRoot(), DocRelativePath.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(path).Should().BeTrue(
            $"对外错误契约文档必须存在：{DocRelativePath}（宿主与模型作者读的是它，不是源码）");

        return File.ReadAllText(path);
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
