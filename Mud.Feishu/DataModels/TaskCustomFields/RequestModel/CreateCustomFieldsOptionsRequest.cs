// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.TaskCustomFields;

/// <summary>
/// 创建自定义任务选项请求体
/// </summary>
public class CreateCustomFieldsOptionsRequest
{
    /// <summary>
    /// <para>选项名称，最大50个字符。</para>
    /// <para>必填：否</para>
    /// <para>示例值：高优</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>颜色索引值，支持0～54中的一个数字。如果不填写，则会随机选一个。</para>
    /// <para>必填：否</para>
    /// <para>示例值：10</para>
    /// <para>最大值：54</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("color_index")]
    public int? ColorIndex { get; set; }

    /// <summary>
    /// <para>要放到某个option之前的option_guid</para>
    /// <para>必填：否</para>
    /// <para>示例值：2bd905f8-ef38-408b-aa1f-2b2ad33b2913</para>
    /// </summary>
    [JsonPropertyName("insert_before")]
    public string? InsertBefore { get; set; }

    /// <summary>
    /// <para>要放到某个option之后的option_guid</para>
    /// <para>必填：否</para>
    /// <para>示例值：b13adf3c-cad6-4e02-8929-550c112b5633</para>
    /// </summary>
    [JsonPropertyName("insert_after")]
    public string? InsertAfter { get; set; }

    /// <summary>
    /// <para>是否隐藏</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// <para>默认值：false</para>
    /// </summary>
    [JsonPropertyName("is_hidden")]
    public bool? IsHidden { get; set; }
}
