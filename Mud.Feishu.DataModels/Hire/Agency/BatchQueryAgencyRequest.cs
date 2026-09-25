// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 搜索猎头供应商列表请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class BatchQueryAgencyRequest
{
    /// <summary>
    /// <para>猎头供应商 ID 列表（最多 20 个），传该值时以其为准，其余查询字段失效</para>
    /// <para>必填：否</para>
    /// <para>示例值：["7412902352778840358"]</para>
    /// </summary>
    [JsonPropertyName("agency_supplier_id_list")]
    public string[]? AgencySupplierIdList { get; set; }

    /// <summary>
    /// <para>搜索关键字，可传入名称或邮箱</para>
    /// <para>必填：否</para>
    /// <para>示例值：猎头</para>
    /// </summary>
    [JsonPropertyName("keyword")]
    public string? Keyword { get; set; }

    /// <summary>
    /// <para>筛选项，相同的 Key 仅可传一次；支持 cooperation_create_time（范围）、cooperation_status、supplier_area、label_id_list（值筛选）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("filter_list")]
    public CommonFilter[]? FilterList { get; set; }
}
