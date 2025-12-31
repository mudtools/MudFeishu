// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels;

/// <summary>
/// <para>国际化文案</para>
/// </summary>
public class I18nResource
{
    /// <summary>
    /// <para>语言。</para>
    /// <para>必填：是</para>
    /// <para>示例值：zh-CN</para>
    /// <para>可选值：<list type="bullet">
    /// <item>zh-CN：中文</item>
    /// <item>en-US：英文</item>
    /// <item>ja-JP：日文</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("locale")]
    public string Locale { get; set; } = string.Empty;

    /// <summary>
    /// <para>文案的 key、value。</para>
    /// <para>必填：是</para>
    /// <para>示例值：{ "@i18n@1": "权限申请", "@i18n@2": "OA审批", "@i18n@3": "Permission" }</para>
    /// </summary>
    [JsonPropertyName("texts")]
    public I18nResourceText[] Texts { get; set; } = [];


    /// <summary>
    /// <para>是否为默认语言。默认语言需要包含所有 key，非默认语言如果 key 不存在会使用默认语言代替。</para>
    /// <para>必填：是</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("is_default")]
    public bool IsDefault { get; set; }
}