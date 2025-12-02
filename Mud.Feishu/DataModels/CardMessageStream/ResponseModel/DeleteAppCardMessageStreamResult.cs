// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.CardMessageStream;

/// <summary>
/// 删除应用消息流卡片 响应体
/// </summary>
public class DeleteAppCardMessageStreamResult
{
    /// <summary>
    /// <para>失败的卡片</para>
    /// </summary>
    [JsonPropertyName("failed_cards")]
    public DeleteFailedUserAppMessageCardItem[]? FailedCards { get; set; }
}

/// <summary>
/// 删除失败的卡片
/// </summary>
public class DeleteFailedUserAppMessageCardItem
{
    /// <summary>
    /// <para>业务 ID</para>
    /// </summary>
    [JsonPropertyName("biz_id")]
    public string BizId { get; set; } = string.Empty;

    /// <summary>
    /// <para>用户 ID</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// <para>原因</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：未知</item>
    /// <item>1：无权限</item>
    /// <item>2：未创建</item>
    /// <item>3：频率限制</item>
    /// <item>4：重复</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}
