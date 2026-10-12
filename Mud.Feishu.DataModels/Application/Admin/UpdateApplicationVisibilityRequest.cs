// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Application;

/// <summary>
/// 更新应用可用范围请求体。更新后对线上立即生效。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class UpdateApplicationVisibilityRequest
{
    /// <summary>
    /// <para>加入可用（白）名单的成员列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("add_visible_list")]
    public AppIdList? AddVisibleList { get; set; }

    /// <summary>
    /// <para>从可用（白）名单移除的成员列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("del_visible_list")]
    public AppIdList? DelVisibleList { get; set; }

    /// <summary>
    /// <para>加入禁用（黑）名单的成员列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("add_invisible_list")]
    public AppIdList? AddInvisibleList { get; set; }

    /// <summary>
    /// <para>从禁用（黑）名单移除的成员列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("del_invisible_list")]
    public AppIdList? DelInvisibleList { get; set; }

    /// <summary>
    /// <para>应用是否对租户内全员可用。true：是；false：否；不传：保持当前状态。未指定且当前为全员可用、或设置为 true 时，add_visible_list/del_visible_list 不生效</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("is_visible_to_all")]
    public bool? IsVisibleToAll { get; set; }
}
