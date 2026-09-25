// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 开放职位请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class OpenJobRequest
{
    /// <summary>
    /// <para>到期日期（毫秒时间戳）；is_never_expired 为 false 时该字段必填且须晚于当前时间</para>
    /// <para>必填：条件必填</para>
    /// </summary>
    [JsonPropertyName("expiry_time")]
    public long? ExpiryTime { get; set; }

    /// <summary>
    /// <para>是否长期有效：true 长期有效 / false 指定到期日期</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("is_never_expired")]
    public bool? IsNeverExpired { get; set; }
}
