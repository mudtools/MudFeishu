// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.TaskCustomFields;

/// <summary>
/// 更新自定义字段请求体
/// </summary>
public class UpdateCustomFieldsRequest
{
    /// <summary>
    /// <para>要修改的自定义字段数据</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("custom_field")]
    public CustomFieldsData? CustomField { get; set; }

    /// <summary>
    /// <para>要修改的自定义字段类型，支持：</para>
    /// <para>* `name`：自定义字段名称。</para>
    /// <para>* `number_setting` ：数字类型设置（当且仅当要更新的自定义字段类型是数字时)</para>
    /// <para>* `member_setting` ：人员类型设置（当且仅当要更新的自定义字段类型是人员时)</para>
    /// <para>* `datetime_setting` ：日期类型设置 (当且仅当要更新的自定义字段类型是日期时)</para>
    /// <para>* `single_select_setting`：单选类型设置 (当且仅当要更新的自定义字段类型是单选时)</para>
    /// <para>* `multi_select_setting`：多选类型设置 (当且仅当要更新的自定义字段类型是多选时)</para>
    /// <para>* `text_setting`: 文本类型设置（当前无可设置项）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("update_fields")]
    public string[]? UpdateFields { get; set; }
}
