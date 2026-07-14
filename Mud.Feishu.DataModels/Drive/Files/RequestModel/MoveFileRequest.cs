// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive.Files;

/// <summary>
/// 移动文件或文件夹请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Drive")]
public class MoveFileRequest
{
    /// <summary>
    /// <para>文件类型。该参数为必填，请忽略左侧必填列的“否”。如果该值为空或者与文件实际类型不匹配，接口会返回失败。</para>
    /// <para>必填：否</para>
    /// <para>示例值：file</para>
    /// <para>可选值：<list type="bullet">
    /// <item>file：普通文件类型</item>
    /// <item>docx：新版文档类型</item>
    /// <item>bitable：多维表格类型</item>
    /// <item>doc：文档类型</item>
    /// <item>sheet：电子表格类型</item>
    /// <item>mindnote：思维笔记类型</item>
    /// <item>folder：文件夹类型</item>
    /// <item>slides：幻灯片类型</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>目标文件夹的 token。</para>
    /// <para>必填：否</para>
    /// <para>示例值：fldbcO1UuPz8VwnpPx5a92abcef</para>
    /// </summary>
    [JsonPropertyName("folder_token")]
    public string? FolderToken { get; set; }
}
