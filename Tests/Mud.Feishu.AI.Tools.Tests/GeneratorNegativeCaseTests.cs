// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Mud.Feishu.AI.Tools.Tests;

/// <summary>
/// WP1（R4 方案 / R-A 根因）：11 条零容忍诊断的<b>真实可触发负例</b>——每条都以
/// <see cref="CSharpGeneratorDriver"/>（经 <see cref="GeneratorDriverHost"/>）驱动生产生成器，
/// 断言目标诊断真实出现在运行结果中。
/// </summary>
/// <remarks>
/// <para>
/// 负例构造与生成器<b>真实上报点</b>一一对应（<c>CuratedToolScanner.cs</c> / <c>DescriptorValidator.cs</c>），
/// 逐条依据见 R4 方案 §WP1 的负例表（R4.1 评审修订 R-9）。
/// </para>
/// <para>
/// <b>元守卫</b>：<see cref="ZeroToleranceIds_ShouldEachHaveDriverNegativeCase"/>
/// 断言 <c>Diagnostics.ZeroToleranceIds</c> 的每一条都登记在本负例集中、且登记的方法真实存在——
/// 新增零容忍诊断而不同批补负例会立即红。它取代 R3 的"债务登记表"（A2 债务清零，数量只允许下降的
/// 自述式债务不再是合法状态）。
/// </para>
/// </remarks>
public class GeneratorNegativeCaseTests
{
    // ────────── 正向对照 ──────────

    /// <summary>正向对照：合法工具（含 Source 交叉校验全过）不产任何诊断。</summary>
    [Fact]
    public void ValidTool_ShouldProduceNoDiagnostics()
    {
        var run = RunSdk(ToolSource("""
            [FeishuTool("fake.get_thing",
                Description = "获取假数据（正向对照）。",
                RequiredScopes = new[] { "fake:read" },
                Source = "IFeishuTenantV1Fake.GetThingAsync")]
            public interface IValidFakeTool
            {
                Task<string> GetThingAsync([ToolParameter("thing_id", "假数据 ID")] string thing_id);
            }
            """));

        run.Diagnostics.Should().BeEmpty("合法工具不应触发任何生成器诊断");
    }

    // ────────── 11 条零容忍负例 ──────────

    [Fact]
    public void MUDFT001_EmptyToolName_ShouldBeReported()
    {
        var run = GeneratorDriverHost.Run(SyntheticSources.Attributes, ToolSource("""
            [FeishuTool("")]
            public interface IUnnamedTool
            {
            }
            """));

        run.ShouldReport("MUDFT001", "[FeishuTool] 缺工具名必须被上报（CuratedToolScanner 缺名分支）");
    }

    [Fact]
    public void MUDFT002_NonSdkSourceInterfaceName_ShouldBeReported()
    {
        var run = RunSdk(ToolSource("""
            [FeishuTool("fake.wrong_sdk_name",
                Description = "Source 接口不符合 SDK 命名范式。",
                Source = "MyClient.Do")]
            public interface IBadSdkNameTool
            {
            }
            """));

        run.ShouldReport("MUDFT002", "Source 解析成功但接口名不符合 IFeishu[Tenant|User]V{n} 范式必须被上报");
    }

    [Fact]
    public void MUDFT003_DuplicateToolName_ShouldBeReported()
    {
        var run = GeneratorDriverHost.Run(SyntheticSources.Attributes, ToolSource("""
            [FeishuTool("dup.tool", Description = "重名工具 A。")]
            public interface IDupToolA
            {
            }

            [FeishuTool("dup.tool", Description = "重名工具 B。")]
            public interface IDupToolB
            {
            }
            """));

        run.ShouldReport("MUDFT003", "工具名全仓冲突必须被上报（DescriptorValidator L1 唯一性）");
    }

    /// <summary>
    /// R4-10：工具名字面不同、但<b>归一后派生常量名相同</b>（<c>fake.a_b</c> / <c>fake.a.b</c> → 都是
    /// <c>FakeAB</c>）必须被上报——否则 <c>FeishuToolNames</c> 出现重复常量（CS0101），
    /// 或 <c>{Tool}Args</c> 的 hintName 重复使 <c>AddSource</c> 抛异常（表面症状是 MUDFT026）。
    /// </summary>
    [Fact]
    public void MUDFT027_DerivedConstantNameCollision_ShouldBeReported()
    {
        var run = GeneratorDriverHost.Run(SyntheticSources.Attributes, ToolSource("""
            [FeishuTool("fake.a_b", Description = "派生常量名碰撞 A（_ 与 . 归一后同名）。")]
            public interface IDerivedCollisionA
            {
            }

            [FeishuTool("fake.a.b", Description = "派生常量名碰撞 B（_ 与 . 归一后同名）。")]
            public interface IDerivedCollisionB
            {
            }
            """));

        run.ShouldNotReport("MUDFT003", "两条工具名字面不同，不属 MUDFT003 的判定范围");
        run.ShouldReport("MUDFT027", "字面不同的工具名归一到同一编译期常量名必须被上报（产物会 CS0101/重复 hintName）");
    }

    /// <summary>
    /// R4-10 对照：归一后仍互异的工具名不得触发 MUDFT027（防"报得太宽"把合法命名拦下）。
    /// </summary>
    [Fact]
    public void MUDFT027_DistinctDerivedConstantNames_ShouldNotBeReported()
    {
        var run = GeneratorDriverHost.Run(SyntheticSources.Attributes, ToolSource("""
            [FeishuTool("fake.a_b", Description = "派生常量名互异 A。")]
            public interface IDistinctDerivedA
            {
            }

            [FeishuTool("fake.a_bc", Description = "派生常量名互异 B。")]
            public interface IDistinctDerivedB
            {
            }
            """));

        run.ShouldNotReport("MUDFT027", "派生常量名互异（FakeAB / FakeABc）不应被上报");
    }

    // ────────── 产物名字唯一性（R4-10） ──────────

    /// <summary>
    /// R4-10：两个命名空间下的<b>同名执行器</b>必须产出互异的注册器类型名 / 核心方法名 / hintName。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 修复前三者都由执行器<b>简单名</b>派生且落入同一命名空间与同一静态类：注册器类型名重复 ⇒
    /// <c>CS0101</c>；静态类出现签名相同的方法 ⇒ <c>CS0111</c>；hintName 重复使 <c>AddSource</c> 抛异常
    /// ⇒ 被 <c>Guard</c> 兜成 <c>MUDFT026</c>（"生成器内部异常"这一表面症状，定位不到根因）。
    /// </para>
    /// <para>
    /// <b>与文档原名的差异</b>：方案原拟 "碰撞即报诊断"，实现改为<b>仅对碰撞者追加命名空间后缀</b>——
    /// 因为注册器类型名与 Core 方法名<b>必须</b>存在（不像工具名可以要求改名），且无碰撞者改名会打断
    /// 手写 <c>AddFeishu{X}Core</c> 调用点。故本用例断言的是"产物并存且名字互异"，而非诊断上报。
    /// </para>
    /// </remarks>
    [Fact]
    public void RegistrarNames_ShouldBeDisambiguated_WhenExecutorSimpleNamesCollide()
    {
        var run = GeneratorDriverHost.RunAsOwnerAssembly(
            SyntheticSources.Attributes,
            SyntheticSources.HandlerAttribute,
            CollidingExecutorSource("FakeExecutorA", "fake.disambiguation_a", "IDisambiguationATool"),
            CollidingExecutorSource("FakeExecutorB", "fake.disambiguation_b", "IDisambiguationBTool"));

        // 修复前：重复 hintName 让 AddSource 抛异常 → Guard 兜成 MUDFT026。
        run.Diagnostics.Should().BeEmpty("消歧后不得有任何诊断（尤其不得出现 MUDFT026 这一「生成器内部异常」表面症状）");

        var generated = string.Join("\n", run.GeneratedSources);

        // ① 注册器类型名互异且保持固定尾缀形态。
        generated.Should().Contain("class DupFakeExecutorAToolDomainRegistrar(",
            "命名空间 A 下的 DupTools 应产出消歧后的注册器名");
        generated.Should().Contain("class DupFakeExecutorBToolDomainRegistrar(",
            "命名空间 B 下的 DupTools 应产出消歧后的注册器名");

        // ② 核心方法名互异（否则 FeishuToolsServiceCollectionCoreExtensions 内 CS0111）。
        generated.Should().Contain("AddFeishuDupToolsFakeExecutorACore(this IServiceCollection services)");
        generated.Should().Contain("AddFeishuDupToolsFakeExecutorBCore(this IServiceCollection services)");

        // ③ 核心方法体内引用的注册器类型必须同步消歧（否则 CS0246）。
        generated.Should().Contain("Registration.DupFakeExecutorAToolDomainRegistrar(executor,");
        generated.Should().Contain("Registration.DupFakeExecutorBToolDomainRegistrar(executor,");
    }

    /// <summary>R4-10 的合成源码节：指定命名空间下的同名执行器 <c>DupTools</c> 及其携带的工具。</summary>
    private static string CollidingExecutorSource(string ns, string toolName, string toolInterfaceName) => $$"""
        namespace {{ns}};

        /// <summary>执行器返回类型桩（生成器按简单名匹配；每个命名空间各自声明一份）。</summary>
        public sealed class FeishuToolResult
        {
        }

        [Mud.Feishu.AI.Tools.FeishuTool("{{toolName}}", Description = "同名执行器消歧用例。")]
        public interface {{toolInterfaceName}}
        {
        }

        internal sealed class DupTools
        {
            [Mud.Feishu.AI.FeishuTools.FeishuToolHandler(typeof({{toolInterfaceName}}))]
            public System.Threading.Tasks.Task<FeishuToolResult> QueryAsync(
                System.Collections.Generic.IReadOnlyDictionary<string, object?> args,
                System.Threading.CancellationToken ct) => throw new System.NotSupportedException();
        }
        """;

    [Fact]
    public void MUDFT004_UnmappableReturnType_ShouldBeReported()
    {
        var run = RunSdk(ToolSource("""
            [FeishuTool("fake.run_job",
                Description = "返回非泛型 Task 的源方法。",
                Source = "IFeishuTenantV1Fake.RunJobAsync")]
            public interface INonGenericTaskTool
            {
            }
            """));

        run.ShouldReport("MUDFT004", "返回类型不可映射（非泛型 Task）必须被上报");
    }

    [Fact]
    public void MUDFT008_UploadParameterNotBinary_ShouldBeReported()
    {
        var run = RunSdk(ToolSource("""
            [FeishuTool("fake.upload",
                Description = "上传方法带不可映射参数。",
                Source = "IFeishuTenantV1Fake.UploadAsync")]
            public interface IBadUploadParamTool
            {
            }
            """));

        run.ShouldReport("MUDFT008", "上传方法的 string 参数无法映射 format:binary 必须被上报");
    }

    [Fact]
    public void MUDFT010_CompositeParameterDegradedToString_ShouldBeReported()
    {
        var run = RunSdk(ToolSource("""
            [FeishuTool("fake.empty_dto", Description = "空 DTO 参数降级为字符串。")]
            public interface IEmptyDtoTool
            {
                Task<string> QueryAsync(EmptyDto options);
            }
            """));

        run.ShouldReport("MUDFT010", "复合 DTO 无可展开属性被降级为 string 必须被上报（模型无法得知可传字段）");
    }

    /// <summary>
    /// R5 / B-6：<c>AnyOf</c> 条件必填组引用了签名中<b>不存在</b>的参数名。
    /// </summary>
    /// <remarks>
    /// 这类错误若放行，渲染出的 <c>"anyOf":[{"required":["不存在的字段"]}]</c>
    /// <b>约束了空气</b>：构建通过、Schema 合法、但约束对模型永久无效——典型的静默失效。
    /// </remarks>
    [Fact]
    public void MUDFT011_AnyOfGroupReferencingUnknownParameter_ShouldBeReported()
    {
        var run = RunSdk(ToolSource("""
            [FeishuTool("fake.any_of_typo", Description = "AnyOf 组引用不存在的参数。", AnyOf = ["user_id|room_idd"])]
            public interface IAnyOfTypoTool
            {
                Task<string> QueryAsync(string user_id);
            }
            """));

        run.ShouldReport("MUDFT011", "AnyOf 组引用不存在的参数必须被上报（否则 anyOf 约束空气、静默失效）");
    }

    /// <summary>
    /// R5 / B-6：<c>AnyOf</c> 组内混入 <c>Required = true</c> 的参数 ⇒ 语义从"至少一个"反转为"全部必填"。
    /// </summary>
    [Fact]
    public void MUDFT011_AnyOfGroupMixingRequiredParameter_ShouldBeReported()
    {
        var run = RunSdk(ToolSource("""
            [FeishuTool("fake.any_of_required_mix", Description = "AnyOf 组混入必填参数。", AnyOf = ["a|b"])]
            public interface IAnyOfRequiredMixTool
            {
                Task<string> QueryAsync(
                    [ToolParameter("a", "a", Required = true)] string a,
                    string b = "");
            }
            """));

        run.ShouldReport("MUDFT011", "AnyOf 组混入 Required=true 参数必须被上报（anyOf 语义会反转为全部必填）");
    }

    [Fact]
    public void MUDFT014_GoldenDrift_ShouldBeReported_AndCleanRunShouldNot()
    {
        // 负例：golden AdditionalText 与实产不一致 → MUDFT014。
        var drifted = GeneratorDriverHost.Run(
            new InMemoryAdditionalText("FeishuToolSchemas.golden.txt", "gold 内容与实产不一致"),
            SyntheticSources.Attributes,
            ToolSource("""
                [FeishuTool("fake.golden_drift", Description = "golden 漂移负例。")]
                public interface IGoldenDriftTool
                {
                }
                """));

        drifted.ShouldReport("MUDFT014", "golden 快照与实产不一致必须被上报（描述符静默漂移门禁）");

        // 对照：无 AdditionalFile 时不做比对，不得报 014。
        var clean = GeneratorDriverHost.Run(SyntheticSources.Attributes, ToolSource("""
            [FeishuTool("fake.golden_drift", Description = "golden 漂移负例。")]
            public interface IGoldenDriftTool
            {
            }
            """));

        clean.ShouldNotReport("MUDFT014", "未声明 golden 时不做比对（重固化流程的前提）");
    }

    [Fact]
    public void MUDFT015_DeclaredToolParameterNameDrift_ShouldBeReported()
    {
        var run = GeneratorDriverHost.Run(SyntheticSources.Attributes, ToolSource("""
            [FeishuTool("fake.declared_name_drift", Description = "[ToolParameter] 名与渲染键漂移。")]
            public interface IDeclaredNameDriftTool
            {
                Task<string> QueryAsync([ToolParameter("user_id", "用户 ID（声明名与 C# 参数名不一致）")] string userId);
            }
            """));

        run.ShouldReport("MUDFT015", "[ToolParameter] 声明名 ≠ 渲染键（模型可见契约漂移）必须被上报");
    }

    [Fact]
    public void MUDFT016_UserIdentityOnNonUserInterface_ShouldBeReported()
    {
        var run = RunSdk(ToolSource("""
            [FeishuTool("fake.my_tasks",
                Description = "user 身份工具落在非 IFeishuUser* 承载接口。",
                Source = "IFeishuUserV2Task.CreateTaskAsync")]
            public interface IFakeUserTool
            {
            }
            """));

        run.ShouldReport("MUDFT016", "User 身份与承载接口令牌类型不符必须被上报（DescriptorValidator L3）");
    }

    [Fact]
    public void MUDFT017_WriteSdkSourceMarkedReadonly_ShouldBeReported()
    {
        var run = RunSdk(ToolSource("""
            [FeishuTool("fake.delete_thing",
                Description = "源方法命中危险词但工具未声明 IsWrite。",
                Source = "IFeishuTenantV1Fake.DeleteThingAsync")]
            public interface IReadButDeleteTool
            {
            }
            """));

        run.ShouldReport("MUDFT017", "SDK 事实为写面（危险词命中）而工具未声明 IsWrite 必须被上报");
    }

    [Fact]
    public void MUDFT019_UnresolvableSource_ShouldBeReported()
    {
        var run = GeneratorDriverHost.Run(SyntheticSources.Attributes, ToolSource("""
            [FeishuTool("fake.ghost_source",
                Description = "Source 指向不存在的类型。",
                Source = "INotExist.Method")]
            public interface IGhostSourceTool
            {
            }
            """));

        run.ShouldReport("MUDFT019", "Source 无法解析（工具面与 SDK 脱钩）必须被上报");
    }

    [Fact]
    public void MUDFT020_UnmappedParameterType_ShouldBeReported()
    {
        // 参数解包产物只发射进工具面实现包（owner 门槛），故须以该程序集名驱动——
        // 见 GeneratorDriverHost.RunAsOwnerAssembly 的说明。
        var run = GeneratorDriverHost.RunAsOwnerAssembly(SyntheticSources.Attributes, ToolSource("""
            [FeishuTool("fake.unmapped_arg", Description = "参数类型不在 ToolArgs 映射表内。")]
            public interface IUnmappedArgTypeTool
            {
                Task<string> QueryAsync([ToolParameter("start_year", "年份（可选）")] long? start_year = null);
            }
            """));

        run.ShouldReport("MUDFT020", "参数 C# 类型不在 ToolArgs 解包映射表内必须被上报（否则生成产物无法编译）");
    }

    [Fact]
    public void MUDFT021_RequiredNullableParameter_ShouldBeWarned()
    {
        var run = GeneratorDriverHost.RunAsOwnerAssembly(SyntheticSources.Attributes, ToolSource("""
            [FeishuTool("fake.required_nullable", Description = "必填参数被声明为可空。")]
            public interface IRequiredNullableTool
            {
                Task<string> QueryAsync([ToolParameter("thing_id", "假数据 ID", Required = true)] string? thing_id);
            }
            """));

        run.ShouldReport("MUDFT021", "Required=true 与可空并存（Schema required 与解包语义不一致）必须被警告");
    }

    [Fact]
    public void OwnerGatedArgs_ShouldNotBeEmittedInNonOwnerAssembly()
    {
        // 对照：非实现包（默认宿主程序集名）下参数解包产物不发射——否则声明样例 [FeishuTool] 接口的
        // 测试工程会因 FeishuTools 的 internal ToolArgs 不可见而 CS0103。
        var run = GeneratorDriverHost.Run(SyntheticSources.Attributes, ToolSource("""
            [FeishuTool("fake.owner_gate", Description = "owner 门槛对照。")]
            public interface IOwnerGateTool
            {
                Task<string> QueryAsync([ToolParameter("thing_id", "假数据 ID", Required = true)] string thing_id);
            }
            """));

        run.GeneratedSources.Should().NotContain(
            source => source.Contains("class FakeOwnerGateArgs", StringComparison.Ordinal),
            "非实现包不得发射参数解包产物（ToolArgs 不可见 → CS0103）");
        run.ShouldNotReport("MUDFT020", "owner 门槛下不做参数类型映射校验");
        run.ShouldNotReport("MUDFT021", "owner 门槛下不做必填可空校验");
    }

    [Fact]
    public void MUDFT022_ToolWithoutHandlerBinding_ShouldBeReported()
    {
        var run = GeneratorDriverHost.RunAsOwnerAssembly(
            SyntheticSources.Attributes,
            SyntheticSources.HandlerAttribute,
            OwnerSource("""
                [FeishuTool("fake.unbound", Description = "没有任何执行器绑定的工具。")]
                public interface IUnboundTool
                {
                    System.Threading.Tasks.Task<string> QueryAsync([ToolParameter("thing_id", "假数据 ID")] string thing_id);
                }
                """));

        run.ShouldReport("MUDFT022", "契约工具缺少 [FeishuToolHandler] 绑定必须被上报（注册完备性的编译期守卫）");
    }

    [Fact]
    public void MUDFT023_HandlerOnInterfaceWithoutFeishuTool_ShouldBeReported()
    {
        var run = GeneratorDriverHost.RunAsOwnerAssembly(
            SyntheticSources.Attributes,
            SyntheticSources.HandlerAttribute,
            OwnerSource("""
                public interface INotACuratedTool
                {
                    System.Threading.Tasks.Task<string> QueryAsync(string thing_id);
                }

                internal sealed class BadTargetTools
                {
                    [FeishuToolHandler(typeof(INotACuratedTool))]
                    public System.Threading.Tasks.Task<FeishuToolResult> QueryAsync(
                        System.Collections.Generic.IReadOnlyDictionary<string, object?> args,
                        System.Threading.CancellationToken ct) => throw new System.NotSupportedException();
                }
                """));

        run.ShouldReport("MUDFT023", "handler 指向未标注 [FeishuTool] 的接口（推导不出工具名）必须被上报");
    }

    [Fact]
    public void MUDFT023_DuplicateHandlerForSameTool_ShouldBeReported()
    {
        var run = GeneratorDriverHost.RunAsOwnerAssembly(
            SyntheticSources.Attributes,
            SyntheticSources.HandlerAttribute,
            OwnerSource("""
                [FeishuTool("fake.dup_binding", Description = "被两个执行器方法绑定的工具。")]
                public interface IDupBindingTool
                {
                    System.Threading.Tasks.Task<string> QueryAsync([ToolParameter("thing_id", "假数据 ID")] string thing_id);
                }

                internal sealed class DupBindingTools
                {
                    [FeishuToolHandler(typeof(IDupBindingTool))]
                    public System.Threading.Tasks.Task<FeishuToolResult> QueryAsync(
                        System.Collections.Generic.IReadOnlyDictionary<string, object?> args,
                        System.Threading.CancellationToken ct) => throw new System.NotSupportedException();

                    [FeishuToolHandler(typeof(IDupBindingTool))]
                    public System.Threading.Tasks.Task<FeishuToolResult> QueryAgainAsync(
                        System.Collections.Generic.IReadOnlyDictionary<string, object?> args,
                        System.Threading.CancellationToken ct) => throw new System.NotSupportedException();
                }
                """));

        run.ShouldReport("MUDFT023", "同一工具被多个执行器方法绑定（产物会重复注册）必须被上报");
    }

    [Fact]
    public void MUDFT024_HandlerWithWrongSignature_ShouldBeReported()
    {
        var run = GeneratorDriverHost.RunAsOwnerAssembly(
            SyntheticSources.Attributes,
            SyntheticSources.HandlerAttribute,
            OwnerSource("""
                [FeishuTool("fake.bad_signature", Description = "handler 方法签名不符。")]
                public interface IBadSignatureTool
                {
                    System.Threading.Tasks.Task<string> QueryAsync([ToolParameter("thing_id", "假数据 ID")] string thing_id);
                }

                internal sealed class BadSignatureTools
                {
                    [FeishuToolHandler(typeof(IBadSignatureTool))]
                    public System.Threading.Tasks.Task<string> QueryAsync(
                        System.Collections.Generic.IReadOnlyDictionary<string, object?> args,
                        System.Threading.CancellationToken ct) => throw new System.NotSupportedException();
                }
                """));

        run.ShouldReport("MUDFT024", "handler 方法签名不符合执行器契约（返回 Task<string>）必须被上报");
    }

    [Fact]
    public void MUDFT025_ExecutorConstructorWithNonServiceParameter_ShouldBeReported()
    {
        var run = GeneratorDriverHost.RunAsOwnerAssembly(
            SyntheticSources.Attributes,
            SyntheticSources.HandlerAttribute,
            OwnerSource("""
                [FeishuTool("fake.bad_dependency", Description = "执行器构造参数不是 DI 服务类型。")]
                public interface IBadDependencyTool
                {
                    System.Threading.Tasks.Task<string> QueryAsync([ToolParameter("thing_id", "假数据 ID")] string thing_id);
                }

                internal sealed class BadDependencyTools(string connectionString)
                {
                    [FeishuToolHandler(typeof(IBadDependencyTool))]
                    public System.Threading.Tasks.Task<FeishuToolResult> QueryAsync(
                        System.Collections.Generic.IReadOnlyDictionary<string, object?> args,
                        System.Threading.CancellationToken ct) => throw new System.NotSupportedException();
                }
                """));

        run.ShouldReport("MUDFT025", "执行器构造参数为 BCL 内建类型（string）无法作为 DI 服务类型解析时必须被上报");
    }

    // ────────── 元守卫 ──────────

    /// <summary>
    /// MUDFT026（生成器内部异常兜底）验证：描述符已声明、为 Error 级、且<b>全部 6 条输出路径</b>
    /// 都经统一 <c>Guard</c> 包装。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>R2-03 对本用例的修订（重要）</b>：本用例原断言"<c>EmitToolSurface</c> 方法体内含 try/catch"——
    /// 那是 R1-WP5 的形态，并且正是 R2-03 认定的缺陷：6 条输出路径只有 1 条有兜底，而当时的验收方式
    /// （对这一条路径注入异常）无法发现另外 5 条裸奔。真实的注入式负例在增量生成器架构下不可行
    /// （管线由 Roslyn 驱动，外部无法注入中间异常），故处置方式改为
    /// <b>结构断言</b>：<c>GuardChecker</c> 逐个解析每一处 <c>RegisterSourceOutput</c>，
    /// 断言第二个实参均为 <c>Guard&lt;T&gt;(...)</c>——覆盖面等于代码里的实际路径数，
    /// <b>新增路径自动被覆盖</b>（详见 <c>ContractGuards/GeneratorOutputGuardContractGuards</c>）。
    /// </para>
    /// <para>本用例保留的职责：描述符声明 + 严重级 + "<c>Guard</c> 是唯一兜底实现"（防两套写法回潮）。</para>
    /// </remarks>
    [Fact]
    public void MUDFT026_GeneratorExceptionGuard_ShouldCoverEveryOutputPath()
    {
        var repoRoot = FindRepositoryRoot();

        // ① 描述符声明存在。
        var diagnosticsPath = Path.Combine(repoRoot, "Mud.Feishu.AI.Tools", "Diagnostics.cs");
        var diagnosticsSource = File.ReadAllText(diagnosticsPath);
        diagnosticsSource.Should().Contain("MUDFT026", "MUDFT026 描述符必须在 Diagnostics.cs 中声明");

        // ② Error 级（零容忍集内的诊断必须是 Error）。
        var severityMatch = System.Text.RegularExpressions.Regex.Match(
            diagnosticsSource,
            @"id:\s*""MUDFT026"".*?defaultSeverity:\s*DiagnosticSeverity\.Error",
            System.Text.RegularExpressions.RegexOptions.Singleline);
        severityMatch.Success.Should().BeTrue("MUDFT026 既列入 ZeroToleranceIds，其 severity 必须是 Error");

        // ③ Guard 是兜底的唯一实现（6/6 路径的结构断言由 GeneratorOutputGuardContractGuards 承担）。
        var generatorPath = Path.Combine(repoRoot, "Mud.Feishu.AI.Tools", "FeishuToolSchemaGenerator.cs");
        var generatorSource = File.ReadAllText(generatorPath);
        generatorSource.Should().Contain("Diagnostics.MUDFT026",
            "Guard 的 catch 块必须上报 MUDFT026（R2-03 故障隔离的唯一上报点）");
        generatorSource.Should().Contain(
            "when (ex is not OperationCanceledException)",
            "Guard 必须放行 OperationCanceledException（否则取消构建会变成构建失败）");
        generatorSource.Should().Contain("private static Action<SourceProductionContext, T> Guard<T>(",
            "全部输出路径必须共用同一个 Guard 包装器（禁止回到逐路径手写 try/catch 的两套写法）");
    }

    /// <summary>
    /// <c>ZeroToleranceIds</c> 的每一条都必须有 driver 负例（双向：不多、不少、登记的方法真实存在）。
    /// 取代 R3 的"债务登记表"——A2 债务清零后，登记负例是<b>义务</b>而非可豁免状态。
    /// </summary>
    [Fact]
    public void ZeroToleranceIds_ShouldEachHaveDriverNegativeCase()
    {
        var zeroToleranceIds = ReadZeroToleranceIds();
        zeroToleranceIds.Should().NotBeEmpty("ZeroToleranceIds 必须可解析（解析失败说明 Diagnostics.cs 结构被改坏）");

        var registry = typeof(GeneratorNegativeCaseTests)
            .GetMethods()
            .Where(static m => m.GetCustomAttributes(typeof(FactAttribute), inherit: false).Length > 0)
            .Select(static m => m.Name)
            .ToArray();

        foreach (var id in zeroToleranceIds)
        {
            registry.Should().Contain(name => name.StartsWith(id, StringComparison.Ordinal),
                $"零容忍诊断 {id} 缺少 driver 负例——新增零容忍项必须同批补可触发负例（防假门禁复发）");
        }
    }

    /// <summary>负例对应的合成源码节（工具面声明）。</summary>
    private static string ToolSource(string content) => $$"""
        using System.Threading.Tasks;
        using FakeSdk;
        using Mud.Feishu.AI.Tools;

        namespace FakeTools;

        {{content}}
        """;

    /// <summary>
    /// 工具面<b>实现包侧</b>的合成源码节（执行器 + <c>[FeishuToolHandler]</c> 绑定）。
    /// </summary>
    /// <remarks>
    /// 注册器 / DI 产物消费 FeishuTools 的 <c>internal</c> 成员，故其诊断只在该程序集名下触发
    /// （<see cref="GeneratorDriverHost.RunAsOwnerAssembly"/>）；本节的类型全部以全限定名书写，
    /// 避免与 <c>FakeTools</c> 节产生命名冲突。
    /// </remarks>
    private static string OwnerSource(string content) => $$"""
        using System.Threading.Tasks;
        using Mud.Feishu.AI.FeishuTools;
        using Mud.Feishu.AI.Tools;

        namespace FakeExecutor;

        /// <summary>执行器返回类型桩（生成器按简单名匹配）。</summary>
        public sealed class FeishuToolResult
        {
        }

        {{content}}
        """;

    /// <summary>驱动生成器并预置完整 SDK 面（特性 + 假 SDK 接口）。</summary>
    /// <summary>
    /// R5 / F-2 正向断言：<b>常量类闭集</b>（<c>static class</c> + <c>const int</c>）的 <b>Schema 侧</b>。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么只断言 Schema 侧</b>：<c>FeishuToolArgs</c> 产物的发射门槛是
    /// <b>「名字契约所有者程序集」</b>（<c>FeishuToolSchemaGenerator.cs:120-126</c>：
    /// 产物消费 FeishuTools 的 <c>internal ToolArgs</c>，其他程序集一并发射会 CS0103）。
    /// driver 的合成程序集名不匹配 ⇒ Args 侧<b>在 driver 中不可观测</b>。
    /// Args 侧由两道互补机制守护：① <b>真实构建</b>——闭集解包失败会触发零容忍
    /// <c>MUDFT020</c>（整份 {Tool}Args 不产出）⇒ 构建失败；
    /// ② <c>ToolArgsClosedSetHelperTests</c> 对 <c>ToolArgs.RequireNamedInt</c> /
    /// <c>RequireEnum</c> 做运行时行为断言。
    /// </para>
    /// <para>
    /// <b>为什么必须有这条正向用例</b>：本项此前零覆盖，且只加负例不够——负例只证明"会报错"，
    /// 不证明"能用"。
    /// </para>
    /// </remarks>
    [Fact]
    public void F2_ConstClassClosedSet_ShouldRenderEnumInSchema()
    {
        var run = RunSdk(ToolSource("""
            public static class BlockTypes
            {
                public const int Page = 1;
                public const int Text = 2;
                public const int Heading1 = 3;
            }

            [FeishuTool("fake.append_blocks", Description = "追加块。")]
            public interface IAppendBlocksTool
            {
                Task<string> AppendAsync(
                    [ToolParameter("block_type", "块类型", Required = true, EnumType = typeof(BlockTypes))] int block_type);
            }
            """));

        var all = string.Join("\n", run.GeneratedSources);

        all.Should().Contain("\"enum\":[\"Page\",\"Text\",\"Heading1\"]", "常量类闭集未渲染进 Schema");
        all.Should().NotContain("value__", "enum 列表混入了 enum 的实例字段 value__");
    }

    /// <summary>
    /// R5 / F-2 正向断言：<b>真实 C# enum 闭集</b>的 <b>Schema 侧</b>。
    /// </summary>
    /// <remarks>
    /// Args 侧（泛型 <c>ToolArgs.RequireEnum&lt;TEnum&gt;</c>）在此<b>不可观测</b>——原因与
    /// <see cref="F2_ConstClassClosedSet_ShouldRenderEnumInSchema"/> 相同（程序集名门槛），
    /// 由 <c>ToolArgsClosedSetHelperTests</c>做运行时行为断言 + 真实构建的 MUDFT020 兜底。
    /// </remarks>
    [Fact]
    public void F2_CsharpEnumClosedSet_ShouldRenderEnumInSchema()
    {
        var run = RunSdk(ToolSource("""
            public enum MsgType
            {
                Text,
                Image,
                Interactive,
            }

            [FeishuTool("fake.send", Description = "发消息。")]
            public interface ISendTool
            {
                Task<string> SendAsync(
                    [ToolParameter("msg_type", "消息类型", Required = true, EnumType = typeof(MsgType))] MsgType msg_type);
            }
            """));

        var all = string.Join("\n", run.GeneratedSources);

        all.Should().Contain("\"enum\":[\"Text\",\"Image\",\"Interactive\"]", "C# enum 闭集未渲染进 Schema");
        all.Should().NotContain("value__", "enum 列表混入了 enum 的实例字段 value__");
    }

    /// <summary>
    /// R5 / F-2 反向自证：合成源里<b>确实</b>带 <c>EnumType</c> 声明位。
    /// 若哪天合成源不再注入该属性，上面两条会因"闭集根本没声明"而<b>假绿</b>。
    /// </summary>
    [Fact]
    public void F2_SyntheticToolParameter_ShouldDeclareEnumType_OtherwiseTheClosedSetCasesAreFalseGreen()
    {
        // 合成源在本文件内联声明（见 ToolSource 的 SyntheticSources 段），
        // 故直接断言"本文件确实给出了 EnumType 声明位"——若哪天删掉，上面两条会假绿。
        var source = ReadSelfSource();
        source.Should().Contain(
            "public System.Type? EnumType { get; set; }",
            "合成 ToolParameterAttribute 未声明 EnumType ⇒ F-2 的正向/负例用例全部假绿");    }

    /// <summary>读取本测试文件自身源码（用于"合成源确实含某声明位"这类自证断言）。</summary>
    private static string ReadSelfSource()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Mud.Feishu.slnx")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull();
        return File.ReadAllText(Path.Combine(
            directory!.FullName, "Tests", "Mud.Feishu.AI.Tools.Tests", "GeneratorNegativeCaseTests.cs"));
    }

    private static GeneratorRun RunSdk(params string[] sources)
        => GeneratorDriverHost.Run([.. SyntheticSources.SdkSurface, .. sources]);

    /// <summary>从生成器源码解析 <c>ZeroToleranceIds</c>（与既有契约守卫同一体例：源码扫描）。</summary>
    private static IReadOnlyList<string> ReadZeroToleranceIds()
    {
        var path = FindRepositoryRoot();
        path = Path.Combine(path, "Mud.Feishu.AI.Tools", "Diagnostics.cs");
        var source = File.ReadAllText(path);

        var block = Regex.Match(source, @"ZeroToleranceIds\s*=\s*\[(?<body>[^\]]*)\]", RegexOptions.Singleline);
        if (!block.Success)
        {
            return [];
        }

        return Regex.Matches(block.Groups["body"].Value, @"""(MUDFT[0-9]{3})""")
            .Select(static m => m.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();
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

/// <summary>合成源码：生成器识别所需的特性与假 SDK 面（详见各成员注释）。</summary>
public static class SyntheticSources
{
    /// <summary>完整 SDK 面（特性 + 假 SDK 接口）：任何带 <c>Source</c> 的负例都必须整套传入。</summary>
    public static string[] SdkSurface => [Attributes, HttpAttributes, FakeSdkInterfaces];

    /// <summary>
    /// <c>[FeishuTool]</c> / <c>[ToolParameter]</c> 的最小合成声明——生成器按
    /// 「命名空间 <c>Mud.Feishu.AI.Tools</c> + 类名」识别（<c>Extractors.GetFeishuToolAttribute</c>），
    /// 故命名空间与形状必须与真实契约一致；其余成员按具名实参访问面声明。
    /// </summary>
    public const string Attributes = """
        namespace Mud.Feishu.AI.Tools
        {
            [System.AttributeUsage(System.AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
            public sealed class FeishuToolAttribute : System.Attribute
            {
                public FeishuToolAttribute(string name) => Name = name;

                public string Name { get; }

                public string Description { get; set; } = string.Empty;

                public string[] RequiredScopes { get; set; } = System.Array.Empty<string>();

                public bool IsWrite { get; set; }

                public string? Source { get; set; }

                /// <summary>条件必填组（R5 / B-6）：每项形如 "a|b"，表示组内至少提供一个。</summary>
                public string[] AnyOf { get; set; } = System.Array.Empty<string>();
            }

            [System.AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Parameter, AllowMultiple = true, Inherited = false)]
            public sealed class ToolParameterAttribute : System.Attribute
            {
                public ToolParameterAttribute(string name, string description) { }

                public bool Required { get; set; }

                /// <summary>取值闭集类型（R5 / F-2）。真实类型在 Mud.Feishu.AI.Tools，本副本供合成源使用。</summary>
                public System.Type? EnumType { get; set; }
            }
        }
        """;

    /// <summary>
    /// <c>[FeishuToolHandler]</c> 的最小合成声明——生成器按「命名空间 <c>Mud.Feishu.AI.FeishuTools</c>
    /// + 类名」识别（<c>ToolHandlerScanner.GetHandlerAttribute</c>），故命名空间与形状必须与真实契约一致。
    /// </summary>
    /// <remarks>
    /// 实参是 <see cref="System.Type"/>（<c>typeof(接口)</c>）：真实特性亦如此——工具名取自被指向接口
    /// 自身的 <c>[FeishuTool]</c> 声明，<b>不能</b>用生成器本趟产出的 <c>FeishuToolNames</c> 常量。
    /// </remarks>
    public const string HandlerAttribute = """
        namespace Mud.Feishu.AI.FeishuTools
        {
            [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
            public sealed class FeishuToolHandlerAttribute : System.Attribute
            {
                public FeishuToolHandlerAttribute(System.Type toolInterface) => ToolInterface = toolInterface;

                public System.Type ToolInterface { get; }
            }
        }
        """;

    /// <summary>
    /// 假 HTTP 绑定特性——生成器按<b>类名</b>匹配（<c>GetAttribute</c>/<c>PostAttribute</c>/…，
    /// <c>Extractors.ExtractHttpInfo</c>），命名空间无关。
    /// </summary>
    public const string HttpAttributes = """
        namespace FakeSdk
        {
            [System.AttributeUsage(System.AttributeTargets.Method)]
            public abstract class HttpAttribute : System.Attribute
            {
                protected HttpAttribute(string route) => Route = route;

                public string Route { get; }
            }

            public sealed class GetAttribute(string route) : HttpAttribute(route);

            public sealed class PostAttribute(string route) : HttpAttribute(route);

            public sealed class PutAttribute(string route) : HttpAttribute(route);

            public sealed class PatchAttribute(string route) : HttpAttribute(route);

            public sealed class DeleteAttribute(string route) : HttpAttribute(route);

            [System.AttributeUsage(System.AttributeTargets.Parameter)]
            public sealed class FormContentAttribute : System.Attribute;
        }
        """;

    /// <summary>
    /// 假 SDK 面：Source 按「类型简单名 + 方法名」解析（<c>Extractors.ResolveSourceMember</c>
    /// 先找 <c>Mud.Feishu.{TypeName}</c>，再全编译递归找简单名），故可完全在合成源码内声明。
    /// </summary>
    public const string FakeSdkInterfaces = """
        namespace FakeSdk
        {
            public class FakeThingResult
            {
                public string ThingId { get; set; } = string.Empty;

                public string Name { get; set; } = string.Empty;
            }

            public class EmptyDto
            {
            }

            public interface IFeishuTenantV1Fake
            {
                [Post("/open-apis/fake/v1/things")]
                System.Threading.Tasks.Task<FakeThingResult?> GetThingAsync(string thing_id);

                [Post("/open-apis/fake/v1/jobs")]
                System.Threading.Tasks.Task RunJobAsync();

                [Post("/open-apis/fake/v1/uploads")]
                System.Threading.Tasks.Task<FakeThingResult?> UploadAsync([FormContent] byte[] file, string path);

                [Get("/open-apis/fake/v1/things")]
                System.Threading.Tasks.Task<FakeThingResult?> DeleteThingAsync(string thing_id);
            }

            public class MyClient
            {
                [Post("/open-apis/fake/v1/do")]
                public System.Threading.Tasks.Task<FakeThingResult?> Do(string input) => throw new System.NotSupportedException();
            }

            public interface IFeishuUserV2Task
            {
                [Post("/open-apis/fake/v2/tasks")]
                System.Threading.Tasks.Task<FakeThingResult?> CreateTaskAsync(string title);
            }
        }
        """;
}
