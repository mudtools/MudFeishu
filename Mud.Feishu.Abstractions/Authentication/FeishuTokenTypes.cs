// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Authentication;

/// <summary>
/// 飞书令牌类型常量定义。
/// </summary>
/// <remarks>
/// 飞书开放平台提供三种独立的访问令牌类型，各有不同的获取方式和适用场景。
/// 这些常量用于 TokenAttribute 的 TokenType 参数和
/// IFeishuAppContext.GetTokenManager 方法的 tokenType 参数。
/// </remarks>
public static class FeishuTokenTypes
{
    /// <summary>
    /// 租户访问令牌（Tenant Access Token）。
    /// </summary>
    /// <remarks>
    /// 用于租户级别的 API 调用，通过 AppId + AppSecret 获取。
    /// 适用于应用以自身身份调用 API 的场景，如发送消息、管理通讯录等。
    /// </remarks>
    public const string TenantAccessToken = "TenantAccessToken";

    /// <summary>
    /// 应用访问令牌（App Access Token）。
    /// </summary>
    /// <remarks>
    /// 用于应用级别的 API 调用，如获取用户信息等。
    /// 通过 AppId + AppSecret 获取，与 TenantAccessToken 的获取方式相同但用途不同。
    /// </remarks>
    public const string AppAccessToken = "AppAccessToken";

    /// <summary>
    /// 用户访问令牌（User Access Token）。
    /// </summary>
    /// <remarks>
    /// 用于用户级别的 API 调用，通过 OAuth 授权码换取。
    /// 适用于应用以用户身份调用 API 的场景，如代表用户操作云文档、发送消息等。
    /// </remarks>
    public const string UserAccessToken = "UserAccessToken";
}
