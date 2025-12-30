// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.TaskCustomFields;

/// <summary>
/// 创建自定义字段请求体
/// </summary>
public class CreateCustomFieldsRequest
{
    /// <summary>
    /// <para>自定义字段要归属的资源类型，支持"tasklist"</para>
    /// <para>必填：是</para>
    /// <para>示例值：tasklist</para>
    /// <para>默认值：tasklist</para>
    /// </summary>
    [JsonPropertyName("resource_type")]
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// <para>自定义字段要归属的资源ID，当`resource_type`为"tasklist"时必须填写清单的GUID。</para>
    /// <para>必填：是</para>
    /// <para>示例值：ec5ed63d-a4a9-44de-a935-7ba243471c0a</para>
    /// <para>最大长度：100</para>
    /// </summary>
    [JsonPropertyName("resource_id")]
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>
    /// <para>字段名称，最大50个字符。</para>
    /// <para>必填：是</para>
    /// <para>示例值：优先级</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>自定义字段类型。</para>
    /// <para>必填：是</para>
    /// <para>示例值：number</para>
    /// <para>可选值：<list type="bullet">
    /// <item>number：数字</item>
    /// <item>datetime：日期</item>
    /// <item>member：成员</item>
    /// <item>single_select：单选</item>
    /// <item>multi_select：多选</item>
    /// <item>text：文本</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

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
    public SelectSettingData? SingleSelectSetting { get; set; }


    /// <summary>
    /// <para>多选设置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("multi_select_setting")]
    public SelectSettingData? MultiSelectSetting { get; set; }

    /// <summary>
    /// <para>文本类型设置（目前文本类型没有可设置项）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("text_setting")]
    public object? TextSetting { get; set; }
}
