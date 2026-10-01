// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Mud.Feishu.AI.Tools.Tests;

/// <summary>
/// R4-2：生成器<b>增量行为</b>用例——把源码注释里"已知限制"（Tier C 的 <c>ScanTool</c> 在语法变换内读
/// <c>context.SemanticModel.Compilation</c>）从**声明**变为**可验证事实**。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么必须复用同一 driver</b>：增量语义只有"同一 <see cref="CSharpGeneratorDriver"/> 连续驱动
/// 多份 compilation"才能观察到。每次新建 driver 会重建全部缓存，无论生成器是否真有陈旧缺陷，
/// 用例都恒绿——那是假绿。故本类的 driver 与语法树实例均由
/// <see cref="GeneratorDriverHost"/> 的增量原语构造。
/// </para>
/// <para>
/// <b>为什么用 <see cref="CSharpCompilation.ReplaceSyntaxTree"/></b>：只替换被编辑的那一棵树，
/// 未改动的树保持<b>同实例</b>——这正是 IDE 增量构建的真实形态；若整编译重建（全部新树），
/// 所有步骤都会因输入变更而重跑，同样看不出缓存粒度问题。
/// </para>
/// <para>
/// <b>陈旧场景</b>：工具接口（<c>[FeishuTool(Source = "IFeishuTenantV1Fake.GetThingAsync")]</c>）声明在
/// 一棵树，被指向的 SDK 方法与它的 <c>[Post(route)]</c> 在<b>另一棵</b>树。只改 SDK 文件时，
/// 工具接口所在语法树不变 ⇒ 若变换结果被整棵语法树粒度缓存，则 route 会停留在旧值。
/// </para>
/// </remarks>
public class GeneratorIncrementalBehaviorTests
{
    private const string RouteV1 = "/open-apis/fake/v1/things";
    private const string RouteV2 = "/open-apis/fake/v2/things";

    /// <summary>工具面源码（<c>Source</c> 指向 SDK 合成面；本树在增量用例中<b>保持不变</b>）。</summary>
    private const string ToolSource = """
        using System.Threading.Tasks;
        using FakeSdk;
        using Mud.Feishu.AI.Tools;

        namespace FakeTools;

        [FeishuTool("fake.get_thing",
            Description = "获取假数据（增量行为用例）。",
            RequiredScopes = new[] { "fake:read" },
            Source = "IFeishuTenantV1Fake.GetThingAsync")]
        public interface IIncrementalFakeTool
        {
            Task<string> GetThingAsync([ToolParameter("thing_id", "假数据 ID")] string thing_id);
        }
        """;

    /// <summary>假 SDK 面（按 route 参数化——route 是唯一被编辑的事实）。</summary>
    private static string SdkSurface(string route) => $$"""
        namespace FakeSdk
        {
            public class FakeThingResult
            {
                public string ThingId { get; set; } = string.Empty;
            }

            public interface IFeishuTenantV1Fake
            {
                [Post("{{route}}")]
                System.Threading.Tasks.Task<FakeThingResult?> GetThingAsync(string thing_id);
            }
        }
        """;

    /// <summary>
    /// 仅编辑 <c>Source</c> 指向的 SDK 文件（改路由）⇒ 工具面产物必须重新发射并携带新路由。
    /// </summary>
    /// <remarks>
    /// 本用例是源码注释「方案 B：保留现状 + 用 <c>GeneratorDriver</c> 用例锁定」的判据：
    /// 若它转红，说明陈旧真实发生，须执行方案 A（接入 <c>CompilationProvider</c> 把该产物降为编译级粒度）。
    /// </remarks>
    [Fact]
    public void EditingSdkRouteOnly_ShouldReemitUpdatedRouteTemplate()
    {
        var trees = GeneratorDriverHost.ParseTrees(
        [
            SyntheticSources.Attributes,
            SyntheticSources.HttpAttributes,
            SdkSurface(RouteV1),
            ToolSource,
        ]);
        var compilation = GeneratorDriverHost.CreateCompilation(trees);
        var driver = GeneratorDriverHost.CreateDriver();

        // ── 第一趟（基线）：产物含旧路由 ──
        var first = GeneratorDriverHost.RunOnce(driver, compilation);
        driver = first.Driver;
        first.Run.Diagnostics.Should().BeEmpty("正向基线不应产生任何生成器诊断");
        Aggregated(first.Run).Should().Contain(RouteV1, "基线产物必须携带 SDK 声明的路由");

        // ── 第二趟：只替换 SDK 那一棵语法树 ──
        var editedCompilation = compilation.ReplaceSyntaxTree(
            trees[2],
            CSharpSyntaxTree.ParseText(SdkSurface(RouteV2), GeneratorDriverHost.ParseOptions));

        var second = GeneratorDriverHost.RunOnce(driver, editedCompilation);
        var text = Aggregated(second.Run);

        text.Should().NotContain(RouteV1,
            "R4-2：SDK 路由已改变，产物不得残留旧路由（残留 = 增量陈旧）");
        text.Should().Contain(RouteV2,
            "R4-2：仅编辑 Source 指向的 SDK 文件时，工具面产物必须重新发射（否则模型拿到陈旧 route）");
    }

    /// <summary>
    /// 无输入变更（同一 compilation 实例再跑一趟）⇒ 全部步骤命中缓存（不为"修陈旧"而牺牲增量收益）。
    /// </summary>
    /// <remarks>
    /// 这是与上一条<b>对称</b>的约束：只要求"改了必须重发"是不够的——若为此把产物降为编译级粒度
    /// （方案 A），每次编辑任何文件都会全量重跑。两条例必须同时绿，才说明粒度选对了。
    /// </remarks>
    [Fact]
    public void NoInputChange_ShouldHitIncrementalCache()
    {
        var compilation = GeneratorDriverHost.CreateCompilation(GeneratorDriverHost.ParseTrees(
        [
            SyntheticSources.Attributes,
            SyntheticSources.HttpAttributes,
            SdkSurface(RouteV1),
            ToolSource,
        ]));
        var driver = GeneratorDriverHost.CreateDriver(trackSteps: true);

        var first = GeneratorDriverHost.RunOnceTracked(driver, compilation);
        driver = first.Driver;

        var second = GeneratorDriverHost.RunOnceTracked(driver, compilation);

        second.Steps.Should().NotBeEmpty("driver 必须处于步骤追踪模式，否则本用例恒绿（假绿）");

        var reasons = second.Steps
            .SelectMany(static step => step.Outputs)
            .Select(static output => output.Reason)
            .Distinct()
            .ToArray();

        reasons.Should().Equal([IncrementalStepRunReason.Cached],
            "无输入变更时必须全部命中缓存——出现 New/Modified 说明增量粒度被破坏");
    }

    private static string Aggregated(GeneratorRun run) => string.Join("\n", run.GeneratedSources);
}
