// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Approval;

/// <summary>
/// 审批附件
/// </summary>
public class ApprovalFileInfo
{
    /// <summary>
    /// <para>附件路径</para>
    /// <para>示例值：https://p3-approval-sign.byteimg.com/lark-approval-attachment/image/20220714/1/332f3596-0845-4746-a4bc-818d54ad435b.png~tplv-ottatrvjsm-image.image?x-expires=1659033558&amp;x-signature=6edF3k%2BaHeAuvfcBRGOkbckoUl4%3D#.png</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// <para>附件大小。单位：字节</para>
    /// <para>示例值：186823</para>
    /// </summary>
    [JsonPropertyName("file_size")]
    public int? FileSize { get; set; }

    /// <summary>
    /// <para>附件标题</para>
    /// <para>示例值：e018906140ed9388234bd03b0.png</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>附件类别</para>
    /// <para>- image：图片</para>
    /// <para>- attachment：附件，与上传时选择的类型一致</para>
    /// <para>示例值：image</para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}
