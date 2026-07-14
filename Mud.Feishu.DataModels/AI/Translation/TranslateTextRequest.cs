// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AI;

/// <summary>
/// 翻译文本请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "AI")]
public class TranslateTextRequest
{
    /// <summary>
    /// <para>源语言</para>
    /// <para>必填：是</para>
    /// <para>示例值：zh</para>
    /// </summary>
    [JsonPropertyName("source_language")]
    public string SourceLanguage { get; set; } = string.Empty;

    /// <summary>
    /// <para>源文本，字符上限为 1,000。</para>
    /// <para>必填：是</para>
    /// <para>示例值：尝试使用一下飞书吧</para>
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// <para>目标语言</para>
    /// <para>必填：是</para>
    /// <para>示例值：en</para>
    /// </summary>
    [JsonPropertyName("target_language")]
    public string TargetLanguage { get; set; } = string.Empty;

    /// <summary>
    /// <para>请求级术语表，携带术语，仅在本次翻译中生效（最多能携带 128个术语词）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("glossary")]
    public TranslateTerm[]? Glossaies { get; set; }

}
