// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 禁用/取消禁用猎头请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class OperateAgencyAccountRequest
{
    /// <summary>
    /// <para>操作类型：1 禁用 / 2 取消禁用</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("option")]
    public int? Option { get; set; }

    /// <summary>
    /// <para>猎头 ID，可通过查询猎头供应商下猎头列表接口获取</para>
    /// <para>必填：是</para>
    /// <para>示例值：7398623155442682156</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>禁用原因，仅当 option 为 1（禁用）时必填</para>
    /// <para>必填：否</para>
    /// <para>示例值：这个人特别不负责</para>
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}
