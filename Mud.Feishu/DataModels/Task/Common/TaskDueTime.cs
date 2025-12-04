// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Task;

/// <summary>
/// <para>任务截止时间。</para>
/// </summary>
public record TaskDueTime
{
    /// <summary>
    /// <para>截止时间/日期的时间戳，距1970-01-01 00:00:00 UTC的毫秒数。如果截止时间是一个日期，需要把日期转换成时间戳，并设置 is_all_day=true</para>
    /// <para>必填：否</para>
    /// <para>示例值：1675454764000</para>
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    /// <summary>
    /// <para>是否截止到一个日期。如果设为true，timestamp中只有日期的部分会被解析和存储。</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("is_all_day")]
    public bool? IsAllDay { get; set; }
}