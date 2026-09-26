// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 获取被评估人关键指标结果请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class QueryMetricDetailListRequest
{
    /// <summary>
    /// <para>周期 ID，1 次只允许查询 1 个周期，可通过获取周期接口获得</para>
    /// <para>必填：是</para>
    /// <para>示例值：6992035450862224940</para>
    /// </summary>
    [JsonPropertyName("semester_id")]
    public string? SemesterId { get; set; }

    /// <summary>
    /// <para>被评估人 ID 列表，与入参 user_id_type 类型一致（1~50 个）</para>
    /// <para>必填：是</para>
    /// <para>示例值：["ou_3245842393d09e9428ad4655da6e30b3"]</para>
    /// </summary>
    [JsonPropertyName("reviewee_user_ids")]
    public string[]? RevieweeUserIds { get; set; }
}
