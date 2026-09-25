// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 查询猎头供应商下猎头列表请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class GetAgencyAccountRequest
{
    /// <summary>
    /// <para>猎头供应商 ID，可通过搜索猎头供应商列表接口获取</para>
    /// <para>必填：是</para>
    /// <para>示例值：7398623155442682156</para>
    /// </summary>
    [JsonPropertyName("supplier_id")]
    public string? SupplierId { get; set; }

    /// <summary>
    /// <para>猎头状态：0 正常 / 1 已禁用 / 2 猎头自助停用</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// <para>角色：0 管理员 / 1 顾问</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("role")]
    public int? Role { get; set; }
}
