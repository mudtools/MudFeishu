// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive.Files;

/// <summary>
/// <para>快捷方式的源文件信息</para>
/// </summary>
public class FileShortcutSrcInfo
{
    /// <summary>
    /// <para>快捷方式对应的源文件类型，可选值参照请求体的 `refer_type`</para>
    /// <para>必填：是</para>
    /// <para>示例值：docx</para>
    /// </summary>
    [JsonPropertyName("target_type")]
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// <para>快捷方式指向的源文件 token</para>
    /// <para>必填：是</para>
    /// <para>示例值：doxbcGvhSVN0R6octqPwAEabcef</para>
    /// </summary>
    [JsonPropertyName("target_token")]
    public string TargetToken { get; set; } = string.Empty;
}
