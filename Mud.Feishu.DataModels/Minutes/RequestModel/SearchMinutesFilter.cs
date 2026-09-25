// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Minutes;

/// <summary>
/// 妙记搜索过滤条件
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Minutes")]
public class SearchMinutesFilter
{
    /// <summary>
    /// <para>按妙记创建者过滤，传入用户 open_id 列表，支持传入 me 表示当前用户；不设置时不过滤</para>
    /// <para>必填：否</para>
    /// <para>示例值：["ou_612b787ccd3259fb3c816b3f678dxxxx"]</para>
    /// </summary>
    [JsonPropertyName("owner_ids")]
    public string[]? OwnerIds { get; set; }

    /// <summary>
    /// <para>按妙记参与者过滤，传入用户 open_id 列表，支持传入 me 表示当前用户；不设置时不过滤</para>
    /// <para>必填：否</para>
    /// <para>示例值：["ou_612b787ccd3259fb3c816b3f678dxxxx"]</para>
    /// </summary>
    [JsonPropertyName("participant_ids")]
    public string[]? ParticipantIds { get; set; }

    /// <summary>
    /// <para>按妙记创建时间过滤，时间范围最大为 1 个月，start_time 须小于等于 end_time</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public MinutesTimeRange? CreateTime { get; set; }
}
