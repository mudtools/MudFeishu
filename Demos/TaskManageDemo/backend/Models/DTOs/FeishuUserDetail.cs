// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace TaskManageDemo.Backend.Models.DTOs;

/// <summary>
/// 飞书用户详细信息
/// </summary>
public class FeishuUserDetail
{
    /// <summary>
    /// OpenId
    /// </summary>
    public string OpenId { get; set; } = string.Empty;

    /// <summary>
    /// UnionId
    /// </summary>
    public string UnionId { get; set; } = string.Empty;

    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 姓名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 英文名
    /// </summary>
    public string? EnName { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    public string? Nickname { get; set; }

    /// <summary>
    /// 头像URL
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// 头像缩略图
    /// </summary>
    public string? AvatarThumb { get; set; }

    /// <summary>
    /// 头像中图
    /// </summary>
    public string? AvatarMiddle { get; set; }

    /// <summary>
    /// 头像大图
    /// </summary>
    public string? AvatarBig { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    public string? Mobile { get; set; }

    /// <summary>
    /// 企业邮箱
    /// </summary>
    public string? EnterpriseEmail { get; set; }

    /// <summary>
    /// 员工编号
    /// </summary>
    public string? EmployeeNo { get; set; }

    /// <summary>
    /// 租户Key
    /// </summary>
    public string? TenantKey { get; set; }
}
