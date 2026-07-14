// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive;

/// <summary>
/// 订阅状态响应体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Drive")]
public class FileSubscriptionOOpsResult
{
    /// <summary>
    /// <para>订阅关系ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：1234567890987654321</para>
    /// </summary>
    [JsonPropertyName("subscription_id")]
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// <para>订阅类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：comment_update</para>
    /// <para>可选值：<list type="bullet">
    /// <item>comment_update：评论更新</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("subscription_type")]
    public string? SubscriptionType { get; set; }

    /// <summary>
    /// <para>是否订阅</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("is_subcribe")]
    public bool? IsSubcribe { get; set; }

    /// <summary>
    /// <para>文档类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：docx</para>
    /// <para>可选值：<list type="bullet">
    /// <item>doc：旧版文档</item>
    /// <item>docx：新版文档</item>
    /// <item>wiki：知识库</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("file_type")]
    public string? FileType { get; set; }
}
