// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.CardMessageStream;

/// <summary>
/// 更新应用消息流卡片请求体
/// </summary>
public class UpdateAppCardMessageStreamRequest
{
    /// <summary>
    /// <para>应用消息卡片</para>
    /// <para>必填：否</para>
    /// <para>最大长度：20</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("feed_cards")]
    public UserOpenAppFeedCardUpdater[]? FeedCards { get; set; }
}


/// <summary>
/// <para>应用消息卡片</para>
/// </summary>
public class UserOpenAppFeedCardUpdater
{
    /// <summary>
    /// <para>应用消息卡片</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("app_feed_card")]
    public OpenAppMessageCard AppMessageCard { get; set; } = new();

    /// <summary>
    /// <para>用户 ID（ID 类型与 user_id_type 的取值一致。如果是商店应用，因不支持获取用户 userID 权限，所以无法值使用 user_id 类型的用户 ID）</para>
    /// <para>必填：是</para>
    /// <para>示例值：ou_a0553eda9014c201e6969b478895c230</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// <para>更新字段列表</para>
    /// <para>必填：是</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：标题</item>
    /// <item>2：头像 key</item>
    /// <item>3：预览</item>
    /// <item>10：状态标签</item>
    /// <item>11：按钮</item>
    /// <item>12：跳转链接</item>
    /// <item>13：即时提醒状态</item>
    /// <item>101：展示时间更新到当前</item>
    /// <item>102：排序时间更新到当前</item>
    /// <item>103：进行通知</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("update_fields")]
    public string[] UpdateFields { get; set; } = [];
}
