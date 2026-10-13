// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mud.AI.ToolSurface;

/// <summary>
/// 令牌身份推导策略的<b>引擎侧镜像</b>（值必须与
/// <c>Mud.AI.Attributes.TokenKindDerivationStrategy</c> 逐值一致）。
/// </summary>
/// <remarks>
/// 工程纪律 §1.2-3 禁止生成器引用任何 <c>Mud.*</c> 运行时程序集，故引擎按 <c>int</c> 读取枚举槽
/// 并映射到本镜像；由 <c>ToolSurfaceContractTests</c> 反射比对两枚举的成员名与数值，机械锁定镜像一致。
/// </remarks>
internal enum ToolSurfaceTokenKindStrategy
{
    Unspecified = 0,
    NamePrefix = 1,
    SuffixMarker = 2,
    InfixMarker = 3,
}

/// <summary>
/// 令牌身份枚举的<b>引擎侧镜像</b>（值与渲染字面量必须与
/// <c>Mud.AI.SdkTokenKind</c> / <c>SdkTokenKindContract.ToLiteral</c> 逐成员一致）。
/// </summary>
/// <remarks>
/// 渲染字面量进 golden 与 <c>x-{{sdk}}</c> 扩展块，是逐字节契约的一部分；镜像漂移即 golden 漂移，
/// 由 <c>ToolSurfaceContractTests</c> 的逐成员比对守卫。
/// </remarks>
internal enum ToolSurfaceTokenKind
{
    Unspecified = 0,
    Tenant = 1,
    User = 2,
    ThirdParty = 3,
    Internal = 4,
    Both = 5,
}

/// <summary>引擎侧令牌身份 → 序列化字面量（与 <c>SdkTokenKindContract.ToLiteral</c> 同表）。</summary>
internal static class ToolSurfaceTokenKindContract
{
    public static string ToLiteral(ToolSurfaceTokenKind kind) => kind switch
    {
        ToolSurfaceTokenKind.Tenant => "tenant",
        ToolSurfaceTokenKind.User => "user",
        ToolSurfaceTokenKind.ThirdParty => "third_party",
        ToolSurfaceTokenKind.Internal => "internal",
        ToolSurfaceTokenKind.Both => "both",
        _ => "unspecified",
    };
}

/// <summary>
/// 「标记串 → 令牌身份」表条目（<c>TokenKindMarkers</c> 槽的解析产物）。
/// </summary>
/// <remarks>
/// 不采用 <c>record</c>：netstandard2.0 目标未注入 <c>IsExternalInit</c>（仓库纪律，
/// 见 <c>GeneratorConfigSnapshot</c> 同例）。
/// </remarks>
internal sealed class TokenKindMarker
{
    public TokenKindMarker(string marker, ToolSurfaceTokenKind kind, ToolSurfaceTokenKindStrategy overrideMode)
    {
        Marker = marker;
        Kind = kind;
        OverrideMode = overrideMode;
    }

    /// <summary>命名标记串（如 <c>IFeishuTenant</c> / <c>_Provider</c>）。</summary>
    public string Marker { get; }

    /// <summary>归一化令牌身份。</summary>
    public ToolSurfaceTokenKind Kind { get; }

    /// <summary>标记串的匹配形态；<see cref="ToolSurfaceTokenKindStrategy.Unspecified"/>
    /// 表示按 profile 的 <see cref="SdkToolProfileModel.TokenKindStrategy"/> 默认形态匹配。
    /// 微信「后缀 + 中缀双形态并存」（设计文档 §3 v2.1 实测）即靠逐条覆写实现。</summary>
    public ToolSurfaceTokenKindStrategy OverrideMode { get; }

    public bool Equals(TokenKindMarker? other)
        => other is not null
            && string.Equals(Marker, other.Marker, StringComparison.Ordinal)
            && Kind == other.Kind
            && OverrideMode == other.OverrideMode;

    public override bool Equals(object? obj) => Equals(obj as TokenKindMarker);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(Marker);
            hash = (hash * 31) + (int)Kind;
            hash = (hash * 31) + (int)OverrideMode;
            return hash;
        }
    }
}

/// <summary>
/// 从 <c>[SdkToolProfile]</c> 的 <see cref="AttributeData"/> 读出的编译期剖面值对象
/// （设计文档 §4.1：引擎唯一消费的剖面载体）。
/// </summary>
/// <remarks>
/// <para>
/// 全部槽位均为编译期字符串常量（生成器无法执行用户代码）；解析失败/缺槽不抛异常，
/// 而是记入 <see cref="MissingRequiredSlots"/>，由入口生成器上报 <c>SDKT002</c>（引擎固定诊断）。
/// </para>
/// <para>
/// 实现 <see cref="IEquatable{T}"/> 全字段值相等：本类型进入增量管道
/// （<c>IncrementalValuesProvider</c>），引用相等会让每条下游路径在每次编辑后重跑。
/// </para>
/// </remarks>
internal sealed class SdkToolProfileModel : IEquatable<SdkToolProfileModel>
{
    /// <summary>必填槽名（缺失即该剖面不可用，报 SDKT002）。</summary>
    private static readonly string[] RequiredSlotNames =
    [
        nameof(Name),
        nameof(ToolAttributeName),
        nameof(ToolAttributeNamespace),
        nameof(SdkNamespaceRoot),
        nameof(ProductPrefix),
        nameof(DiagnosticPrefix),
        nameof(DiagnosticCategory),
    ];

    private int _hashCode;
    private bool _hashCodeComputed;

    private SdkToolProfileModel()
    {
        Name = string.Empty;
        ToolAttributeName = string.Empty;
        ToolAttributeNamespace = string.Empty;
        ToolHandlerAttributeName = string.Empty;
        ToolHandlerAttributeNamespace = string.Empty;
        ParameterAttributeName = string.Empty;
        SdkNamespaceRoot = string.Empty;
        InterfaceNameRegex = string.Empty;
        TokenKindMarkers = ImmutableArray<TokenKindMarker>.Empty;
        ProductPrefix = string.Empty;
        ProductPluralPrefix = string.Empty;
        DiagnosticPrefix = string.Empty;
        DiagnosticCategory = string.Empty;
        ToolingDiagnosticCategory = string.Empty;
        WriteVerbKeywords = ImmutableArray<string>.Empty;
        SchemaExtensionKey = string.Empty;
        OwnerAssembly = string.Empty;
        GeneratedNamespace = string.Empty;
        ContractNamespace = string.Empty;
        RegistrationNamespace = string.Empty;
        RiskEnumFullName = string.Empty;
        ResultTypeName = string.Empty;
        BindingTypeName = string.Empty;
        OutputWrapperNamespace = string.Empty;
        OutputWrapperTypeNames = ImmutableArray<string>.Empty;
        SdkInterfacePrefix = string.Empty;
        CapabilityCatalogPropertyName = string.Empty;
        GoldenFileName = string.Empty;
        GoldenUpdatePropertyName = string.Empty;
        GuidanceDirectory = string.Empty;
        AffordanceDirectory = string.Empty;
        MissingRequiredSlots = ImmutableArray<string>.Empty;
    }

    /// <summary>SDK 名（<c>Feishu</c> / <c>Wechat</c>），同一编译内唯一。</summary>
    public string Name { get; private set; }

    // ────────── 1. 特性识别 ──────────

    public string ToolAttributeName { get; private set; }
    public string ToolAttributeNamespace { get; private set; }
    public string ToolHandlerAttributeName { get; private set; }
    public string ToolHandlerAttributeNamespace { get; private set; }
    public string ParameterAttributeName { get; private set; }

    // ────────── 2-4. 源解析 / 接口范式 / 令牌身份 ──────────

    public string SdkNamespaceRoot { get; private set; }
    public string InterfaceNameRegex { get; private set; }
    public ToolSurfaceTokenKindStrategy TokenKindStrategy { get; private set; }
    public ImmutableArray<TokenKindMarker> TokenKindMarkers { get; private set; }

    // ────────── 5-7. 产物前缀 / 诊断 / 危险词 ──────────

    public string ProductPrefix { get; private set; }
    public string ProductPluralPrefix { get; private set; }
    public string DiagnosticPrefix { get; private set; }
    public string DiagnosticCategory { get; private set; }
    public string ToolingDiagnosticCategory { get; private set; }
    public ImmutableArray<string> WriteVerbKeywords { get; private set; }

    // ────────── 8-11. 描述符与契约产物事实 ──────────

    public string SchemaExtensionKey { get; private set; }
    public string OwnerAssembly { get; private set; }
    public string GeneratedNamespace { get; private set; }
    public string ContractNamespace { get; private set; }
    public string RegistrationNamespace { get; private set; }
    public string RiskEnumFullName { get; private set; }

    // ────────── 12-17. 执行器 / 输出 / 聚合事实 ──────────

    public string ResultTypeName { get; private set; }
    public string BindingTypeName { get; private set; }
    public string OutputWrapperNamespace { get; private set; }
    public ImmutableArray<string> OutputWrapperTypeNames { get; private set; }
    public string SdkInterfacePrefix { get; private set; }

    // ────────── 开关与资产 ──────────

    public string CapabilityCatalogPropertyName { get; private set; }
    public string GoldenFileName { get; private set; }
    public string GoldenUpdatePropertyName { get; private set; }
    public string GuidanceDirectory { get; private set; }
    public string AffordanceDirectory { get; private set; }

    /// <summary>缺失的必填槽名清单；非空即该剖面不可用（入口报 <c>SDKT002</c>）。</summary>
    public ImmutableArray<string> MissingRequiredSlots { get; private set; }

    /// <summary>拼出该剖面的动态诊断 ID（<c>{DiagnosticPrefix}{槽位号:D3}</c>）。</summary>
    public string DiagnosticId(int slot)
        => DiagnosticPrefix + slot.ToString("D3", CultureInfo.InvariantCulture);

    /// <summary>
    /// 从 <see cref="AttributeData"/> 解析剖面模型。永不抛异常（解析失败反映在
    /// <see cref="MissingRequiredSlots"/>；类型不匹配的单槽按空串处理）。
    /// </summary>
    public static SdkToolProfileModel FromAttributeData(AttributeData attribute)
    {
        if (attribute is null)
        {
            throw new ArgumentNullException(nameof(attribute));
        }

        var model = new SdkToolProfileModel();

        // 构造参数 0 = Name
        if (attribute.ConstructorArguments.Length > 0
            && attribute.ConstructorArguments[0].Value is string name)
        {
            model.Name = name;
        }

        foreach (var named in attribute.NamedArguments)
        {
            var value = named.Value.Value as string;
            switch (named.Key)
            {
                case nameof(ToolAttributeName): model.ToolAttributeName = value ?? string.Empty; break;
                case nameof(ToolAttributeNamespace): model.ToolAttributeNamespace = value ?? string.Empty; break;
                case nameof(ToolHandlerAttributeName): model.ToolHandlerAttributeName = value ?? string.Empty; break;
                case nameof(ToolHandlerAttributeNamespace): model.ToolHandlerAttributeNamespace = value ?? string.Empty; break;
                case nameof(ParameterAttributeName): model.ParameterAttributeName = value ?? string.Empty; break;
                case nameof(SdkNamespaceRoot): model.SdkNamespaceRoot = value ?? string.Empty; break;
                case nameof(InterfaceNameRegex): model.InterfaceNameRegex = value ?? string.Empty; break;
                case nameof(TokenKindStrategy):
                    model.TokenKindStrategy = named.Value.Value is int strategy
                        ? (ToolSurfaceTokenKindStrategy)strategy
                        : ToolSurfaceTokenKindStrategy.Unspecified;
                    break;
                case nameof(TokenKindMarkers): model.TokenKindMarkers = ParseMarkers(value); break;
                case nameof(ProductPrefix): model.ProductPrefix = value ?? string.Empty; break;
                case nameof(ProductPluralPrefix): model.ProductPluralPrefix = value ?? string.Empty; break;
                case nameof(DiagnosticPrefix): model.DiagnosticPrefix = value ?? string.Empty; break;
                case nameof(DiagnosticCategory): model.DiagnosticCategory = value ?? string.Empty; break;
                case nameof(ToolingDiagnosticCategory): model.ToolingDiagnosticCategory = value ?? string.Empty; break;
                case nameof(WriteVerbKeywords): model.WriteVerbKeywords = SplitList(value); break;
                case nameof(SchemaExtensionKey): model.SchemaExtensionKey = value ?? string.Empty; break;
                case nameof(OwnerAssembly): model.OwnerAssembly = value ?? string.Empty; break;
                case nameof(GeneratedNamespace): model.GeneratedNamespace = value ?? string.Empty; break;
                case nameof(ContractNamespace): model.ContractNamespace = value ?? string.Empty; break;
                case nameof(RegistrationNamespace): model.RegistrationNamespace = value ?? string.Empty; break;
                case nameof(RiskEnumFullName): model.RiskEnumFullName = value ?? string.Empty; break;
                case nameof(ResultTypeName): model.ResultTypeName = value ?? string.Empty; break;
                case nameof(BindingTypeName): model.BindingTypeName = value ?? string.Empty; break;
                case nameof(OutputWrapperNamespace): model.OutputWrapperNamespace = value ?? string.Empty; break;
                case nameof(OutputWrapperTypeNames): model.OutputWrapperTypeNames = SplitList(value); break;
                case nameof(SdkInterfacePrefix): model.SdkInterfacePrefix = value ?? string.Empty; break;
                case nameof(CapabilityCatalogPropertyName): model.CapabilityCatalogPropertyName = value ?? string.Empty; break;
                case nameof(GoldenFileName): model.GoldenFileName = value ?? string.Empty; break;
                case nameof(GoldenUpdatePropertyName): model.GoldenUpdatePropertyName = value ?? string.Empty; break;
                case nameof(GuidanceDirectory): model.GuidanceDirectory = value ?? string.Empty; break;
                case nameof(AffordanceDirectory): model.AffordanceDirectory = value ?? string.Empty; break;
            }
        }

        // 兜底诊断类别缺省：引擎固定值（约束 0 下无需兼容旧消费方）。
        if (model.ToolingDiagnosticCategory.Length == 0)
        {
            model.ToolingDiagnosticCategory = "Mud.AI.Tooling";
        }

        var missing = ImmutableArray.CreateBuilder<string>();
        foreach (var slotName in RequiredSlotNames)
        {
            var value = slotName switch
            {
                nameof(Name) => model.Name,
                nameof(ToolAttributeName) => model.ToolAttributeName,
                nameof(ToolAttributeNamespace) => model.ToolAttributeNamespace,
                nameof(SdkNamespaceRoot) => model.SdkNamespaceRoot,
                nameof(ProductPrefix) => model.ProductPrefix,
                nameof(DiagnosticPrefix) => model.DiagnosticPrefix,
                nameof(DiagnosticCategory) => model.DiagnosticCategory,
                _ => string.Empty,
            };
            if (string.IsNullOrWhiteSpace(value))
            {
                missing.Add(slotName);
            }
        }

        model.MissingRequiredSlots = missing.ToImmutable();
        return model;
    }

    /// <summary>
    /// 解析「标记串=Kind 成员名」表（<c>;</c> 分隔）。
    /// 标记串可带 <c>prefix:</c> / <c>suffix:</c> / <c>infix:</c> 前缀覆写匹配形态
    /// （微信双形态并存即 <c>"suffix:_Provider=ThirdParty;infix:Provider=ThirdParty"</c>）。
    /// 无法识别的条目静默跳过（由 SDKT002 的必填槽守卫兜住结构性错误，不在此处报错）。
    /// </summary>
    private static ImmutableArray<TokenKindMarker> ParseMarkers(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return ImmutableArray<TokenKindMarker>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<TokenKindMarker>();
        // netstandard2.0 参考程序集的 IsNullOrWhiteSpace 无 [NotNullWhen(false)] 注解，显式窄化。
        var text = raw!;
        foreach (var entry in text.Split(';'))
        {
            var separatorIndex = entry.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex == entry.Length - 1)
            {
                continue;
            }

            var marker = entry.Substring(0, separatorIndex).Trim();
            var kindText = entry.Substring(separatorIndex + 1).Trim();

            var mode = ToolSurfaceTokenKindStrategy.Unspecified;
            if (marker.StartsWith("prefix:", StringComparison.Ordinal))
            {
                mode = ToolSurfaceTokenKindStrategy.NamePrefix;
                marker = marker.Substring("prefix:".Length);
            }
            else if (marker.StartsWith("suffix:", StringComparison.Ordinal))
            {
                mode = ToolSurfaceTokenKindStrategy.SuffixMarker;
                marker = marker.Substring("suffix:".Length);
            }
            else if (marker.StartsWith("infix:", StringComparison.Ordinal))
            {
                mode = ToolSurfaceTokenKindStrategy.InfixMarker;
                marker = marker.Substring("infix:".Length);
            }

            // Enum.TryParse 按成员名（Tenant/User/…）；数值形态不接受（防手写错值）——
            // TryParse 对 "5"/"999" 一律返回 true（数值直通），必须再以 IsDefined + 成员名回环钉死。
            if (!Enum.TryParse<ToolSurfaceTokenKind>(kindText, ignoreCase: false, out var kind)
                || !Enum.IsDefined(typeof(ToolSurfaceTokenKind), kind)
                || Enum.GetName(typeof(ToolSurfaceTokenKind), kind) != kindText
                || marker.Length == 0)
            {
                continue;
            }

            builder.Add(new TokenKindMarker(marker, kind, mode));
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<string> SplitList(string? raw)
        => string.IsNullOrWhiteSpace(raw)
            ? ImmutableArray<string>.Empty
            : raw!.Split('|').Select(static s => s.Trim()).Where(static s => s.Length > 0).ToImmutableArray();

    /// <inheritdoc />
    public bool Equals(SdkToolProfileModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Name == other.Name
            && ToolAttributeName == other.ToolAttributeName
            && ToolAttributeNamespace == other.ToolAttributeNamespace
            && ToolHandlerAttributeName == other.ToolHandlerAttributeName
            && ToolHandlerAttributeNamespace == other.ToolHandlerAttributeNamespace
            && ParameterAttributeName == other.ParameterAttributeName
            && SdkNamespaceRoot == other.SdkNamespaceRoot
            && InterfaceNameRegex == other.InterfaceNameRegex
            && TokenKindStrategy == other.TokenKindStrategy
            && TokenKindMarkers.SequenceEqual(other.TokenKindMarkers)
            && ProductPrefix == other.ProductPrefix
            && ProductPluralPrefix == other.ProductPluralPrefix
            && DiagnosticPrefix == other.DiagnosticPrefix
            && DiagnosticCategory == other.DiagnosticCategory
            && ToolingDiagnosticCategory == other.ToolingDiagnosticCategory
            && WriteVerbKeywords.SequenceEqual(other.WriteVerbKeywords)
            && SchemaExtensionKey == other.SchemaExtensionKey
            && OwnerAssembly == other.OwnerAssembly
            && GeneratedNamespace == other.GeneratedNamespace
            && ContractNamespace == other.ContractNamespace
            && RegistrationNamespace == other.RegistrationNamespace
            && RiskEnumFullName == other.RiskEnumFullName
            && ResultTypeName == other.ResultTypeName
            && BindingTypeName == other.BindingTypeName
            && OutputWrapperNamespace == other.OutputWrapperNamespace
            && OutputWrapperTypeNames.SequenceEqual(other.OutputWrapperTypeNames)
            && SdkInterfacePrefix == other.SdkInterfacePrefix
            && CapabilityCatalogPropertyName == other.CapabilityCatalogPropertyName
            && GoldenFileName == other.GoldenFileName
            && GoldenUpdatePropertyName == other.GoldenUpdatePropertyName
            && GuidanceDirectory == other.GuidanceDirectory
            && AffordanceDirectory == other.AffordanceDirectory
            && MissingRequiredSlots.SequenceEqual(other.MissingRequiredSlots);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as SdkToolProfileModel);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        if (!_hashCodeComputed)
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(Name);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(ToolAttributeName);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(ToolAttributeNamespace);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(ToolHandlerAttributeName);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(ToolHandlerAttributeNamespace);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(ParameterAttributeName);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(SdkNamespaceRoot);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(InterfaceNameRegex);
                hash = (hash * 31) + (int)TokenKindStrategy;
                foreach (var marker in TokenKindMarkers)
                {
                    hash = (hash * 31) + marker.GetHashCode();
                }

                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(ProductPrefix);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(ProductPluralPrefix);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(DiagnosticPrefix);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(DiagnosticCategory);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(ToolingDiagnosticCategory);
                foreach (var verb in WriteVerbKeywords)
                {
                    hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(verb);
                }

                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(SchemaExtensionKey);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(OwnerAssembly);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(GeneratedNamespace);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(ContractNamespace);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(RegistrationNamespace);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(RiskEnumFullName);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(ResultTypeName);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(BindingTypeName);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(OutputWrapperNamespace);
                foreach (var wrapper in OutputWrapperTypeNames)
                {
                    hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(wrapper);
                }

                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(SdkInterfacePrefix);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(CapabilityCatalogPropertyName);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(GoldenFileName);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(GoldenUpdatePropertyName);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(GuidanceDirectory);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(AffordanceDirectory);
                _hashCode = hash;
            }

            _hashCodeComputed = true;
        }

        return _hashCode;
    }
}

/// <summary>
/// 剖面集合的<b>值相等</b>载体（增量管线的剖面扇出入口）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么不能直接用 <see cref="ImmutableArray{T}"/></b>：它的相等性实现是<b>底层数组引用比较</b>。
/// 一旦把剖面数组直接投进增量图，任何一次编译变更（哪怕只是新增一个无关类）都会让
/// <c>CompilationProvider.Select</c> 的产出被判定为 <c>Modified</c>，进而让整条工具面扇出
/// （Schemas/Names/Contracts/Args/Registrars/Guidance/Catalog）在每个击键上重跑。
/// 本类型按元素逐项比较 + 缓存哈希，使「剖面未变但编译变了」退化为 <c>Unchanged</c>，下游保持 Cached。
/// </para>
/// <para>顺序敏感比较：<see cref="ProfileDiscovery.ResolveProfiles"/> 已按 <c>Name</c> 排序，故顺序稳定。</para>
/// </remarks>
internal sealed class SdkToolProfileSet : IEquatable<SdkToolProfileSet>
{
    private int _hashCode;
    private bool _hashCodeComputed;

    private SdkToolProfileSet(ImmutableArray<SdkToolProfileModel> items) => Items = items;

    /// <summary>空集合（纯 HTTP 消费方的典型路径）。</summary>
    public static SdkToolProfileSet Empty { get; } = new(ImmutableArray<SdkToolProfileModel>.Empty);

    /// <summary>剖面数组（按 Name 排序）。</summary>
    public ImmutableArray<SdkToolProfileModel> Items { get; }

    /// <summary>剖面数量。</summary>
    public int Count => Items.Length;

    /// <summary>按序取剖面。</summary>
    public SdkToolProfileModel this[int index] => Items[index];

    /// <summary>由剖面数组构造（解析失败的空结果统一走 <see cref="Empty"/>）。</summary>
    public static SdkToolProfileSet Create(ImmutableArray<SdkToolProfileModel> items)
        => items.IsDefaultOrEmpty ? Empty : new SdkToolProfileSet(items);

    /// <inheritdoc />
    public bool Equals(SdkToolProfileSet? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (Items.Length != other.Items.Length)
        {
            return false;
        }

        for (var i = 0; i < Items.Length; i++)
        {
            if (!Items[i].Equals(other.Items[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as SdkToolProfileSet);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        if (!_hashCodeComputed)
        {
            unchecked
            {
                var hash = 17;
                foreach (var profile in Items)
                {
                    hash = (hash * 31) + profile.GetHashCode();
                }

                _hashCode = hash;
            }

            _hashCodeComputed = true;
        }

        return _hashCode;
    }
}

/// <summary>
/// 剖面发现（设计文档 §5.3）：在编译中找出「实现 <c>ISdkToolProfile</c> 且标注 <c>[SdkToolProfile]</c>」
/// 的类，产出按 <see cref="SdkToolProfileModel.Name"/> 排序的剖面数组（确定性扇出顺序）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么走 <c>CompilationProvider</c> 而非 SyntaxProvider 流</b>：扫描变换
/// （<c>ScanTools</c>/<c>ScanHandlers</c>）需要剖面集合参与过滤，而 <c>CreateSyntaxProvider</c>
/// 的变换签名只有 <c>GeneratorSyntaxContext</c>——剖面必须由变换自行从编译解析
/// （上游 R4-2 已确立「变换携带 compilation、随编译失效」的粒度纪律，编辑剖面类同样触发重扫）。
/// </para>
/// <para>
/// <b>空剖面短路（§5.3/§7.2 硬性前提）</b>：编译中不存在任何 <c>ISdkToolProfile</c> 实现时
/// 返回空数组，扫描变换拿到空数组后在进入 <c>ToolSurface*</c> 逻辑前直接返回。
/// 注意 <c>ISdkToolProfile</c> 定义于 Abstractions、所有消费方恒引用该程序集，
/// 接口存在性判定无法借 <c>GetTypeByMetadataName</c> 早退——故解析结果按
/// <see cref="Compilation"/> 实例缓存（见 <see cref="ResolveProfiles"/>），
/// 全程序集类型枚举每编译至多支付一次。
/// </para>
/// <para>
/// 只消费「成对合法」的剖面；不成对的半边由 <see cref="ProfileContractGuardAnalyzer"/>（SDKT001）
/// 在分析器侧报错，生成器侧静默忽略（避免同一问题双重报告）。
/// 必填槽缺失的剖面<b>保留在结果中</b>（<see cref="SdkToolProfileModel.MissingRequiredSlots"/> 非空），
/// 由入口上报 SDKT002 并从扇出中排除。
/// </para>
/// </remarks>
internal static class ProfileDiscovery
{
    internal const string ProfileInterfaceFullName = "Mud.AI.ISdkToolProfile";
    internal const string ProfileAttributeNamespace = "Mud.AI.Attributes";

    /// <summary>解析当前编译中的全部合法剖面（按 Name 排序；同名去重取首个，防 hintName 撞名）。</summary>
    /// <remarks>
    /// <para>
    /// 结果按 <see cref="Compilation"/> 实例经 <see cref="ConditionalWeakTable{TKey, TValue}"/> 缓存：
    /// 同一编译内的 profiles 节点（<c>CompilationProvider</c>）与全部 ScanTool/ScanHandler 变换
    /// 共享一次全程序集枚举，消除「每候选接口/方法重复枚举」的成本乘数。
    /// <see cref="Compilation"/> 不可变；缓存值（全字符串槽位的 <see cref="SdkToolProfileModel"/>）
    /// 不持有符号引用，条目随编译实例一并被 GC 回收。
    /// </para>
    /// <para>
    /// <see cref="Lazy{T}"/> 保证并发变换下工厂体只执行一次（<c>ConditionalWeakTable</c> 本身
    /// 不承诺工厂单次执行）；同一编译内解析异常同样只支付一次代价。
    /// </para>
    /// </remarks>
    public static ImmutableArray<SdkToolProfileModel> ResolveProfiles(Compilation compilation)
        => ResolutionCache
            .GetValue(compilation, static c => new Lazy<ImmutableArray<SdkToolProfileModel>>(() => ResolveProfilesCore(c)))
            .Value;

    private static readonly ConditionalWeakTable<Compilation, Lazy<ImmutableArray<SdkToolProfileModel>>> ResolutionCache = new();

    private static ImmutableArray<SdkToolProfileModel> ResolveProfilesCore(Compilation compilation)
    {
        try
        {
            // [F-4 修复·B-5] 语法级预筛：纯 HTTP / 纯 DTO 消费方（无任何剖面文本）直接短路，
            // 不再对编译内全部源类型求 AllInterfaces 传递闭包（0.1~0.3s × 每项目 × 每 TFM）。
            // 保守性：被消费的剖面必须同时「实现 ISdkToolProfile」且「标注 [SdkToolProfile]」，
            // 两者都必然在源文本以标识符出现（"ISdkToolProfile" 含子串 "SdkToolProfile"，任一命中即疑似）。
            // 生成文件同样在 SyntaxTrees 内被扫描，不漏报。
            if (!SyntaxPrefilter.MayContain(compilation, "ISdkToolProfile", "SdkToolProfile"))
            {
                return ImmutableArray<SdkToolProfileModel>.Empty;
            }

            var profileInterface = compilation.GetTypeByMetadataName(ProfileInterfaceFullName);
            if (profileInterface is null)
            {
                return ImmutableArray<SdkToolProfileModel>.Empty;
            }

            var found = new List<SdkToolProfileModel>();
            var seenNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var symbol in EnumerateTypes(compilation.Assembly.GlobalNamespace))
            {
                if (!symbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, profileInterface)))
                {
                    continue;
                }

                AttributeData? attributeData = null;
                foreach (var attribute in symbol.GetAttributes())
                {
                    var attributeClass = attribute.AttributeClass;
                    if (attributeClass is not null
                        && attributeClass.Name is "SdkToolProfileAttribute" or "SdkToolProfile"
                        && string.Equals(attributeClass.ContainingNamespace?.ToDisplayString(), ProfileAttributeNamespace, StringComparison.Ordinal))
                    {
                        attributeData = attribute;
                        break;
                    }
                }

                // 成对守卫：缺特性半边 → 交给 SDKT001，生成器不消费。
                if (attributeData is null)
                {
                    continue;
                }

                var model = SdkToolProfileModel.FromAttributeData(attributeData);
                if (seenNames.Add(model.Name))
                {
                    found.Add(model);
                }
            }

            found.Sort(static (a, b) => string.CompareOrdinal(a.Name, b.Name));
            return found.ToImmutableArray();
        }
        catch (Exception ex)
        {
            GeneratorDebugLogger.LogError(nameof(ProfileDiscovery), ex);
            return ImmutableArray<SdkToolProfileModel>.Empty;
        }
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceSymbol ns)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            yield return type;
            foreach (var nested in EnumerateNested(type))
            {
                yield return nested;
            }
        }

        foreach (var child in ns.GetNamespaceMembers())
        {
            foreach (var type in EnumerateTypes(child))
            {
                yield return type;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateNested(INamedTypeSymbol type)
    {
        foreach (var nested in type.GetTypeMembers())
        {
            yield return nested;
            foreach (var deeper in EnumerateNested(nested))
            {
                yield return deeper;
            }
        }
    }
}