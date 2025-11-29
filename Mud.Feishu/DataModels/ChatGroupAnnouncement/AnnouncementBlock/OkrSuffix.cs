// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroupNotice;

/// <summary>
/// <para>OKR Block</para>
/// </summary>
public abstract class OkrSuffix
{
    /// <summary>
    /// <para>是否设置过私密权限</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("confidential")]
    public bool? Confidential { get; set; }

    /// <summary>
    /// <para>位置编号</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("position")]
    public int? Position { get; set; }

    /// <summary>
    /// <para>打分信息</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("score")]
    public int? Score { get; set; }

    /// <summary>
    /// <para>是否可见</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// <para>默认值：true</para>
    /// </summary>
    [JsonPropertyName("visible")]
    public bool? Visible { get; set; }

    /// <summary>
    /// <para>权重</para>
    /// <para>必填：否</para>
    /// <para>示例值：0.5</para>
    /// </summary>
    [JsonPropertyName("weight")]
    public float? Weight { get; set; }

    /// <summary>
    /// <para>进展信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("progress_rate")]
    public OkrProgressRate? ProgressRate { get; set; }

    /// <summary>
    /// <para>文本内容</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("content")]
    public BlockText? Content { get; set; }
}

/// <summary>
/// <para>OKR Objective Block</para>
/// </summary>
public class BlockOkrObjective : OkrSuffix
{
    /// <summary>
    /// <para>Objective ID</para>
    /// <para>必填：否</para>
    /// <para>示例值："7109022409227026460"</para>
    /// </summary>
    [JsonPropertyName("objective_id")]
    public string? ObjectiveId { get; set; }
}

/// <summary>
/// <para>OKR Key Result</para>
/// </summary>
public class BlockOkrKeyResult : OkrSuffix
{
    /// <summary>
    /// <para>Key Result 的 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值："7109022573011894300"</para>
    /// </summary>
    [JsonPropertyName("kr_id")]
    public string? KrId { get; set; }
}

/// <summary>
/// <para>进展信息</para>
/// </summary>
public class OkrProgressRate
{
    /// <summary>
    /// <para>状态模式</para>
    /// <para>必填：否</para>
    /// <para>示例值："simple"</para>
    /// <para>可选值：<list type="bullet">
    /// <item>simple：简单模式</item>
    /// <item>advanced：高级模式</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    /// <summary>
    /// <para>当前进度</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("current")]
    public float? Current { get; set; }

    /// <summary>
    /// <para>当前进度百分比，simple mode 下使用</para>
    /// <para>必填：否</para>
    /// <para>示例值：100</para>
    /// </summary>
    [JsonPropertyName("percent")]
    public float? Percent { get; set; }

    /// <summary>
    /// <para>进展状态</para>
    /// <para>必填：否</para>
    /// <para>示例值："normal"</para>
    /// <para>可选值：<list type="bullet">
    /// <item>unset：未设置</item>
    /// <item>normal：正常</item>
    /// <item>risk：有风险</item>
    /// <item>extended：已延期</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("progress_status")]
    public string? ProgressStatus { get; set; }

    /// <summary>
    /// <para>进度起始值，advanced模式使用</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("start")]
    public float? Start { get; set; }

    /// <summary>
    /// <para>状态类型</para>
    /// <para>必填：否</para>
    /// <para>示例值："default"</para>
    /// <para>可选值：<list type="bullet">
    /// <item>default：风险最高的Key Result状态</item>
    /// <item>custom：自定义</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("status_type")]
    public string? StatusType { get; set; }

    /// <summary>
    /// <para>进度目标值，advanced模式使用</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("target")]
    public float? Target { get; set; }
}