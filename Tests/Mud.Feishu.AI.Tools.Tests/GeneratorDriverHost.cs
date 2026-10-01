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
    /// <summary>默认宿主程序集名（非工具面实现包 → 名字契约表 / 契约表 / 参数解包器等 owner-gated 产物不发射）。</summary>
    public const string DefaultAssemblyName = "GeneratorDriverTests";

    /// <summary>工具面实现包程序集名（owner-gated 产物的发射门槛；与生成器的 <c>ToolNamesOwnerAssembly</c> 一致）。</summary>
    public const string OwnerAssemblyName = "Mud.Feishu.AI.FeishuTools";

    /// <summary>驱动生成器（无 AdditionalFiles）并返回运行结果。</summary>
    public static GeneratorRun Run(params string[] sources)
        => RunCore(additionalText: null, sources);

    /// <summary>驱动生成器（带一个 AdditionalFile，用于 MUDFT014 golden 负例）并返回运行结果。</summary>
    public static GeneratorRun Run(AdditionalText? additionalText, params string[] sources)
        => RunCore(additionalText, sources);

    /// <summary>
    /// 以<b>工具面实现包</b>的程序集名驱动（参数解包器与工具名/契约表同款 owner 门槛）。
    /// </summary>
    /// <remarks>
    /// 参数解包产物消费 FeishuTools 的 <c>internal</c> 成员，故只在实现包内发射——与其相关的诊断
    /// （<c>MUDFT020</c>/<c>MUDFT021</c>）必须在本门槛下才能被触发。合成源码无需真的存在
    /// <c>ToolArgs</c>：本宿主只收集<b>生成器诊断</b>，不编译产物（见下方注释）。
    /// </remarks>
    public static GeneratorRun RunAsOwnerAssembly(params string[] sources)
        => RunCore(additionalText: null, sources, OwnerAssemblyName);

    private static GeneratorRun RunCore(
        AdditionalText? additionalText,
        string[] sources,
        string assemblyName = DefaultAssemblyName)
    {
        var compilation = CreateCompilation(ParseTrees(sources), assemblyName);
        var driver = CreateDriver(additionalText);

        // 注意：诊断必须经 RunGenerators + GetRunResult 获取——
        // RunGeneratorsAndUpdateCompilation 的重载在部分流程下不会把生成器诊断带进 GetRunResult()。
        var runResult = driver.RunGenerators(compilation).GetRunResult();

        return new GeneratorRun(
            runResult.Diagnostics.ToImmutableArray(),
            runResult.GeneratedTrees.Select(static tree => tree.ToString()).ToImmutableArray());
    }

    // ────────── 增量行为用例原语（R4-2） ──────────
    //
    // 增量语义只有"同一 driver 连续驱动多份 compilation"才能观察到；每次新建 driver 会重建全部
    // 缓存，用例恒绿（假绿）。故把 driver / 语法树实例的构造拆出来供增量用例复用。

    /// <summary>解析选项（各路径共用；文档模式为 Parse——生成器只读 XML 注释文本）。</summary>
    public static CSharpParseOptions ParseOptions { get; } = new(
        languageVersion: LanguageVersion.CSharp12,
        documentationMode: DocumentationMode.Parse);

    /// <summary>
    /// 把合成源码解析为<b>保留实例</b>的语法树数组——增量用例须以
    /// <see cref="CSharpCompilation.ReplaceSyntaxTree"/> 只替换改动的那一棵，未改动的树保持同实例，
    /// 否则整编译失效、用例退化为恒绿。
    /// </summary>
    public static ImmutableArray<SyntaxTree> ParseTrees(IEnumerable<string> sources)
        => sources.Select(source => CSharpSyntaxTree.ParseText(source, ParseOptions)).ToImmutableArray();

    /// <summary>以给定语法树构造编译（增量用例用）。</summary>
    public static CSharpCompilation CreateCompilation(
        ImmutableArray<SyntaxTree> trees,
        string assemblyName = DefaultAssemblyName)
        => CSharpCompilation.Create(
            assemblyName,
            trees,
            CreateBclReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

    /// <summary>创建可复用的增量 driver（增量用例用）。</summary>
    /// <param name="additionalText">附加文件（如 golden 快照）；默认无。</param>
    /// <param name="trackSteps">
    /// 是否追踪增量步骤（<see cref="RunOnceTracked"/> 需要；默认关闭以免无谓开销）。
    /// </param>
    public static GeneratorDriver CreateDriver(AdditionalText? additionalText = null, bool trackSteps = false)
        => CSharpGeneratorDriver.Create(
            generators: [new FeishuToolSchemaGenerator().AsSourceGenerator()],
            additionalTexts: additionalText is null
                ? ImmutableArray<AdditionalText>.Empty
                : [additionalText],
            parseOptions: ParseOptions,
            driverOptions: new GeneratorDriverOptions(
                IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: trackSteps));

    /// <summary>
    /// 以既有 driver 驱动一次编译，返回<b>带缓存状态的新 driver</b> 与本次运行结果
    /// （<see cref="GeneratorDriver"/> 不可变，须回填返回值才能延续缓存）。
    /// </summary>
    public static (GeneratorDriver Driver, GeneratorRun Run) RunOnce(GeneratorDriver driver, Compilation compilation)
    {
        var updated = driver.RunGenerators(compilation);
        var runResult = updated.GetRunResult();

        return (updated, new GeneratorRun(
            runResult.Diagnostics.ToImmutableArray(),
            runResult.GeneratedTrees.Select(static tree => tree.ToString()).ToImmutableArray()));
    }

    /// <summary>
    /// 同 <see cref="RunOnce"/>，另返回<b>增量步骤轨迹</b>（须以
    /// <c>CreateDriver(trackSteps: true)</c> 创建 driver；否则轨迹为空）。
    /// </summary>
    /// <remarks>
    /// 缓存判定依据是每步输出的 <see cref="IncrementalStepRunReason"/>：<c>Cached</c> 即复用上趟结果。
    /// 只看"产出文本相同"不足以证明缓存命中（全部重跑也会得到相同文本）。
    /// </remarks>
    public static (GeneratorDriver Driver, GeneratorRun Run, ImmutableArray<IncrementalGeneratorRunStep> Steps) RunOnceTracked(
        GeneratorDriver driver,
        Compilation compilation)
    {
        var updated = driver.RunGenerators(compilation);
        var runResult = updated.GetRunResult();

        var steps = runResult.Results.IsDefaultOrEmpty
            ? ImmutableArray<IncrementalGeneratorRunStep>.Empty
            : runResult.Results.SelectMany(static result => result.TrackedSteps.Values.SelectMany(static v => v))
                .ToImmutableArray();

        return (updated, new GeneratorRun(
            runResult.Diagnostics.ToImmutableArray(),
            runResult.GeneratedTrees.Select(static tree => tree.ToString()).ToImmutableArray()), steps);
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
