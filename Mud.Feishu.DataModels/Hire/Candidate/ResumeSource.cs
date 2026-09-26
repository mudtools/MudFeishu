// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 简历来源信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class ResumeSource
{
    /// <summary>
    /// <para>简历来源 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：1111</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>简历来源中文名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：内推</para>
    /// </summary>
    [JsonPropertyName("zh_name")]
    public string? ZhName { get; set; }

    /// <summary>
    /// <para>简历来源英文名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：referral</para>
    /// </summary>
    [JsonPropertyName("en_name")]
    public string? EnName { get; set; }

    /// <summary>
    /// <para>启用状态：1-启用，2-停用</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("active_status")]
    public int? ActiveStatus { get; set; }

    /// <summary>
    /// <para>简历来源类型：10000-内推，10001-猎头，10002-内部来源，10003-第三方招聘网站，10004-社交媒体，10005-线下来源，10006-其他，10007-外部推荐，10008-员工转岗，10009-实习生转正</para>
    /// <para>必填：否</para>
    /// <para>示例值：10001</para>
    /// </summary>
    [JsonPropertyName("resume_source_type")]
    public string? ResumeSourceType { get; set; }
}
