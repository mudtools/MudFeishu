// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive;

/// <summary>
/// 协作者权限成员详情模型
/// <para>协作者权限成员详情模型，包括用户、群组、部门、用户组等。</para>
/// </summary>
public class PermissionMemberDetail : PermissionMember
{
    /// <summary>
    /// <para>协作者的名字</para>
    /// <para>必填：否</para>
    /// <para>示例值：zhangsan</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>协作者的头像</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://s3-imfile.feishucdn.com/static-resource/v1/v3_0061_b576862d-92e0-4abc-bbb5-6f78f927a61g~?image_size=72x72&amp;cut_type=default-face&amp;quality=&amp;format=jpeg&amp;sticker_format=.webp</para>
    /// </summary>
    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    /// <summary>
    /// <para>协作者的外部标签</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("external_label")]
    public bool? ExternalLabel { get; set; }
}