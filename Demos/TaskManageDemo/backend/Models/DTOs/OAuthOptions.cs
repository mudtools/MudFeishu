// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace TaskManageDemo.Backend.Models.DTOs;

/// <summary>
/// OAuth 配置选项
/// </summary>
public class OAuthOptions
{
    /// <summary>
    /// 重定向URI
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// State 过期时间（分钟）
    /// </summary>
    public int StateExpirationMinutes { get; set; } = 10;

    /// <summary>
    /// JWT 配置
    /// </summary>
    public JwtOptions Jwt { get; set; } = new JwtOptions();
}

/// <summary>
/// JWT 配置选项
/// </summary>
public class JwtOptions
{
    /// <summary>
    /// 密钥
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// 颁发者
    /// </summary>
    public string Issuer { get; set; } = "TaskManageDemo";

    /// <summary>
    /// 受众
    /// </summary>
    public string Audience { get; set; } = "TaskManageDemo.Client";

    /// <summary>
    /// 过期时间（分钟）
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;
}
