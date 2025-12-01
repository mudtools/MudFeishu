// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Cards;

/// <summary>
/// 机器人单聊即时提醒响应体
/// </summary>
public class BotTimeSentiveResult
{

    /// <summary>
    /// <para>失败原因</para>
    /// </summary>
    [JsonPropertyName("failed_user_reasons")]
    public TimeSentiveFailedReason[]? FailedUserReasons { get; set; }

}

/// <summary>
/// <para>失败原因</para>
/// </summary>
public class TimeSentiveFailedReason
{
    /// <summary>
    /// <para>错误码</para>
    /// </summary>
    [JsonPropertyName("error_code")]
    public int? ErrorCode { get; set; }

    /// <summary>
    /// <para>错误信息</para>
    /// </summary>
    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// <para>用户id</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }
}