// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels;

/// <summary>
/// 国际化文案
/// </summary>
public class I18nResourceText
{
    /// <summary>
    /// <para>文案 key。key 以 `@i18n@` 开头，该字段主要用于做国际化，允许用户同时传多个语言的文案，审批中心会根据用户当前的语音环境使用对应的文案，如果没有传用户当前的语音环境文案，则会使用默认的语言文案。</para>
    /// <para>必填：是</para>
    /// <para>示例值：@i18n@1</para>
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// <para>文案内容</para>
    /// <para>必填：是</para>
    /// <para>示例值：people</para>
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}
