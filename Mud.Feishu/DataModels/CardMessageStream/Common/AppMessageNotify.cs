// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.CardMessageStream;

/// <summary>
/// <para>通知设置，当前可设置通知是否关闭，为空时默认进行通知</para>
/// </summary>
public class AppMessageNotify
{
    /// <summary>
    /// <para>是否关闭通知</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// <para>默认值：false</para>
    /// </summary>
    [JsonPropertyName("close_notify")]
    public bool? CloseNotify { get; set; }

    /// <summary>
    /// <para>自定义语音播报文本内容（仅支持移动端）</para>
    /// <para>必填：否</para>
    /// <para>示例值：您有新的订单</para>
    /// </summary>
    [JsonPropertyName("custom_sound_text")]
    public string? CustomSoundText { get; set; }

    /// <summary>
    /// <para>是否播报自定义语音（仅支持移动端；播报语音包暂不支持切换，默认为女声）</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// <para>默认值：false</para>
    /// </summary>
    [JsonPropertyName("with_custom_sound")]
    public bool? WithCustomSound { get; set; }
}