// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Mud.AI.ToolSurface.Extraction;

/// <summary>
/// SDK 接口名令牌结构的引擎侧镜像值对象。
/// </summary>
/// <remarks>
/// <para>
/// 与运行时契约 <c>Mud.AI.Abstractions.InterfaceIdentity</c>（设计文档 §3/§4.3）**逐字段镜像**
/// （InterfaceName / Kind / Domain / Resource / Version / RawTokenMarker），由
/// <c>ToolSurfaceContractTests</c> 反射比对机械锁定。镜像的原因：工程纪律 §1.2-3 禁止生成器
/// 引用任何 <c>Mud.*</c> 运行时程序集，故引擎持有自己的 <c>ToolSurfaceTokenKind</c> 形态值对象。
/// </para>
/// <para>不采用 <c>record</c>：netstandard2.0 目标未注入 <c>IsExternalInit</c>（仓库纪律）。</para>
/// </remarks>
internal sealed class ToolSurfaceInterfaceIdentity : IEquatable<ToolSurfaceInterfaceIdentity?>
{
    public ToolSurfaceInterfaceIdentity(
        string interfaceName,
        ToolSurfaceTokenKind kind,
        string domain,
        string resource,
        int? version,
        string? rawTokenMarker)
    {
        InterfaceName = interfaceName;
        Kind = kind;
        Domain = domain;
        Resource = resource;
        Version = version;
        RawTokenMarker = rawTokenMarker;
    }

    /// <summary>接口名（如 <c>IFeishuTenantV1BitableAppTable</c>）。</summary>
    public string InterfaceName { get; }

    /// <summary>归一化令牌身份（引擎唯一消费的身份轴，设计文档 §3）。</summary>
    public ToolSurfaceTokenKind Kind { get; }

    /// <summary>域名段（如 <c>Bitable</c>；正则无 <c>domain</c> 组时为空串）。</summary>
    public string Domain { get; }

    /// <summary>资源段（可为空串，如 <c>V3User</c> 只有 Domain）。</summary>
    public string Resource { get; }

    /// <summary>版本号（<c>V{n}</c> 的 <c>n</c>；正则无 <c>version</c> 组或非数字时 <see langword="null"/>）。仅作 profile 本地诊断，引擎不强制消费。</summary>
    public int? Version { get; }

    /// <summary>命中的原始命名标记串（如 <c>IFeishuTenant</c> / <c>_Provider</c>；无标记命中时 <see langword="null"/>）。供生态本地授权链读取精细语义（设计文档 §3）。</summary>
    public string? RawTokenMarker { get; }

    public bool Equals(ToolSurfaceInterfaceIdentity? other)
        => other is not null
            && string.Equals(InterfaceName, other.InterfaceName, StringComparison.Ordinal)
            && Kind == other.Kind
            && string.Equals(Domain, other.Domain, StringComparison.Ordinal)
            && string.Equals(Resource, other.Resource, StringComparison.Ordinal)
            && Version == other.Version
            && string.Equals(RawTokenMarker ?? string.Empty, other.RawTokenMarker ?? string.Empty, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as ToolSurfaceInterfaceIdentity);

    public override int GetHashCode()
    {
        var comparer = StringComparer.Ordinal;
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + comparer.GetHashCode(InterfaceName);
            hash = (hash * 31) + (int)Kind;
            hash = (hash * 31) + comparer.GetHashCode(Domain);
            hash = (hash * 31) + comparer.GetHashCode(Resource);
            hash = (hash * 31) + Version.GetHashCode();
            hash = (hash * 31) + comparer.GetHashCode(RawTokenMarker ?? string.Empty);
            return hash;
        }
    }
}

/// <summary>
/// 接口名 → <see cref="ToolSurfaceInterfaceIdentity"/> 解析器
/// （泛化自上游 <c>Extractors.TryParseSdkInterfaceName</c>，设计文档 §4.2 第 3/4 项）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么不从命名空间推导模块名</b>（上游结论保留）：SDK 接口的模块信息只存在于目录结构里、
/// Roslyn 符号不可见，域名（Domain）是符号层唯一可用的能力轴。
/// </para>
/// <para>
/// 命名范式正则来自剖面槽 <c>InterfaceNameRegex</c>（组名契约：<c>domain</c> / <c>resource</c> /
/// <c>version</c> 三个可选命名组）；令牌身份来自 <c>TokenKindStrategy</c> + <c>TokenKindMarkers</c>
/// 标记表——引擎内部零 SDK 命名字符串（设计文档 §2/§3）。
/// </para>
/// </remarks>
internal static class InterfaceIdentityParser
{
    /// <summary>
    /// 按 pattern 缓存编译后的正则——<c>RegexOptions.Compiled</c> 的 JIT 成本只付一次，
    /// 且不同 profile 的 pattern 互不干扰（netstandard2.0 可用 <c>System.Text.RegularExpressions</c>）。
    /// </summary>
    private static readonly ConcurrentDictionary<string, Regex> RegexCache = new(StringComparer.Ordinal);

    /// <summary>解析接口名的令牌结构。不符合剖面正则返回 <see langword="false"/>。</summary>
    public static bool TryParse(
        string interfaceName,
        SdkToolProfileModel profile,
        out ToolSurfaceInterfaceIdentity? identity)
    {
        identity = null;
        try
        {
            if (string.IsNullOrEmpty(interfaceName)
                || string.IsNullOrEmpty(profile.InterfaceNameRegex))
            {
                return false;
            }

            var regex = RegexCache.GetOrAdd(
                profile.InterfaceNameRegex,
                static pattern => new Regex(pattern, RegexOptions.Compiled));
            var match = regex.Match(interfaceName);
            if (!match.Success)
            {
                return false;
            }

            var domain = GetGroupValue(match, "domain");
            var resource = GetGroupValue(match, "resource");

            // 版本段与运行时契约 InterfaceIdentity.Version 同类型（int?）：非数字版本视为无版本，
            // 不把字符串形态泄漏进镜像（版本只作 profile 本地诊断，不参与任何产物渲染）。
            var versionGroup = match.Groups["version"];
            int? version = null;
            if (versionGroup.Success
                && int.TryParse(versionGroup.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedVersion))
            {
                version = parsedVersion;
            }

            var kind = DeriveTokenKind(interfaceName, profile, out var rawMarker);
            identity = new ToolSurfaceInterfaceIdentity(interfaceName, kind, domain, resource, version, rawMarker);
            return true;
        }
        catch (Exception ex)
        {
            GeneratorDebugLogger.LogError(nameof(InterfaceIdentityParser), ex);
            identity = null;
            return false;
        }
    }

    /// <summary>
    /// 按「标记串 → 令牌身份」表推导归一化身份（泛化自上游
    /// <c>CuratedToolScanner.DeriveIdentityFromSource</c> 的 <c>StartsWith("IFeishuTenant")</c> 硬编码）。
    /// </summary>
    /// <remarks>
    /// 匹配形态：标记条目带 <c>OverrideMode</c>（非 Unspecified）时按覆写形态匹配
    /// （NamePrefix=StartsWith、SuffixMarker=EndsWith、InfixMarker=Contains）；
    /// 否则按 profile 的 <see cref="SdkToolProfileModel.TokenKindStrategy"/> 默认形态
    /// （策略亦为 Unspecified 时回退 NamePrefix——与上游前缀语义同形）。
    /// 表按声明序取<b>首个命中</b>；无命中返回 <see cref="ToolSurfaceTokenKind.Both"/>
    /// 且 <paramref name="rawMarker"/> 为 <see langword="null"/>（上游"双令牌基接口"语义）。
    /// </remarks>
    public static ToolSurfaceTokenKind DeriveTokenKind(
        string interfaceName,
        SdkToolProfileModel profile,
        out string? rawMarker)
    {
        rawMarker = null;
        try
        {
            foreach (var marker in profile.TokenKindMarkers)
            {
                var mode = marker.OverrideMode != ToolSurfaceTokenKindStrategy.Unspecified
                    ? marker.OverrideMode
                    : profile.TokenKindStrategy != ToolSurfaceTokenKindStrategy.Unspecified
                        ? profile.TokenKindStrategy
                        : ToolSurfaceTokenKindStrategy.NamePrefix;

                var hit = mode switch
                {
                    ToolSurfaceTokenKindStrategy.SuffixMarker => interfaceName.EndsWith(marker.Marker, StringComparison.Ordinal),
                    ToolSurfaceTokenKindStrategy.InfixMarker => interfaceName.IndexOf(marker.Marker, StringComparison.Ordinal) >= 0,
                    _ => interfaceName.StartsWith(marker.Marker, StringComparison.Ordinal),
                };

                if (hit)
                {
                    rawMarker = marker.Marker;
                    return marker.Kind;
                }
            }
        }
        catch (Exception ex)
        {
            GeneratorDebugLogger.LogError(nameof(InterfaceIdentityParser), ex);
        }

        return ToolSurfaceTokenKind.Both;
    }

    private static string GetGroupValue(Match match, string groupName)
    {
        var group = match.Groups[groupName];
        return group.Success ? group.Value : string.Empty;
    }
}