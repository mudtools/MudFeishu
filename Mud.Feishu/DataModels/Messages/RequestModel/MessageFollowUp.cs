// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Messages;

/// <summary>
/// 跟随气泡
/// </summary>
public class MessageFollowUp
{
    /// <summary>
    /// 气泡的内容。示例值："你好"
    /// </summary>
    [JsonPropertyName("content")]
    public
#if NET7_0_OR_GREATER
        required
#endif
        string? Content
    { get; set; }

    /// <summary>
    /// 气泡的多语言内容。
    /// </summary>
    [JsonPropertyName("i18n_contents")]
    public List<I18nContentLanguage>? I18nContents { get; set; }
}
