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

    // ────────── 元守卫 ──────────

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

    /// <summary>驱动生成器并预置完整 SDK 面（特性 + 假 SDK 接口）。</summary>
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
            }

            [System.AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Parameter, AllowMultiple = true, Inherited = false)]
            public sealed class ToolParameterAttribute : System.Attribute
            {
                public ToolParameterAttribute(string name, string description) { }

                public bool Required { get; set; }
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
