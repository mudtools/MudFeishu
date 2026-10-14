// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;
using Mud.AI.Attributes;
using Mud.AI.ToolSurface;
using Mud.AI.ToolSurface.Extraction;

namespace Mud.AI.Generator.Tests;

/// <summary>
/// 工具面引擎的<b>契约层</b>测试（设计文档 §10）：剖面模型解析、引擎侧镜像枚举与运行时枚举的一致性、
/// SDKT001 剖面成对守卫。
/// </summary>
/// <remarks>
/// 镜像枚举一致性是本引擎的核心风险点：工程纪律禁止生成器引用任何 <c>Mud.*</c> 运行时程序集，
/// 故 <see cref="ToolSurfaceTokenKind"/> / <see cref="ToolSurfaceTokenKindStrategy"/> 只能以「逐值镜像」
/// 存在。镜像漂移不会编译报错，只表现为 golden 逐字节漂移或令牌身份错判——必须由反射比对机械锁定。
/// </remarks>
public class ToolSurfaceContractTests
{
    // ────────── 镜像枚举契约 ──────────

    [Fact]
    public void TokenKindMirror_MustMatchRuntimeEnum_MemberNamesAndValues()
    {
        var runtime = typeof(SdkTokenKind);
        var mirror = typeof(ToolSurfaceTokenKind);

        var runtimeMembers = Enum.GetValues(runtime).Cast<System.Enum>()
            .ToDictionary(static m => m.ToString(), static m => Convert.ToInt32(m), StringComparer.Ordinal);
        var mirrorMembers = Enum.GetValues(mirror).Cast<System.Enum>()
            .ToDictionary(static m => m.ToString(), static m => Convert.ToInt32(m), StringComparer.Ordinal);

        mirrorMembers.Keys.Should().BeEquivalentTo(runtimeMembers.Keys,
            "引擎侧令牌身份镜像的成员名必须与 Mud.AI.SdkTokenKind 完全一致（改名即 golden 漂移）");

        foreach (var name in runtimeMembers.Keys)
        {
            mirrorMembers[name].Should().Be(runtimeMembers[name],
                $"镜像成员 {name} 的数值必须与运行时枚举一致（引擎按 int 读取特性槽）");
        }
    }

    [Fact]
    public void TokenKindMirror_ToLiteral_MustMatchRuntimeContract()
    {
        foreach (var kind in Enum.GetValues(typeof(SdkTokenKind)).Cast<SdkTokenKind>())
        {
            var mirrorKind = (ToolSurfaceTokenKind)(int)kind;
            ToolSurfaceTokenKindContract.ToLiteral(mirrorKind)
                .Should().Be(SdkTokenKindContract.ToLiteral(kind),
                    $"令牌身份 {kind} 的渲染字面量必须与运行时契约同表（进 golden 与 x-{{sdk}} 扩展块）");
        }
    }

    /// <summary>
    /// <c>InterfaceIdentity</c> 的<b>逐字段镜像</b>守卫（设计文档 §4.3）：引擎侧
    /// <see cref="ToolSurfaceInterfaceIdentity"/> 的属性名与类型必须与运行时契约逐项一致——
    /// 镜像漂移不会编译报错（两份类型各自独立），只会在接线时表现为字段读错/丢失。
    /// </summary>
    [Fact]
    public void InterfaceIdentityMirror_MustMatchRuntimeContract_MemberNamesAndTypes()
    {
        var runtime = typeof(InterfaceIdentity).GetProperties()
            .ToDictionary(static p => p.Name, static p => DescribeMirroredType(p.PropertyType), StringComparer.Ordinal);
        var mirror = typeof(ToolSurfaceInterfaceIdentity).GetProperties()
            .ToDictionary(static p => p.Name, static p => DescribeMirroredType(p.PropertyType), StringComparer.Ordinal);

        runtime.Should().NotBeEmpty("守卫自身必须能反射到运行时契约，否则比对失效");
        mirror.Keys.Should().BeEquivalentTo(runtime.Keys,
            "引擎侧 InterfaceIdentity 镜像的字段集必须与 Mud.AI.InterfaceIdentity 完全一致");

        foreach (var name in runtime.Keys)
        {
            mirror[name].Should().Be(runtime[name],
                $"镜像字段 {name} 的类型必须与运行时契约一致（Version 是 int?，不是 string）");
        }
    }

    /// <summary>
    /// 类型描述归一：令牌身份枚举在两侧是<b>两个程序集内的镜像枚举</b>，按「同一身份轴」折算。
    /// </summary>
    private static string DescribeMirroredType(System.Type type)
        => type == typeof(SdkTokenKind) || type == typeof(ToolSurfaceTokenKind)
            ? "SdkTokenKind"
            : type.FullName ?? type.Name;

    [Fact]
    public void TokenKindStrategyMirror_MustMatchRuntimeEnum()
    {
        var runtime = typeof(TokenKindDerivationStrategy);
        var mirror = typeof(ToolSurfaceTokenKindStrategy);

        var runtimeMembers = Enum.GetValues(runtime).Cast<System.Enum>()
            .ToDictionary(static m => m.ToString(), static m => Convert.ToInt32(m), StringComparer.Ordinal);
        var mirrorMembers = Enum.GetValues(mirror).Cast<System.Enum>()
            .ToDictionary(static m => m.ToString(), static m => Convert.ToInt32(m), StringComparer.Ordinal);

        mirrorMembers.Should().Equal(runtimeMembers);
    }

    // ────────── 剖面槽位面契约（"加了槽但没进值对象/值相等"的静默漂移） ──────────

    /// <summary>
    /// 剖面的公开槽位面必须在「<c>[SdkToolProfile]</c> 特性」与「引擎值对象 <see cref="SdkToolProfileModel"/>」两侧同构。
    /// </summary>
    /// <remarks>
    /// <b>失败模式是静默的</b>：只在一侧加槽不会编译失败——值对象读不到该槽 ⇒ 该槽永远不会影响产物
    /// （特性里写了、golden 里看不出），且没有其它守卫覆盖这条缝。故按<b>属性名集合</b>镜像锁定
    /// （类型刻意不比对：值对象侧集合槽用 <c>ImmutableArray</c> 承载、特性侧是分隔串）。
    /// </remarks>
    [Fact]
    public void ProfileSlotSurface_AttributeProperties_ShouldMirrorModelProperties()
    {
        // DeclaredOnly：排除 System.Attribute 继承来的 TypeId（它不是剖面槽）。
        var attributeSlots = typeof(SdkToolProfileAttribute)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(static property => property.Name)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        var modelSlots = typeof(SdkToolProfileModel)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(static property => property.Name)
            // MissingRequiredSlots 是引擎的派生诊断事实（不属于剖面作者声明的槽位面）。
            .Where(static name => name != nameof(SdkToolProfileModel.MissingRequiredSlots))
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        modelSlots.Should().Equal(
            attributeSlots,
            "槽位面必须在特性与值对象两侧同构——单侧加槽/改名不会编译失败，只表现为「槽位静默读不到」");
    }

    /// <summary>
    /// <see cref="SdkToolProfileModel"/> 的<b>每个槽</b>都必须参与 <c>Equals</c> 与 <c>GetHashCode</c>。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么必须补这条守卫</b>：本类型是<b>整条工具面扇出的增量管线入口</b>
    /// （<c>CompilationProvider.Select(ProfileDiscovery.ResolveProfiles)</c> → 全部发射路径）。
    /// 某个槽漏进值相等 ⇒「改了该槽但编译产物未变」被判定为 <c>Unchanged</c> ⇒
    /// <b>产物静默过期</b>（schema/golden 与实际剖面不一致，构建全绿）。
    /// </para>
    /// <para>
    /// 既有 <c>ToolSurfaceEqualityFieldCoverageTests</c> 只覆盖 <c>Extraction/</c> 下三个值键
    /// （<c>CapabilityEntry</c>/<c>CapabilityParameter</c>/<c>ToolSchemaModel</c>），
    /// 本类型此前<b>无守卫</b>——本用例关闭该缺口（与 AT-B16 同类失效通道）。
    /// </para>
    /// <para>
    /// 判据取"槽名出现在方法体文本中"（对标量槽与集合槽两种形态都成立：集合槽走
    /// <c>xxx.SequenceEqual(other.xxx)</c>，标量槽走 <c>xxx == other.xxx</c> /
    /// <c>GetHashCode(xxx)</c>）。源码扫描而非反射：字段是否入哈希是<b>方法体</b>事实。
    /// </para>
    /// </remarks>
    [Fact]
    public void ProfileSlots_EverySlot_ShouldParticipateInEqualityAndHash()
    {
        var path = TestRepoRoot.PathOf("Mud.AI.Generator", "ToolSurface", "SdkToolProfileModel.cs");
        var source = File.ReadAllText(path);

        // 只取 SdkToolProfileModel 自身的方法体：本文件还包含 TokenKindMarker 与 SdkToolProfileSet 的
        // 同名签名方法（`public override int GetHashCode()` 在文件里出现 3 次），不裁剪就会读到别的类型的体
        // ⇒ 槽名全部"未参与"（假红）。
        var modelTypeIndex = source.IndexOf("internal sealed class SdkToolProfileModel", StringComparison.Ordinal);
        modelTypeIndex.Should().BeGreaterThan(0, "未找到 SdkToolProfileModel 类型声明（解析面已变，请先修守卫）");
        var setTypeIndex = source.IndexOf("internal sealed class SdkToolProfileSet", StringComparison.Ordinal);
        setTypeIndex.Should().BeGreaterThan(modelTypeIndex, "SdkToolProfileSet 应声明在 SdkToolProfileModel 之后（解析面已变，请先修守卫）");
        var modelSource = source.Substring(modelTypeIndex, setTypeIndex - modelTypeIndex);

        var equalsBody = ReadMethodBody(modelSource, "public bool Equals(SdkToolProfileModel? other)");
        var hashBody = ReadMethodBody(modelSource, "public override int GetHashCode()");
        equalsBody.Should().NotBeNullOrEmpty("未找到 SdkToolProfileModel.Equals 方法体（守卫解析失效 ⇒ 假绿）");
        hashBody.Should().NotBeNullOrEmpty("未找到 SdkToolProfileModel.GetHashCode 方法体（守卫解析失效 ⇒ 假绿）");

        // 金丝雀：确认读到的是"对的方法体"而不是同名的其它类型的体（否则判据退化为必红/假绿）。
        equalsBody.Should().Contain("other.", "读到的不是 SdkToolProfileModel.Equals 的体");
        hashBody.Should().Contain("_hashCodeComputed", "读到的不是 SdkToolProfileModel.GetHashCode 的体");

        var slots = typeof(SdkToolProfileAttribute)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(static property => property.Name)
            .ToArray();
        slots.Should().NotBeEmpty("守卫自身必须能反射到槽位面，否则比对失效");

        var missingFromEquals = slots
            .Where(slot => !equalsBody.Contains(slot, StringComparison.Ordinal)).ToArray();
        var missingFromHash = slots
            .Where(slot => !hashBody.Contains(slot, StringComparison.Ordinal)).ToArray();

        missingFromEquals.Should().BeEmpty(
            "以下槽未参与 Equals —— 改动它们不会让增量管线重算，产物静默过期：{0}",
            string.Join(", ", missingFromEquals));

        missingFromHash.Should().BeEmpty(
            "以下槽未参与 GetHashCode —— 仅在该槽上不同的两个剖面会得到相同哈希：{0}",
            string.Join(", ", missingFromHash));
    }

    /// <summary>按签名定位方法并返回其方法体（花括号配平；支持表达式体成员）。</summary>
    private static string ReadMethodBody(string source, string signaturePrefix)
    {
        var start = source.IndexOf(signaturePrefix, StringComparison.Ordinal);
        if (start < 0)
        {
            return string.Empty;
        }

        var open = source.IndexOf('{', start);
        var semicolon = source.IndexOf(';', start);
        if (semicolon >= 0 && (open < 0 || semicolon < open))
        {
            return source.Substring(start, semicolon - start + 1);
        }

        if (open < 0)
        {
            return string.Empty;
        }

        var depth = 0;
        for (var i = open; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source.Substring(open, i - open + 1);
                }
            }
        }

        return string.Empty;
    }

    // ────────── 诊断槽位表契约 ──────────

    [Fact]
    public void DiagnosticSlots_AreUniqueAndContiguousWithUpstreamCanonicalNumbers()
    {
        var slots = ToolSurfaceDiagnostics.Slots.Select(static s => s.Slot).ToList();

        slots.Distinct().Should().HaveCount(slots.Count, "槽位号必须唯一（拼入诊断 ID 后决定档位）");
        slots.Should().BeInAscendingOrder();

        // 上游 AT-B14 清理后的僵尸位不得复活；026（兜底）与 027（派生常量冲突）必须存在。
        slots.Should().NotContain(7, "007 已在上游清理，本表不复活");
        slots.Should().NotContain(12, "012 已在上游清理，本表不复活");
        slots.Should().NotContain(13, "013 已在上游清理，本表不复活");
        slots.Should().Contain(ToolSurfaceDiagnostics.SlotGeneratorInternalError);
        slots.Should().Contain(ToolSurfaceDiagnostics.SlotDerivedConstantNameConflict);

        // per-tool affordance 四槽（F-7）：028/029/030（Error 零容忍）+ 031（Warning 基线）。
        slots.Should().Contain(ToolSurfaceDiagnostics.SlotAffordanceStructure);
        slots.Should().Contain(ToolSurfaceDiagnostics.SlotAffordanceOrphan);
        slots.Should().Contain(ToolSurfaceDiagnostics.SlotAffordanceDanglingReference);
        slots.Should().Contain(ToolSurfaceDiagnostics.SlotAffordanceBudget);
    }

    [Fact]
    public void ZeroToleranceSlots_ExcludeWarningAndInfoLevels()
    {
        // v2.1 修正：009/018/021 非零容忍。把截断告警升级为构建阻断会让消费方无法发布。
        var zero = ToolSurfaceDiagnostics.ZeroToleranceSlots;

        zero.Should().NotContain(ToolSurfaceDiagnostics.SlotOutputSchemaTruncation);
        zero.Should().NotContain(ToolSurfaceDiagnostics.SlotCapabilityCoverageReport);
        zero.Should().NotContain(ToolSurfaceDiagnostics.SlotRequiredNullableConflict);

        // affordance 的结构/归属/引用是"策展错误"（零容忍），预算是"内容偏胖"（Warning，走基线拦增量）。
        zero.Should().Contain(ToolSurfaceDiagnostics.SlotAffordanceStructure);
        zero.Should().Contain(ToolSurfaceDiagnostics.SlotAffordanceOrphan);
        zero.Should().Contain(ToolSurfaceDiagnostics.SlotAffordanceDanglingReference);
        zero.Should().NotContain(ToolSurfaceDiagnostics.SlotAffordanceBudget,
            "把 token 预算告警升级为构建阻断会使消费方无法发布（与 009/021 同口径）");

        foreach (var slot in zero)
        {
            var definition = ToolSurfaceDiagnostics.Slots.First(s => s.Slot == slot);
            definition.Severity.Should().Be(DiagnosticSeverity.Error,
                $"零容忍槽位 {slot} 必须是 Error 级，否则消费方 .editorconfig 升级口径无意义");
        }
    }

    [Fact]
    public void DiagnosticId_UsesProfilePrefixAndThreeDigitSlot()
    {
        var profile = ResolveSingleProfile("""
            [SdkToolProfile("Feishu",
                ToolAttributeName = "FeishuTool",
                ToolAttributeNamespace = "Mud.Feishu.AI.Tools",
                SdkNamespaceRoot = "Mud.Feishu",
                ProductPrefix = "FeishuTool",
                DiagnosticPrefix = "MUDFT",
                DiagnosticCategory = "MudFeishu.AI")]
            sealed class FeishuProfile : ISdkToolProfile { }
            """);

        profile.DiagnosticId(1).Should().Be("MUDFT001");
        profile.DiagnosticId(26).Should().Be("MUDFT026");
        profile.DiagnosticId(9).Should().Be("MUDFT009");
    }

    [Fact]
    public void Factory_BakesProfileSpecificIds_AndFallbackSlotUsesToolingCategory()
    {
        var profile = ResolveSingleProfile("""
            [SdkToolProfile("Wechat",
                ToolAttributeName = "WechatTool",
                ToolAttributeNamespace = "Mud.Wechat.AI.Tools",
                SdkNamespaceRoot = "Mud.Wechat",
                ProductPrefix = "WechatTool",
                DiagnosticPrefix = "MUDWX",
                DiagnosticCategory = "MudWechat.AI",
                ToolingDiagnosticCategory = "MudWechat.Tooling")]
            sealed class WechatProfile : ISdkToolProfile { }
            """);

        var factory = ToolSurfaceDiagnostics.For(profile);

        factory[ToolSurfaceDiagnostics.SlotToolNameConflict].Id.Should().Be("MUDWX003");
        factory[ToolSurfaceDiagnostics.SlotToolNameConflict].Category.Should().Be("MudWechat.AI");

        // 兜底槽类别例外（§6.1 v2.1 实测）：上游 MUDFT026 = MudFeishu.Tooling。
        factory[ToolSurfaceDiagnostics.SlotGeneratorInternalError].Id.Should().Be("MUDWX026");
        factory[ToolSurfaceDiagnostics.SlotGeneratorInternalError].Category.Should().Be("MudWechat.Tooling");

        // 同剖面必须复用同一工厂实例（描述符缓存跨扫描复用）。
        ToolSurfaceDiagnostics.For(profile).Should().BeSameAs(factory);
    }

    [Fact]
    public void Factory_UnknownSlot_Throws()
    {
        var profile = ResolveSingleProfile("""
            [SdkToolProfile("Test",
                ToolAttributeName = "TestTool",
                ToolAttributeNamespace = "Test.Tools",
                SdkNamespaceRoot = "Test.Sdk",
                ProductPrefix = "TestTool",
                DiagnosticPrefix = "MUDTT",
                DiagnosticCategory = "Test.AI")]
            sealed class TestProfile : ISdkToolProfile { }
            """);

        var factory = ToolSurfaceDiagnostics.For(profile);

        // 007/012/013 是已删除的僵尸位：越界访问必须抛，而非静默产出错误描述符。
        Action act = () => _ = factory[7];
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ────────── 剖面模型解析 ──────────

    [Fact]
    public void ResolveProfiles_ValidProfile_PopulatesAllSlotsAndNoMissing()
    {
        var profile = ResolveSingleProfile("""
            [SdkToolProfile("Feishu",
                ToolAttributeName = "FeishuTool",
                ToolAttributeNamespace = "Mud.Feishu.AI.Tools",
                ToolHandlerAttributeName = "FeishuToolHandler",
                ToolHandlerAttributeNamespace = "Mud.Feishu.AI.FeishuTools",
                ParameterAttributeName = "ToolParameter",
                SdkNamespaceRoot = "Mud.Feishu",
                InterfaceNameRegex = "^IFeishu(?<domain>Tenant|User)?V(?<version>\d+)(?<resource>.+)$",
                TokenKindStrategy = TokenKindDerivationStrategy.NamePrefix,
                TokenKindMarkers = "IFeishuTenant=Tenant;IFeishuUser=User",
                ProductPrefix = "FeishuTool",
                ProductPluralPrefix = "FeishuTools",
                DiagnosticPrefix = "MUDFT",
                DiagnosticCategory = "MudFeishu.AI",
                WriteVerbKeywords = "delete|update|create",
                SchemaExtensionKey = "x-feishu",
                OwnerAssembly = "Mud.Feishu.AI.FeishuTools",
                GeneratedNamespace = "Mud.Feishu.AI.Tools.Generated",
                ContractNamespace = "Mud.Feishu.AI.FeishuTools",
                RegistrationNamespace = "Mud.Feishu.AI.FeishuTools.Registration",
                RiskEnumFullName = "Mud.Feishu.AI.Tools.FeishuToolRisk",
                ResultTypeName = "FeishuToolResult",
                BindingTypeName = "FeishuToolBinding",
                OutputWrapperNamespace = "Mud.Feishu.DataModels",
                OutputWrapperTypeNames = "FeishuApiResult|FeishuApiPageListResult",
                SdkInterfacePrefix = "IFeishu",
                CapabilityCatalogPropertyName = "FeishuToolCatalog",
                GoldenFileName = "FeishuToolSchemas.golden.txt",
                GoldenUpdatePropertyName = "FeishuToolGoldenUpdate",
                GuidanceDirectory = "/Guidance/")]
            sealed class FeishuProfile : ISdkToolProfile { }
            """);

        profile.MissingRequiredSlots.Should().BeEmpty();
        profile.Name.Should().Be("Feishu");
        profile.ToolAttributeName.Should().Be("FeishuTool");
        profile.TokenKindStrategy.Should().Be(ToolSurfaceTokenKindStrategy.NamePrefix);
        profile.WriteVerbKeywords.Should().Equal("delete", "update", "create");
        profile.OutputWrapperTypeNames.Should().Equal("FeishuApiResult", "FeishuApiPageListResult");
        profile.GoldenFileName.Should().Be("FeishuToolSchemas.golden.txt");

        // TokenKindMarkers 解析产物。
        profile.TokenKindMarkers.Should().HaveCount(2);
        profile.TokenKindMarkers[0].Marker.Should().Be("IFeishuTenant");
        profile.TokenKindMarkers[0].Kind.Should().Be(ToolSurfaceTokenKind.Tenant);
        profile.TokenKindMarkers[0].OverrideMode.Should().Be(ToolSurfaceTokenKindStrategy.Unspecified);
        profile.TokenKindMarkers[1].Marker.Should().Be("IFeishuUser");
        profile.TokenKindMarkers[1].Kind.Should().Be(ToolSurfaceTokenKind.User);

        // 兜底类别缺省为引擎固定值。
        profile.ToolingDiagnosticCategory.Should().Be("Mud.AI.Tooling");
    }

    [Fact]
    public void TokenKindMarkers_PerEntryModeOverride_SupportsWechatDualForm()
    {
        // 微信「后缀 + 中缀双形态并存」（§3 v2.1 实测）：逐条覆写匹配形态。
        var profile = ResolveSingleProfile("""
            [SdkToolProfile("Wechat",
                ToolAttributeName = "WechatTool",
                ToolAttributeNamespace = "Mud.Wechat.AI.Tools",
                SdkNamespaceRoot = "Mud.Wechat",
                ProductPrefix = "WechatTool",
                DiagnosticPrefix = "MUDWX",
                DiagnosticCategory = "MudWechat.AI",
                TokenKindStrategy = TokenKindDerivationStrategy.SuffixMarker,
                TokenKindMarkers = "suffix:_Provider=ThirdParty;infix:Provider=ThirdParty;prefix:Internal=Internal")]
            sealed class WechatProfile : ISdkToolProfile { }
            """);

        profile.TokenKindMarkers.Should().HaveCount(3);
        profile.TokenKindMarkers[0].Should().BeEquivalentTo(new
        {
            Marker = "_Provider",
            Kind = ToolSurfaceTokenKind.ThirdParty,
            OverrideMode = ToolSurfaceTokenKindStrategy.SuffixMarker,
        });
        profile.TokenKindMarkers[1].Marker.Should().Be("Provider");
        profile.TokenKindMarkers[1].OverrideMode.Should().Be(ToolSurfaceTokenKindStrategy.InfixMarker);
        profile.TokenKindMarkers[2].Marker.Should().Be("Internal");
        profile.TokenKindMarkers[2].OverrideMode.Should().Be(ToolSurfaceTokenKindStrategy.NamePrefix);
    }

    [Fact]
    public void TokenKindMarkers_MalformedEntries_AreSkippedSilently()
    {
        // 结构性错误由 SDKT002 的必填槽守卫兜住；标记表只接受成员名形态的 Kind。
        var profile = ResolveSingleProfile("""
            [SdkToolProfile("Test",
                ToolAttributeName = "TestTool",
                ToolAttributeNamespace = "Test.Tools",
                SdkNamespaceRoot = "Test.Sdk",
                ProductPrefix = "TestTool",
                DiagnosticPrefix = "MUDTT",
                DiagnosticCategory = "Test.AI",
                TokenKindMarkers = "=Tenant;IFeishuTenant=;NoSeparator;Good=User;Numeric=5;Undefined=999;WrongCase=tenant")]
            sealed class TestProfile : ISdkToolProfile { }
            """);

        // Numeric=5 / Undefined=999 必须被拒（Enum.TryParse 对数值串返回 true，回环校验钉死）。
        profile.TokenKindMarkers.Should().ContainSingle()
            .Which.Marker.Should().Be("Good");
    }

    [Fact]
    public void MissingRequiredSlots_ListsEveryBlankRequiredSlot()
    {
        var profile = ResolveSingleProfile("""
            [SdkToolProfile("Partial")]
            sealed class PartialProfile : ISdkToolProfile { }
            """);

        // Name 由构造参数提供，不在缺失列；其余 6 个必填槽全空。
        profile.MissingRequiredSlots.Should().Equal(
            "ToolAttributeName",
            "ToolAttributeNamespace",
            "SdkNamespaceRoot",
            "ProductPrefix",
            "DiagnosticPrefix",
            "DiagnosticCategory");
        profile.Name.Should().Be("Partial");
    }

    [Fact]
    public void ResolveProfiles_SortsByNameOrdinalAndDeduplicatesSameName()
    {
        // 同名去重防 hintName 撞名；排序保证多剖面扇出顺序确定（产物发射顺序可复现）。
        var profiles = ResolveProfiles("""
            [SdkToolProfile("Zebra", ToolAttributeName = "ZTool", ToolAttributeNamespace = "Z", SdkNamespaceRoot = "Z", ProductPrefix = "ZTool", DiagnosticPrefix = "MUDZ", DiagnosticCategory = "Z.AI")]
            sealed class ZebraProfile : ISdkToolProfile { }

            [SdkToolProfile("Alpha", ToolAttributeName = "ATool", ToolAttributeNamespace = "A", SdkNamespaceRoot = "A", ProductPrefix = "ATool", DiagnosticPrefix = "MUDA", DiagnosticCategory = "A.AI")]
            sealed class AlphaProfile : ISdkToolProfile { }

            [SdkToolProfile("Alpha", ToolAttributeName = "ATool2", ToolAttributeNamespace = "A2", SdkNamespaceRoot = "A2", ProductPrefix = "ATool2", DiagnosticPrefix = "MUDA2", DiagnosticCategory = "A2.AI")]
            sealed class AlphaDuplicate : ISdkToolProfile { }
            """);

        profiles.Select(static p => p.Name).Should().Equal("Alpha", "Zebra");
        profiles.Select(static p => p.ToolAttributeName).Should().Equal("ATool", "ZTool");
    }

    [Fact]
    public void ResolveProfiles_IgnoresUnpairedAndForeignShapes()
    {
        // 缺特性半边交 SDKT001；异型特性（同名判定不成立）不得误命中。
        var profiles = ResolveProfiles("""
            sealed class OnlyInterface : ISdkToolProfile { }

            [SdkToolProfile("Valid", ToolAttributeName = "VTool", ToolAttributeNamespace = "V", SdkNamespaceRoot = "V", ProductPrefix = "VTool", DiagnosticPrefix = "MUDV", DiagnosticCategory = "V.AI")]
            sealed class ValidProfile : ISdkToolProfile { }

            [Obsoleted]
            sealed class ForeignAttributeShape : ISdkToolProfile { }

            [System.Obsolete]
            sealed class NotAProfile { }

            public class ObsoletedAttribute : System.Attribute { }
            """);

        profiles.Should().ContainSingle().Which.Name.Should().Be("Valid");
    }

    [Fact]
    public void ResolveProfiles_WithoutInterfaceReference_ReturnsEmpty()
    {
        // 无 Abstractions 引用时接口查找即返回空（不进入类型枚举）。
        var compilation = Compile("public class Plain { }");
        ProfileDiscovery.ResolveProfiles(compilation).Should().BeEmpty();
    }

    [Fact]
    public void ResolveProfiles_SameCompilationInstance_SharesSingleResolution()
    {
        // P2-1 回归：同一 Compilation 实例上 profiles 节点与全部 ScanTool/ScanHandler 变换
        // 必须共享一次全程序集枚举——缓存命中时返回同一底层数组实例（ImmutableArray 引用相等）。
        const string source = """
            [SdkToolProfile("CacheProbe",
                ToolAttributeName = "CacheProbeTool",
                ToolAttributeNamespace = "C",
                SdkNamespaceRoot = "C",
                ProductPrefix = "CacheProbeTool",
                DiagnosticPrefix = "MUDCP",
                DiagnosticCategory = "C.AI")]
            sealed class CacheProbeProfile : ISdkToolProfile { }
            """;

        var compilation = Compile(ProfileHeader + source);
        var first = ProfileDiscovery.ResolveProfiles(compilation);
        var second = ProfileDiscovery.ResolveProfiles(compilation);

        first.Should().NotBeEmpty();
        second.Equals(first).Should().BeTrue("同一 Compilation 的重复解析必须命中缓存（共享一次枚举）");
    }

    [Fact]
    public void ResolveProfiles_DifferentCompilationInstances_ResolveIndependently()
    {
        // 增量失效语义：不同 Compilation（IDE 每次击键）必须重新解析，不得跨编译复用陈旧剖面。
        const string source = """
            [SdkToolProfile("CacheProbe",
                ToolAttributeName = "CacheProbeTool",
                ToolAttributeNamespace = "C",
                SdkNamespaceRoot = "C",
                ProductPrefix = "CacheProbeTool",
                DiagnosticPrefix = "MUDCP",
                DiagnosticCategory = "C.AI")]
            sealed class CacheProbeProfile : ISdkToolProfile { }
            """;

        var first = ProfileDiscovery.ResolveProfiles(Compile(ProfileHeader + source));
        var second = ProfileDiscovery.ResolveProfiles(Compile(ProfileHeader + source));

        first.Should().NotBeEmpty();
        second.Equals(first).Should().BeFalse("不同 Compilation 必须独立解析（不得复用陈旧结果）");
    }

    [Fact]
    public void ProfileModel_IsValueEqualAcrossSeparateResolutions()
    {
        // 增量缓存前提：同一剖面的两次解析必须值相等（引用相等会让每条下游路径每次编辑重跑）。
        const string source = """
            [SdkToolProfile("Value",
                ToolAttributeName = "ValueTool",
                ToolAttributeNamespace = "V",
                SdkNamespaceRoot = "V",
                ProductPrefix = "ValueTool",
                DiagnosticPrefix = "MUDVV",
                DiagnosticCategory = "V.AI",
                TokenKindMarkers = "prefix:A=Tenant;B=User",
                WriteVerbKeywords = "delete|create")]
            sealed class ValueProfile : ISdkToolProfile { }
            """;

        var first = ResolveSingleProfile(source);
        var second = ResolveSingleProfile(source);

        first.Should().NotBeSameAs(second);
        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    // ────────── SDKT001 剖面成对守卫 ──────────

    [Fact]
    public void ContractGuard_InterfaceWithoutAttribute_ReportsSdkT001()
    {
        var diagnostics = RunGuard("""
            using Mud.AI;

            sealed class BareProfile : ISdkToolProfile { }
            """);

        var sdkT001 = diagnostics.Where(static d => d.Id == DiagnosticIds.SdkToolProfileContractViolation).ToList();
        sdkT001.Should().ContainSingle();
        sdkT001[0].Severity.Should().Be(DiagnosticSeverity.Error);
        sdkT001[0].GetMessage().Should().Contain("缺少 [SdkToolProfile]");
    }

    [Fact]
    public void ContractGuard_AttributeWithoutInterface_ReportsSdkT001()
    {
        var diagnostics = RunGuard("""
            using Mud.AI.Attributes;

            [SdkToolProfile("Misplaced", ToolAttributeName = "MTool")]
            sealed class MisplacedProfile { }
            """);

        var sdkT001 = diagnostics.Where(static d => d.Id == DiagnosticIds.SdkToolProfileContractViolation).ToList();
        sdkT001.Should().ContainSingle();
        sdkT001[0].GetMessage().Should().Contain("未实现 ISdkToolProfile");
    }

    [Fact]
    public void ContractGuard_PairedProfile_ReportsNothing()
    {
        var diagnostics = RunGuard(ValidPairedSource);

        diagnostics.Should().NotContain(static d => d.Id == DiagnosticIds.SdkToolProfileContractViolation);
    }

    [Fact]
    public void ContractGuard_UnrelatedTypes_ReportsNothing()
    {
        var diagnostics = RunGuard("""
            using Mud.AI;
            using Mud.AI.Attributes;

            public class PlainService { }

            [System.Obsolete]
            public sealed class Flagged { }
            """);

        diagnostics.Should().NotContain(static d => d.Id == DiagnosticIds.SdkToolProfileContractViolation);
    }

    [Fact]
    public void ContractGuard_ReportLocation_PointsAtTheMissingSide()
    {
        // 缺特性 → 报类标识符（补特性的位置）；缺接口 → 报基列表（补 : ISdkToolProfile 的位置）。
        var missingAttribute = RunGuard("""
            using Mud.AI;

            sealed class BareProfile : ISdkToolProfile { }
            """).First(static d => d.Id == DiagnosticIds.SdkToolProfileContractViolation);

        var attributeLine = missingAttribute.Location.GetLineSpan().StartLinePosition.Line;
        attributeLine.Should().Be(2, "缺特性时应定位到类标识符所在行");

        var missingInterface = RunGuard("""
            using Mud.AI;
            using Mud.AI.Attributes;

            [SdkToolProfile("Misplaced")]
            sealed class MisplacedProfile : System.IDisposable
            {
                public void Dispose() { }
            }
            """).First(static d => d.Id == DiagnosticIds.SdkToolProfileContractViolation);

        var baseListLine = missingInterface.Location.GetLineSpan().StartLinePosition.Line;
        baseListLine.Should().Be(4, "缺接口实现时应定位到基列表（: System.IDisposable）所在行");
    }

    // ────────── 测试辅助 ──────────

    private const string ValidPairedSource = """
        using Mud.AI;
        using Mud.AI.Attributes;

        [SdkToolProfile("Paired",
            ToolAttributeName = "PairTool",
            ToolAttributeNamespace = "P.Tools",
            SdkNamespaceRoot = "P.Sdk",
            ProductPrefix = "PairTool",
            DiagnosticPrefix = "MUDPP",
            DiagnosticCategory = "P.AI")]
        sealed class PairedProfile : ISdkToolProfile { }
        """;

    /// <summary>剖面声明体（using 头由 <see cref="ProfileFile"/> 提供）。</summary>
    private const string ProfileHeader = """
        using Mud.AI;
        using Mud.AI.Attributes;

        """;

    private static SdkToolProfileModel ResolveSingleProfile(string body)
    {
        var profiles = ResolveProfiles(body);
        profiles.Should().ContainSingle("用例只应声明一个剖面");
        return profiles[0];
    }

    private static ImmutableArray<SdkToolProfileModel> ResolveProfiles(string body)
    {
        var compilation = Compile(ProfileHeader + body);
        return ProfileDiscovery.ResolveProfiles(compilation);
    }

    private static ImmutableArray<Diagnostic> RunGuard(string source)
    {
        var compilation = Compile(source);
        var withAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new ProfileContractGuardAnalyzer()));
        return withAnalyzers.GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();
    }

    private static CSharpCompilation Compile(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        return CSharpCompilation.Create(
            "ToolSurfaceContractTest",
            new[] { tree },
            BasicReferenceAssemblies.GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}