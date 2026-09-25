// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 猎头供应商信息（获取/按名称查询猎头供应商响应子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class Agency
{
    /// <summary>
    /// <para>猎头供应商 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：6898173495386147079</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>猎头供应商名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：超越猎头公司</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>供应商联系人 ID，仅用于唯一标识</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_f476cb099ac9227c9bae09ce46112579</para>
    /// </summary>
    [JsonPropertyName("contactor_id")]
    public string? ContactorId { get; set; }

    /// <summary>
    /// <para>供应商联系人名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("contactor_name")]
    public I18nName? ContactorName { get; set; }
}
