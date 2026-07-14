// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive;

/// <summary>
/// 添加/取消表情回应请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Drive")]
public class UpdateReactionCommentReactionRequest
{
    /// <summary>
    /// <para>操作类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：add</para>
    /// <para>最大长度：100</para>
    /// <para>最小长度：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>add：添加表情回复</item>
    /// <item>delete：删除添加的reaction</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// <para>回复 ID</para>
    /// <para>可通过调用 添加回复、获取回复信息 接口获取</para>
    /// <para>必填：是</para>
    /// <para>示例值：1234567890</para>
    /// <para>最大长度：100</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("reply_id")]
    public string ReplyId { get; set; } = string.Empty;

    /// <summary>
    /// <para>reaction 类型</para>
    /// <para>可选值：ANGRY, APPLAUSE, ATTENTION, AWESOME, BEAR, BEER, BETRAYED, BIGKISS, BLACKFACE, BLUBBER, BLUSH, BOMB, CAKE, CHUCKLE, CLAP, CLEAVER, COMFORT, CRAZY, CRY, CUCUMBER, DETERGENT, DIZZY, DONE, DONNOTGO, DROOL, DROWSY, DULL, DULLSTARE, EATING, EMBARRASSED, ENOUGH, ERROR, EYESCLOSED, FACEPALM, FINGERHEART, FISTBUMP, FOLLOWME, FROWN, GIFT, GLANCE, GOODJOB, HAMMER, HAUGHTY, HEADSET, HEART, HEARTBROKEN, HIGHFIVE, HUG, HUSKY, INNOCENTSMILE, JIAYI, JOYFUL, KISS, LAUGH, LIPS, LOL, LOOKDOWN, LOVE, MONEY, MUSCLE, NOSEPICK, OBSESSED, OK, PARTY, PETRIFIED, POOP, PRAISE, PROUD, PUKE, RAINBOWPUKE, ROSE, SALUTE, SCOWL, SHAKE, SHHH, SHOCKED, SHOWOFF, SHY, SICK, SILENT, SKULL, SLAP, SLEEP, SLIGHT, SMART, SMILE, SMIRK, SMOOCH, SMUG, SOB, SPEECHLESS, SPITBLOOD, STRIVE, SWEAT, TEARS, TEASE, TERROR, THANKS, THINKING, THUMBSUP, TOASTED, TONGUE, TRICK, UPPERLEFT, WAIL, WAVE, WELLDONE, WHAT, WHIMPER, WINK, WITTY, WOW, WRONGED, XBLUSH, YAWN, YEAH, FIREWORKS, BULL, CALF, AWESOMEN, 2021, CANDIEDHAWS, REDPACKET, FORTUNE, LUCK, FIRECRACKER, Yes, No, Get, LGTM, Lemon, EatingFood, Hundred, MinusOne, ThumbsDown, Fire, OKR, Drumstick, BubbleTea, Loudspeaker, Pin, Coffee, Alarm, Trophy, Music, Typing, Pepper, CheckMark, CrossMark.</para>
    /// <para>必填：是</para>
    /// <para>示例值：ANGRY</para>
    /// <para>最大长度：100</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("reaction_type")]
    public string ReactionType { get; set; } = string.Empty;
}
