// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalComments;

/// <summary>
/// 创建评论请求体
/// </summary>
public class CreateCommentRequest
{
    /// <summary>
    /// <para>评论内容，JSON 格式，传入时需要压缩转义为字符串。以下示例值未转义，你可参考请求体示例中的示例 content 进行编辑。</para>
    /// <para>**JSON 内参数说明**：</para>
    /// <para>- text：string 类型，评论文本内容。</para>
    /// <para>- files：Attachment[] 类型，附件信息。</para>
    /// <para>- url：string 类型，附件链接。</para>
    /// <para>- thumbnailURL：string 类型，缩略图链接。</para>
    /// <para>- fileSize：int64 类型，文件大小。</para>
    /// <para>- title：string 类型，标题。</para>
    /// <para>- type：string 类型，附件类型，取值 image 表示图片类型。</para>
    /// <para>**注意**：</para>
    /// <para>- 如需 @用户，则需要在该参数内设置用户名的文本，例如 `@username`，同时通过 at_info_list 参数实现 @ 效果。</para>
    /// <para>- 对于附件，在 PC 端使用 HTTP 资源链接传图片资源可能会导致缩略图异常，建议使用 HTTPS 传资源附件。</para>
    /// <para>必填：否</para>
    /// <para>示例值：{\"text\":\"@username艾特展示\",\"files\":[{\"url\":\"xxx\",\"fileSize\":155149,\"title\":\"9a9fedc5cfb01a4a20c715098.png\",\"type\":\"image\",\"extra\":\"\"}]}</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// <para>评论中艾特人信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("at_info_list")]
    public CommentAtInfo[]? AtInfoLists { get; set; }


    /// <summary>
    /// <para>父评论 ID，如果是回复评论，需要传入该值。获取方式：</para>
    /// <para>- 调用当前接口成功后会返回本次评论的 ID，你可以保存用于下次使用。</para>
    /// <para>必填：否</para>
    /// <para>示例值：7081516627711524883</para>
    /// </summary>
    [JsonPropertyName("parent_comment_id")]
    public string? ParentCommentId { get; set; }

    /// <summary>
    /// <para>评论 ID。如果需要编辑、删除一条评论，则需要将该评论的 ID 传入当前参数。获取方式：</para>
    /// <para>- 调用当前接口成功后会返回本次评论的 ID，你可以保存用于下次使用。</para>
    /// <para>必填：否</para>
    /// <para>示例值：7081516627711524883</para>
    /// </summary>
    [JsonPropertyName("comment_id")]
    public string? CommentId { get; set; }

    /// <summary>
    /// <para>是否不启用 Bot，取值为 true 时只同步数据，不触发 Bot。</para>
    /// <para>**说明**：飞书审批中自定义审批填写 false，其他情况填写 true。</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("disable_bot")]
    public bool? DisableBot { get; set; }

    /// <summary>
    /// <para>附加字段，JSON 格式，传入时需要压缩转义为字符串。</para>
    /// <para>必填：否</para>
    /// <para>示例值：{\"a\":\"a\"}</para>
    /// </summary>
    [JsonPropertyName("extra")]
    public string? Extra { get; set; }
}
