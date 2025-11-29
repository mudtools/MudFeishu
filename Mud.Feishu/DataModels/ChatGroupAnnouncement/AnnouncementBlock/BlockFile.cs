// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroupNotice;


/// <summary>
/// <para>文件 Block</para>
/// </summary>
public class BlockFile
{
    /// <summary>
    /// <para>附件 Token</para>
    /// <para>必填：否</para>
    /// <para>示例值：boxbcOj88GDkmWGm2zsTyCBqoLb</para>
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    /// <summary>
    /// <para>文件名</para>
    /// <para>必填：否</para>
    /// <para>示例值：文件名</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>视图类型，卡片视图（默认）或预览视图</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：卡片视图</item>
    /// <item>2：预览视图</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("view_type")]
    public int? ViewType { get; set; }
}