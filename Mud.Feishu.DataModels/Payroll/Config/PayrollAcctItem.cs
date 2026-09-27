// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Payroll;

/// <summary>
/// 算薪项（acct_item）信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Payroll")]
public class PayrollAcctItem
{
    /// <summary>
    /// <para>算薪项 ID</para>
    /// <para>示例值：7169773973790425132</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>算薪项名称（多语言）</para>
    /// </summary>
    [JsonPropertyName("i18n_names")]
    public I18nContent[]? I18nNames { get; set; }

    /// <summary>
    /// <para>算薪项分类 ID</para>
    /// <para>示例值：7169773973790425132</para>
    /// </summary>
    [JsonPropertyName("category_id")]
    public string? CategoryId { get; set; }

    /// <summary>
    /// <para>算薪项数据类型：1（文本）/ 2（金额）/ 3（数值）/ 4（百分数）/ 5（日期）/ 6（引用项）</para>
    /// </summary>
    [JsonPropertyName("data_type")]
    public int? DataType { get; set; }

    /// <summary>
    /// <para>小数位数</para>
    /// <para>示例值：2</para>
    /// </summary>
    [JsonPropertyName("decimal_places")]
    public int? DecimalPlaces { get; set; }

    /// <summary>
    /// <para>启用状态：1（已启用）/ 2（已停用）</para>
    /// </summary>
    [JsonPropertyName("active_status")]
    public int? ActiveStatus { get; set; }
}
