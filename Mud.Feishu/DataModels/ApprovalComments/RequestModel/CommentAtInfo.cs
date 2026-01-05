// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalComments;

/// <summary>
/// <para>评论中艾特人信息</para>
/// </summary>
public class CommentAtInfo
{
    /// <summary>
    /// <para>被艾特人的 ID，ID 类型与查询参数 user_id_type 取值一致。</para>
    /// <para>必填：是</para>
    /// <para>示例值：579fd9c4</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// <para>被艾特人的姓名</para>
    /// <para>必填：是</para>
    /// <para>示例值：张敏</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>被艾特人在评论中的位置，从 0 开始。用于偏移覆盖。例如：</para>
    /// <para>- 取值为 0 时的效果：@username 示例文本</para>
    /// <para>- 取值为 2 时的效果：示例 @username 文本</para>
    /// <para>- 取值为 4 时的效果：示例文本 @username</para>
    /// <para>**注意**：该参数生效方式是覆盖生效，因此你需要先通过 content 参数设置用户名称的文本内容，然后再通过该参数将实际生效的@效果覆盖到用户名称的文本内容上。</para>
    /// <para>必填：是</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("offset")]
    public string Offset { get; set; } = string.Empty;
}
