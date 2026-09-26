// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 更新员工状态请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class PatchEmployeeRequest
{
    /// <summary>
    /// <para>操作类型：1 转正 / 2 离职 / 3 恢复至待入职 / 4 撤销离职 / 5 撤销转正</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("operation")]
    public int? Operation { get; set; }

    /// <summary>
    /// <para>转正信息（operation = 1 时必填）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("conversion_info")]
    public EmployeeConversionInfo? ConversionInfo { get; set; }

    /// <summary>
    /// <para>离职信息（operation = 2 时必填）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("overboard_info")]
    public EmployeeOverboardInfo? OverboardInfo { get; set; }
}
