// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI;

/// <summary>
/// SDK 接口名解析结果（值对象）：接口命名范式经 profile 解析后的结构化事实。
/// </summary>
/// <remarks>
/// v2.3 随引擎进仓（原 <c>Mud.HttpUtils.InterfaceIdentity</c>）。
/// <para>
/// 引擎侧不变量：本类型只携带<b>归一化后的语义</b>（令牌身份 <see cref="Kind"/>、域、资源、版本），
/// 不携带任何 SDK 命名前缀字符串——前缀/后缀/中缀的推导发生在 profile 解析委托内。
/// </para>
/// <para>
/// <see cref="RawTokenMarker"/> 保留生态本地的原始命名标记（如微信 <c>_Provider</c> 与 <c>_ThirdParty</c>
/// 在 <see cref="SdkTokenKind"/> 上共用 <c>ThirdParty</c>，但授权链需要区分），供 SDK 自己的授权链读取；
/// 引擎只对 <see cref="Kind"/> 做归一化消费。
/// </para>
/// </remarks>
public sealed class InterfaceIdentity : IEquatable<InterfaceIdentity?>
{
    /// <summary>
    /// 构造解析结果。
    /// </summary>
    /// <param name="interfaceName">接口简单名（如 <c>IFeishuTenantV3User</c>）。</param>
    /// <param name="kind">归一化令牌身份。</param>
    /// <param name="domain">域名段（如 <c>Bitable</c>），可为空串。</param>
    /// <param name="resource">资源段（如 <c>AppTable</c>），可为空串。</param>
    /// <param name="version">版本号（<c>V{n}</c> 的 <c>n</c>）；命名不含版本段时为 <see langword="null"/>。</param>
    /// <param name="rawTokenMarker">生态本地原始命名标记（如 <c>_Provider</c>）；无则为空串。</param>
    public InterfaceIdentity(
        string interfaceName,
        SdkTokenKind kind,
        string domain,
        string resource,
        int? version,
        string rawTokenMarker = "")
    {
        InterfaceName = interfaceName ?? throw new ArgumentNullException(nameof(interfaceName));
        Kind = kind;
        Domain = domain ?? string.Empty;
        Resource = resource ?? string.Empty;
        Version = version;
        RawTokenMarker = rawTokenMarker ?? string.Empty;
    }

    /// <summary>接口简单名（解析输入原文，供诊断定位）。</summary>
    public string InterfaceName { get; }

    /// <summary>归一化令牌身份（引擎唯一消费的身份轴）。</summary>
    public SdkTokenKind Kind { get; }

    /// <summary>域名段（能力归属轴；工具名与模块统计取用）。</summary>
    public string Domain { get; }

    /// <summary>资源段（可为空，如 <c>V3User</c> 只有 Domain）。</summary>
    public string Resource { get; }

    /// <summary>版本号（<c>V{n}</c>）；缺省 <see langword="null"/>，仅作 profile 本地诊断。</summary>
    public int? Version { get; }

    /// <summary>生态本地原始命名标记（如 <c>_Provider</c>），供 SDK 授权链区分精细语义；引擎不消费。</summary>
    public string RawTokenMarker { get; }

    /// <inheritdoc />
    public bool Equals(InterfaceIdentity? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return string.Equals(InterfaceName, other.InterfaceName, StringComparison.Ordinal)
            && Kind == other.Kind
            && string.Equals(Domain, other.Domain, StringComparison.Ordinal)
            && string.Equals(Resource, other.Resource, StringComparison.Ordinal)
            && Version == other.Version
            && string.Equals(RawTokenMarker, other.RawTokenMarker, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as InterfaceIdentity);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(InterfaceName);
            hash = (hash * 31) + (int)Kind;
            hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(Domain);
            hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(Resource);
            hash = (hash * 31) + Version.GetHashCode();
            hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(RawTokenMarker);
            return hash;
        }
    }
}
