// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.CardMessageStream;


/// <summary>
/// <para>跳转 URL（仅支持 https 协议）</para>
/// </summary>
public class OpenAppMessageCardUrl
{
    /// <summary>
    /// <para>默认 URL</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://www.feishu.cn/</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// <para>Android 平台 URL</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://www.feishu.cn/</para>
    /// </summary>
    [JsonPropertyName("android_url")]
    public string? AndroidUrl { get; set; }

    /// <summary>
    /// <para>iOS 平台 URL</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://www.feishu.cn/</para>
    /// </summary>
    [JsonPropertyName("ios_url")]
    public string? IosUrl { get; set; }

    /// <summary>
    /// <para>PC URL</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://www.feishu.cn/</para>
    /// </summary>
    [JsonPropertyName("pc_url")]
    public string? PcUrl { get; set; }
}