namespace Mud.Feishu.DataModels.EmployeeType;

/// <summary>
/// 新建的人员类型信息请求体。
/// </summary>
public class EmployeeTypeEnumRequest
{
    /// <summary>
    /// 人员类型的选项内容。
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; set; }

    /// <summary>
    /// 人员类型的选项类型。新增人员类型时固定取值为 2 即可。
    /// <para>可选值有：1：内置类型，只读。新增人员类型时不支持选择该类型。2：自定义。</para>
    /// </summary>
    [JsonPropertyName("enum_type")]
    public required int EnumType { get; set; }

    /// <summary>
    /// 人员类型的选项激活状态。只有已激活的选项可以用于配置用户属性。
    /// <para>可选值有：1：激活 2：未激活</para>
    /// </summary>
    [JsonPropertyName("enum_status")]
    public int EnumStatus { get; set; }

    /// <summary>
    /// 选项内容的国际化配置。
    /// </summary>
    [JsonPropertyName("i18n_content")]
    public List<I18nContent>? I18nContent { get; set; }
}