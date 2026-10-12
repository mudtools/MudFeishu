// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Application;

/// <summary>
/// 应用版本的发布备注（更新说明与可见范围）。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppVersionRemark
{
    /// <summary>
    /// <para>发布说明</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("remark")]
    public string? Remark { get; set; }

    /// <summary>
    /// <para>更新说明</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("update_remark")]
    public string? UpdateRemark { get; set; }

    /// <summary>
    /// <para>版本可见范围配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("visibility")]
    public AppVersionRemarkVisibility? Visibility { get; set; }
}

/// <summary>
/// 应用版本的可见范围配置。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppVersionRemarkVisibility
{
    /// <summary>
    /// <para>是否对所有用户可见</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_all")]
    public bool? IsAll { get; set; }

    /// <summary>
    /// <para>可见人员列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("visible_list")]
    public ContactsRangeVisibleList? VisibleList { get; set; }

    /// <summary>
    /// <para>不可见人员列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("invisible_list")]
    public ContactsRangeVisibleList? InvisibleList { get; set; }
}

/// <summary>
/// 应用版本订阅的事件详情。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppVersionEventInfo
{
    /// <summary>
    /// <para>事件类型（事件标识）</para>
    /// <para>必填：否</para>
    /// <para>示例值：contact.user.created_v3</para>
    /// </summary>
    [JsonPropertyName("event_type")]
    public string? EventType { get; set; }

    /// <summary>
    /// <para>事件名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("event_name")]
    public string? EventName { get; set; }

    /// <summary>
    /// <para>事件描述</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("event_description")]
    public string? EventDescription { get; set; }
}
