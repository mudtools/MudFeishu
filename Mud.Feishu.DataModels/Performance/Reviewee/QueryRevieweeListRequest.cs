// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 获取被评估人信息请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class QueryRevieweeListRequest
{
    /// <summary>
    /// <para>周期 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：6992035450862224940</para>
    /// </summary>
    [JsonPropertyName("semester_id")]
    public string? SemesterId { get; set; }

    /// <summary>
    /// <para>用户 ID 列表（0~50 个），与入参 user_id_type 类型一致，用于查询指定被评估人</para>
    /// <para>必填：否</para>
    /// <para>示例值：["ou_3245842393d09e9428ad4655da6e30b3"]</para>
    /// </summary>
    [JsonPropertyName("user_ids")]
    public string[]? UserIds { get; set; }

    /// <summary>
    /// <para>项目 ID 列表，用于查询指定项目下的被评估人信息</para>
    /// <para>必填：否</para>
    /// <para>示例值：["7266780609384392723"]</para>
    /// </summary>
    [JsonPropertyName("activity_ids")]
    public string[]? ActivityIds { get; set; }
}
