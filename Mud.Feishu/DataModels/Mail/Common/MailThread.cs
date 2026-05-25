// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Mail;

/// <summary>
/// <para>邮件会话</para>
/// </summary>
public class MailThread
{
    /// <summary>
    /// <para>会话ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：xx</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>会话内的最新的一封邮件摘要，用于快速预览邮件核心内容</para>
    /// <para>必填：否</para>
    /// <para>示例值：hello world</para>
    /// </summary>
    [JsonPropertyName("body_preview")]
    public string? BodyPreview { get; set; }

    /// <summary>
    /// <para>会话中的邮件列表</para>
    /// <para>必填：否</para>
    /// <para>最大长度：999</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("messages")]
    public MialMessage[]? Messages { get; set; }


}