// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Authentication;

/// <summary>
/// 飞书用户认证配置选项
/// </summary>
/// <remarks>
/// <para>配置示例：</para>
/// <code>
/// services.AddFeishuUserContext(options =>
/// {
///     options.OpenIdClaimType = "custom_open_id";
///     options.EnableSensitiveLog = false;
/// });
/// </code>
/// </remarks>
public class FeishuUserAuthenticationOptions
{
    /// <summary>
    /// OpenId 的 Claim 类型名称
    /// </summary>
    /// <remarks>
    /// 默认值为 "open_id"，同时会尝试 ClaimTypes.NameIdentifier 作为备选
    /// </remarks>
    public string OpenIdClaimType { get; set; } = "open_id";

    /// <summary>
    /// OpenId 的备用 Claim 类型名称
    /// </summary>
    /// <remarks>
    /// 默认值为 ClaimTypes.NameIdentifier
    /// </remarks>
    public string OpenIdFallbackClaimType { get; set; } = System.Security.Claims.ClaimTypes.NameIdentifier;

    /// <summary>
    /// UnionId 的 Claim 类型名称
    /// </summary>
    public string UnionIdClaimType { get; set; } = "union_id";

    /// <summary>
    /// UserId 的 Claim 类型名称
    /// </summary>
    public string UserIdClaimType { get; set; } = "user_id";

    /// <summary>
    /// 用户名称的 Claim 类型名称
    /// </summary>
    public string NameClaimType { get; set; } = System.Security.Claims.ClaimTypes.Name;

    /// <summary>
    /// 是否启用分布式追踪
    /// </summary>
    /// <remarks>
    /// 启用后会创建 Activity 并设置用户相关标签
    /// </remarks>
    public bool EnableDistributedTracing { get; set; } = true;

    /// <summary>
    /// 是否在日志中记录敏感信息（如 OpenId）
    /// </summary>
    /// <remarks>
    /// 默认为 false，敏感信息将被脱敏处理。
    /// 生产环境建议保持 false
    /// </remarks>
    public bool EnableSensitiveLog { get; set; } = false;
}
