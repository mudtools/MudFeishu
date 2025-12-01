// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.CardMessageStream;

/// <summary>
/// 更新消息流卡片按钮请求体
/// </summary>
public class UpdateCardMessageStreamButtonRequest
{
    /// <summary>
    /// <para>用户 ID 列表（ID 类型与 user_id_type 的取值一致。如果是商店应用，因不支持获取用户 user ID 权限，所以无法使用 user_id 类型的用户 ID）</para>
    /// <para>必填：否</para>
    /// <para>最大长度：20</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("user_ids")]
    public string[]? UserIds { get; set; }

    /// <summary>
    /// <para>群 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：oc_a0553eda9014c201e6969b478895c230</para>
    /// </summary>
    [JsonPropertyName("chat_id")]
    public string ChatId { get; set; } = string.Empty;

    /// <summary>
    /// <para>交互按钮（非必填字段，如未传入该字段，则不展示按钮；最多展示 2 个按钮）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("buttons")]
    public OpenAppMessageCardButtons? Buttons { get; set; }
}
