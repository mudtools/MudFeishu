// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Mud.Feishu.AI.Tools;

namespace Mud.Feishu.AI.Tools.Tests;

/// <summary>
/// 生成器 driver 测试宿主（WP1 / R-A 根因）：以 <see cref="CSharpGeneratorDriver"/> 真实驱动
/// <see cref="FeishuToolSchemaGenerator"/>，对合成源码断言产出的诊断。
/// </summary>
/// <remarks>
/// <para>
/// <b>密封性</b>：生成器对 <c>[FeishuTool]</c>（按 <c>Mud.Feishu.AI.Tools</c> 命名空间 + 类名判定）、
/// <c>[ToolParameter]</c>、<c>[Post]</c>/<c>[Delete]</c>/<c>[FormContent]</c>（按类名判定）的匹配都是
/// 符号名级的，故合成源码<b>自带</b>这些特性与假 SDK 接口声明——除 BCL 引用外零外部程序集依赖，
/// 无需引用 Mud.Feishu / Mud.HttpUtils 的 bin 输出。
/// </para>
/// <para>
/// <b>反向验证纪律（WP1 的 DoD 核心）</b>：故意把某条诊断的上报条件改成恒假，对应负例必须失败。
/// 负例断言的是"目标 ID 出现在 driver 产出的诊断集中"，而不是"源码里出现过该 ID"。
/// </para>
/// </remarks>
public static class GeneratorDriverHost
{
    /// <summary>驱动生成器（无 AdditionalFiles）并返回运行结果。</summary>
    public static GeneratorRun Run(params string[] sources)
        => RunCore(additionalText: null, sources);

    /// <summary>驱动生成器（带一个 AdditionalFile，用于 MUDFT014 golden 负例）并返回运行结果。</summary>
    public static GeneratorRun Run(AdditionalText? additionalText, params string[] sources)
        => RunCore(additionalText, sources);

    private static GeneratorRun RunCore(AdditionalText? additionalText, string[] sources)
    {
        var parseOptions = new CSharpParseOptions(
            languageVersion: LanguageVersion.CSharp12,
            documentationMode: DocumentationMode.Parse);

        var compilation = CSharpCompilation.Create(
            "GeneratorDriverTests",
            sources.Select(source => CSharpSyntaxTree.ParseText(source, parseOptions)),
            CreateBclReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        // 注意：诊断必须经 RunGenerators + GetRunResult 获取——
        // RunGeneratorsAndUpdateCompilation 的重载在部分流程下不会把生成器诊断带进 GetRunResult()。
        var driver = CSharpGeneratorDriver.Create(
            generators: [new FeishuToolSchemaGenerator().AsSourceGenerator()],
            additionalTexts: additionalText is null
                ? ImmutableArray<AdditionalText>.Empty
                : [additionalText],
            parseOptions: parseOptions);

        var runResult = driver.RunGenerators(compilation).GetRunResult();

        return new GeneratorRun(
            runResult.Diagnostics.ToImmutableArray(),
            runResult.GeneratedTrees.Select(static tree => tree.ToString()).ToImmutableArray());
    }

    /// <summary>
    /// BCL 引用集：生成器产物的编译不是本测试的目的（driver 只要求语法/符号层运行），
    /// 合成源码只需要 <see cref="object"/> / <see cref="Attribute"/> / <see cref="Task"/> 等核心类型。
    /// </summary>
    private static IEnumerable<MetadataReference> CreateBclReferences()
    {
        var coreLib = typeof(object).Assembly.Location;
        yield return MetadataReference.CreateFromFile(coreLib);

        var runtimeDirectory = Path.GetDirectoryName(coreLib)!;
        foreach (var fileName in new[]
                 {
                     "System.Runtime.dll",
                     "System.Runtime.Extensions.dll",
                     "System.Threading.Tasks.dll",
                     "System.Collections.dll",
                     "System.Linq.dll",
                     "System.IO.dll",
                 })
        {
            var path = Path.Combine(runtimeDirectory, fileName);
            if (File.Exists(path))
            {
                yield return MetadataReference.CreateFromFile(path);
            }
        }
    }
}

/// <summary>一次 driver 运行的结果（诊断 + 产出源码文本）。</summary>
public sealed record GeneratorRun(
    ImmutableArray<Diagnostic> Diagnostics,
    ImmutableArray<string> GeneratedSources)
{
    /// <summary>运行结果中出现的全部诊断 ID。</summary>
    public IReadOnlyCollection<string> DiagnosticIds
        => Diagnostics.Select(static d => d.Id).ToArray();

    /// <summary>断言目标零容忍诊断被真实触发。</summary>
    public GeneratorRun ShouldReport(string diagnosticId, string because)
    {
        Diagnostics.Should().Contain(d => d.Id == diagnosticId, because);
        return this;
    }

    /// <summary>断言目标诊断<b>未</b>被触发（对照用）。</summary>
    public GeneratorRun ShouldNotReport(string diagnosticId, string because)
    {
        Diagnostics.Should().NotContain(d => d.Id == diagnosticId, because);
        return this;
    }
}

/// <summary>内存 AdditionalText（MUDFT014 的 golden 负例用）。</summary>
public sealed class InMemoryAdditionalText(string path, string content) : AdditionalText
{
    public override string Path { get; } = path;

    public override SourceText GetText(System.Threading.CancellationToken cancellationToken = default)
        => SourceText.From(content);
}
