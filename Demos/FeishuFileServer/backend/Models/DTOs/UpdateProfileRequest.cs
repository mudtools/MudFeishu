// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace FeishuFileServer.Models.DTOs;

/// <summary>
/// 更新用户资料请求
/// 用于更新用户的邮箱和显示名称
/// </summary>
public class UpdateProfileRequest
{
    /// <summary>
    /// 邮箱地址
    /// </summary>
    [EmailAddress]
    public string? Email { get; set; }

    /// <summary>
    /// 显示名称
    /// </summary>
    [StringLength(50, MinimumLength = 1)]
    public string? DisplayName { get; set; }
}
