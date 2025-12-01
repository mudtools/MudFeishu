// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.CardMessageStream;

/// <summary>
/// 删除应用消息流卡片请求体
/// </summary>
public class DeleteAppCardMessageStreamRequest
{
    /// <summary>
    /// <para>应用消息卡片</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("feed_cards")]
    public AppMessageCardDeleteData[]? FeedCards { get; set; }
}

/// <summary>
/// 应用消息卡片
/// </summary>
public record AppMessageCardDeleteData
{
    /// <summary>
    /// <para>业务 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：ed381d34-49ac-4876-8d9e-23447acb587e</para>
    /// </summary>
    [JsonPropertyName("biz_id")]
    public string BizId { get; set; } = string.Empty;

    /// <summary>
    /// <para>用户 ID（ID 类型与 user_id_type 的取值一致。如果</para>
    /// <para>是商店应用，因不支持获取用户 userID 权限，所以无法值使用 user_id 类型的用户 ID）</para>
    /// <para>必填：是</para>
    /// <para>示例值：ou_88553eda9014c201e6969b478895c223</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;
}