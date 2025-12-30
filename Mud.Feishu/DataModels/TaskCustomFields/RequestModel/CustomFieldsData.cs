// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.TaskCustomFields;

/// <summary>
/// 自定义字段数据
/// </summary>
public class CustomFieldsData
{
    /// <summary>
    /// <para>字段名称，支持最大50个字符。</para>
    /// <para>必填：否</para>
    /// <para>示例值：优先级</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

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
    /// <para>单选设置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("single_select_setting")]
    public SelectSettingInfo? SingleSelectSetting { get; set; }


    /// <summary>
    /// <para>多选设置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("multi_select_setting")]
    public SelectSettingInfo? MultiSelectSetting { get; set; }

    /// <summary>
    /// <para>文本类型设置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("text_setting")]
    public object? TextSetting { get; set; }
}
