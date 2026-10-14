// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.ToolSurface.Extraction;

namespace Mud.AI.Generator.Tests;

/// <summary>
/// <see cref="Extractors"/> 文档注释提取的双途径测试：语义途径（GetDocumentationCommentXml）
/// 与语法树回退（DocumentationMode.None 时 csc 不解析文档注释 → 语义途径恒空）。
/// </summary>
/// <remarks>
/// 回归背景：消费方未开启 <c>GenerateDocumentationFile</c>（如本仓库 Demos）时，
/// 槽位 005/006 曾全部误报——文档注释在语法树里，但语义模型不携带。
/// </remarks>
public class ExtractorsDocCommentTests
{
    private const string Source = """
        namespace Probe;

        public interface IDoc
        {
            /// <summary>列出文档（方法级 summary）。</summary>
            /// <param name="folderId">文件夹 ID（方法级 param）。</param>
            System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<string>> ListAsync(
                string? folderId, System.Threading.CancellationToken cancellationToken = default);

            /// <summary>多行 summary：
            /// 第二行内容。</summary>
            /// <param name="docId">文档 ID。</param>
            System.Threading.Tasks.Task<string> DeleteAsync(
                string docId, System.Threading.CancellationToken cancellationToken = default);

            System.Threading.Tasks.Task<string> GetAsync(
                string id, System.Threading.CancellationToken cancellationToken = default);
        }
        """;

    [Fact]
    public void DocumentationModeNone_SyntaxFallback_ExtractsSummaryAndParam()
    {
        var method = FindMethod(DocumentationMode.None, "ListAsync");

        Extractors.GetDocSummary(method).Should().Be("列出文档（方法级 summary）。", "DocumentationMode.None 时语法树回退必须生效");
        Extractors.GetParamDoc(method, "folderId").Should().Be("文件夹 ID（方法级 param）。");
    }

    [Fact]
    public void DocumentationModeNone_SyntaxFallback_JoinsMultilineSummary()
    {
        var method = FindMethod(DocumentationMode.None, "DeleteAsync");

        Extractors.GetDocSummary(method).Should().Be("多行 summary： 第二行内容。", "多行 /// 注释拼接后按既有空白归一规则折叠");
        Extractors.GetParamDoc(method, "docId").Should().Be("文档 ID。");
    }

    [Fact]
    public void DocumentationModeNone_NoDocComment_ReturnsNull_NoFalsePositive()
    {
        var method = FindMethod(DocumentationMode.None, "GetAsync");

        Extractors.GetDocSummary(method).Should().BeNull("真缺失时回退不得凭空产出");
        Extractors.GetParamDoc(method, "id").Should().BeNull();
    }

    [Fact]
    public void DocumentationModeParse_SemanticPath_Unchanged()
    {
        var method = FindMethod(DocumentationMode.Parse, "ListAsync");

        Extractors.GetDocSummary(method).Should().Be("列出文档（方法级 summary）。", "语义途径（Parse 模式）行为不得被回退改动");
        Extractors.GetParamDoc(method, "folderId").Should().Be("文件夹 ID（方法级 param）。");
    }

    private static IMethodSymbol FindMethod(DocumentationMode documentationMode, string methodName)
    {
        // DocumentationMode 挂在 ParseOptions（与 csc 行为一致：无 /doc → None，语义模型不解析文档注释）。
        var tree = CSharpSyntaxTree.ParseText(Source, new CSharpParseOptions(LanguageVersion.Latest, documentationMode: documentationMode));
        var compilation = CSharpCompilation.Create(
            "ExtractorsDocCommentProbe",
            new[] { tree },
            BasicReferenceAssemblies.GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return compilation.GetTypeByMetadataName("Probe.IDoc")!
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Single(m => m.Name == methodName);
    }
}