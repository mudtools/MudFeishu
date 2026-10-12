// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Application;

/// <summary>
/// 更新应用通讯录权限范围请求体。更新后线上立即生效。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class UpdateApplicationContactsRangeRequest
{
    /// <summary>
    /// <para>通讯录权限范围的更新方式</para>
    /// <para>必填：是</para>
    /// <para>可选值：equal_to_availability（与应用可用范围保持一致）、some（部分成员，add_visible_list/del_visible_list 增量生效）、all（全部成员）</para>
    /// <para>示例值：some</para>
    /// </summary>
    [JsonPropertyName("contacts_range_type")]
    public string? ContactsRangeType { get; set; }

    /// <summary>
    /// <para>加入通讯录权限范围的成员列表，仅 contacts_range_type 为 some 时生效</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("add_visible_list")]
    public AppIdList? AddVisibleList { get; set; }

    /// <summary>
    /// <para>从通讯录权限范围移除的成员列表，仅 contacts_range_type 为 some 时生效</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("del_visible_list")]
    public AppIdList? DelVisibleList { get; set; }
}
