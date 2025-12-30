// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Task;

namespace Mud.Feishu.DataModels.TaskCustomFields;

/// <summary>
/// 自定义字段信息
/// </summary>
public class CustomFieldInfo
{
    /// <summary>
    /// <para>自定义字段的GUID</para>
    /// <para>必填：否</para>
    /// <para>示例值：34d4b29f-3d58-4bc5-b752-6be80fb687c8</para>
    /// </summary>
    [JsonPropertyName("guid")]
    public string? Guid { get; set; }

    /// <summary>
    /// <para>自定义字段名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：优先级</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>自定义字段类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：number</para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>数字类型的字段设置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("number_setting")]
    public NumberSettingData? NumberSetting { get; set; }


    /// <summary>
    /// <para>人员类型的字段设置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("member_setting")]
    public MemberSettingData? MemberSetting { get; set; }

    /// <summary>
    /// <para>时间日期类型的字段设置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("datetime_setting")]
    public DatetimeSettingData? DatetimeSetting { get; set; }

    /// <summary>
    /// <para>单选类型的字段设置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("single_select_setting")]
    public SelectSettingInfo? SingleSelectSetting { get; set; }

    /// <summary>
    /// <para>多选类型的字段设置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("multi_select_setting")]
    public SelectSettingInfo? MultiSelectSetting { get; set; }

    /// <summary>
    /// <para>创建人</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("creator")]
    public TaskMember? Creator { get; set; }

    /// <summary>
    /// <para>自定义字段创建的时间戳(ms)</para>
    /// <para>必填：否</para>
    /// <para>示例值：1688196600000</para>
    /// </summary>
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    /// <summary>
    /// <para>自定义字段的更新时间戳(ms)</para>
    /// <para>必填：否</para>
    /// <para>示例值：1688196600000</para>
    /// </summary>
    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }

    /// <summary>
    /// <para>文本类型设置（目前文本类型没有可设置项）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("text_setting")]
    public object? TextSetting { get; set; }
}
