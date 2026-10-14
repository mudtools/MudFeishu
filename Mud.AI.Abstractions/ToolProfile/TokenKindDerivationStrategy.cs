// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Attributes;

/// <summary>
/// 令牌身份推导策略：如何从 SDK 接口名推出 <see cref="SdkTokenKind"/>。
/// </summary>
/// <remarks>
/// v2.3 随引擎进仓（原 <c>Mud.HttpUtils.Attributes.TokenKindDerivationStrategy</c>）。
/// 引擎只按本策略 + profile 的 <c>TokenKindMarkers</c> 标记表推导，永不内联任何 SDK 的命名事实。
/// 实测：微信同时存在后缀形态（<c>IWechatWorkAccountIdBotService_Provider</c>）与中缀形态
/// （<c>IWechatWorkProviderAccountIdBotService</c>），故策略必须三值，且标记表按形态分列。
/// </remarks>
public enum TokenKindDerivationStrategy
{
    /// <summary>未指定（等价 <see cref="SdkTokenKind.Unspecified"/>，剖面契约守卫报错）。</summary>
    Unspecified = 0,

    /// <summary>前缀标记：接口名以标记串开头（飞书 <c>IFeishuTenant</c>→Tenant、<c>IFeishuUser</c>→User）。</summary>
    NamePrefix = 1,

    /// <summary>后缀标记：接口名以标记串结尾（微信 <c>…_Provider</c>→ThirdParty、<c>…_Internal</c>→Internal）。</summary>
    SuffixMarker = 2,

    /// <summary>中缀标记：接口名在 I + SDK 词后含标记串（微信 <c>IWechatWorkProvider…</c>→ThirdParty）。</summary>
    InfixMarker = 3,
}
