// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;

/// <summary>
/// <para>日历访问控制数据信息。</para>
/// </summary>
public class CalendarAclDataInfo : CalendarAclData
{
    /// <summary>
    /// <para>访问控制 ID。该 ID 在单个日历实体内唯一，不同日历实体可能存在重复的访问控制 ID。</para>
    /// <para>必填：是</para>
    /// <para>示例值：user_xxxxxx</para>
    /// </summary>
    [JsonPropertyName("acl_id")]
    public string AclId { get; set; } = string.Empty;
}