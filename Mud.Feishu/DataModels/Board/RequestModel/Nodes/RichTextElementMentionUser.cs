// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Board;

/// <summary>
/// <para>@用户类别信息（当前元素为@用户类别时候需要有当前字段）</para>
/// </summary>
public class RichTextElementMentionUser
{
    /// <summary>
    /// <para>用户openID，可通过 [获取指定用户的 Open ID](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-openid) 获取</para>
    /// <para>必填：是</para>
    /// <para>示例值：ou_da5****************dfe</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// <para>文字属性</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("text_style")]
    public RichTextElementTextStyle? TextStyle { get; set; }
}
