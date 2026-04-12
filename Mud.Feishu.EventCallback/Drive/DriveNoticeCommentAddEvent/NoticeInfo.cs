// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Drive;

/// <summary>
/// 通知元信息
/// </summary>
public class NoticeInfo
{
    /// <summary>
    /// <para>文档类型</para>
    /// <para>**可选值有**：</para>
    /// <para>doc:旧版文档,docx:新版文档,sheet:电子表格,bitable:多维表格,slides:幻灯片,file:文件</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`1` ～ `50` 字符</para>
    /// <para>必填：否</para>
    /// <para>可选值：<list type="bullet">
    /// <item>doc：旧版文档</item>
    /// <item>docx：新版文档</item>
    /// <item>sheet：电子表格</item>
    /// <item>bitable：多维表格</item>
    /// <item>slides：幻灯片</item>
    /// <item>file：文件</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("file_type")]
    public string? FileType { get; set; }

    /// <summary>
    /// <para>文档token</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`22` ～ `27` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("file_token")]
    public string? FileToken { get; set; }

    /// <summary>
    /// <para>用户 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("from_user_id")]
    public UserIdInfo? FromUserId { get; set; }

    /// <summary>
    /// <para>用户 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("to_user_id")]
    public UserIdInfo? ToUserId { get; set; }

    /// <summary>
    /// <para>评论操作类型，枚举值：add_comment、add_reply</para>
    /// <para>**可选值有**：</para>
    /// <para>add_comment:添加评论,add_reply:添加回复</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`2` ～ `50` 字符</para>
    /// <para>必填：否</para>
    /// <para>可选值：<list type="bullet">
    /// <item>add_comment：添加评论</item>
    /// <item>add_reply：添加回复</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("notice_type")]
    public string? NoticeType { get; set; }
}