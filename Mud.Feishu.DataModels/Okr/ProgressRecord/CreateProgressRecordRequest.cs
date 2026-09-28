// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Okr;

/// <summary>
/// 创建 OKR 进展记录请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Okr")]
public class CreateProgressRecordRequest
{
    /// <summary>
    /// <para>进展来源（必填）</para>
    /// <para>必填：是</para>
    /// <para>示例值：2021.12.20~2021.12.26 周报</para>
    /// </summary>
    [JsonPropertyName("source_title")]
    public string SourceTitle { get; set; } = string.Empty;

    /// <summary>
    /// <para>进展来源链接（必填），需匹配正则 ^https?://.*$</para>
    /// <para>必填：是</para>
    /// <para>示例值：www.baidu.com</para>
    /// </summary>
    [JsonPropertyName("source_url")]
    public string SourceUrl { get; set; } = string.Empty;

    /// <summary>
    /// <para>目标 id（必填），与 target_type 对应，可通过批量获取 OKR / 获取用户的 OKR 列表接口获取</para>
    /// <para>必填：是</para>
    /// <para>示例值：7041430377642082323</para>
    /// </summary>
    [JsonPropertyName("target_id")]
    public string TargetId { get; set; } = string.Empty;

    /// <summary>
    /// <para>目标类型（必填）：2 Objective、3 Key Result</para>
    /// <para>必填：是</para>
    /// <para>示例值：2</para>
    /// </summary>
    [JsonPropertyName("target_type")]
    public int TargetType { get; set; }

    /// <summary>
    /// <para>进展详情（必填），富文本格式</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("content")]
    public ContentBlock Content { get; set; } = new ContentBlock();

    /// <summary>
    /// <para>PC 端进展来源链接，需匹配正则 ^https?://.*$</para>
    /// <para>必填：否</para>
    /// <para>示例值：open.feishu.cn</para>
    /// </summary>
    [JsonPropertyName("source_url_pc")]
    public string? SourceUrlPc { get; set; }

    /// <summary>
    /// <para>移动端进展来源链接，需匹配正则 ^https?://.*$</para>
    /// <para>必填：否</para>
    /// <para>示例值：open.feishu.cn</para>
    /// </summary>
    [JsonPropertyName("source_url_mobile")]
    public string? SourceUrlMobile { get; set; }
}
