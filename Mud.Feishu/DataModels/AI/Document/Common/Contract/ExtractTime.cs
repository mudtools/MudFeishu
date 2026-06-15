// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AI;


/// <summary>
/// <para>期限相关信息，包括开始日期、结束日期、有效时长</para>
/// </summary>
public class ExtractTime
{
    /// <summary>
    /// <para>开始时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：2020-07-01</para>
    /// </summary>
    [JsonPropertyName("time_start")]
    public string? TimeStart { get; set; }

    /// <summary>
    /// <para>结束时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：2022-06-30</para>
    /// </summary>
    [JsonPropertyName("time_end")]
    public string? TimeEnd { get; set; }

    /// <summary>
    /// <para>原文中抽取出的开始时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：2020年07月1日</para>
    /// </summary>
    [JsonPropertyName("original_time_start")]
    public string? OriginalTimeStart { get; set; }

    /// <summary>
    /// <para>原文中抽取出的结束时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：2022年6月30日</para>
    /// </summary>
    [JsonPropertyName("original_time_end")]
    public string? OriginalTimeEnd { get; set; }

    /// <summary>
    /// <para>原文中关于开始时间的描述</para>
    /// <para>必填：否</para>
    /// <para>示例值：本协议自有效期自【2020】年【07】月【1】日至【2022】年【6】月【30】日，有效期2年。</para>
    /// </summary>
    [JsonPropertyName("text_start")]
    public string? TextStart { get; set; }

    /// <summary>
    /// <para>原文中关于结束时间的描述</para>
    /// <para>必填：否</para>
    /// <para>示例值：本协议自有效期自【2020】年【07】月【1】日至【2022】年【6】月【30】日，有效期2年。</para>
    /// </summary>
    [JsonPropertyName("text_end")]
    public string? TextEnd { get; set; }

    /// <summary>
    /// <para>合同持续时长</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("initial_term")]
    public ExtractTerm? InitialTerm { get; set; }


    /// <summary>
    /// <para>原文中关于持续时间的描述</para>
    /// <para>必填：否</para>
    /// <para>示例值：2年</para>
    /// </summary>
    [JsonPropertyName("text_initial_term")]
    public string? TextInitialTerm { get; set; }
}