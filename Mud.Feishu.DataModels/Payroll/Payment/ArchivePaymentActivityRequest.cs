// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Payroll;

/// <summary>
/// 封存发薪活动请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Payroll")]
public class ArchivePaymentActivityRequest
{
    /// <summary>
    /// <para>发薪活动 ID，可通过查询发薪活动列表接口获取；仅当发薪活动状态为审批通过时方可封存</para>
    /// <para>必填：是</para>
    /// <para>示例值：7433255159377444875</para>
    /// </summary>
    [JsonPropertyName("activity_id")]
    public string? ActivityId { get; set; }
}
