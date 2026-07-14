// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive.Files;

/// <summary>
/// <para>复制的新文件信息</para>
/// </summary>
public class NewCopyFileInfo
{
    /// <summary>
    /// <para>复制的新文件 token</para>
    /// <para>必填：是</para>
    /// <para>示例值：doxcnUkUOWtOelpFcha2Zabcef</para>
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// <para>新文件的名称</para>
    /// <para>必填：是</para>
    /// <para>示例值：Demo copy</para>
    /// <para>最大长度：250</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>新文件的类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：docx</para>
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// <para>新文件的父文件夹 token</para>
    /// <para>必填：否</para>
    /// <para>示例值：fldbcO1UuPz8VwnpPx5a92abcef</para>
    /// </summary>
    [JsonPropertyName("parent_token")]
    public string? ParentToken { get; set; }

    /// <summary>
    /// <para>文件在浏览器中的 URL 链接</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://feishu.cn/docx/doxcnUkUOWtOelpFcha2Zabcef</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// <para>快捷方式文件信息（该参数不会返回）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("shortcut_info")]
    public CopyFileShortcutInfo? ShortcutInfo { get; set; }

    /// <summary>
    /// <para>文件创建时间（该参数不会返回）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1686125119</para>
    /// </summary>
    [JsonPropertyName("created_time")]
    public string? CreatedTime { get; set; }

    /// <summary>
    /// <para>文件最近修改时间（该参数不会返回）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1686125119</para>
    /// </summary>
    [JsonPropertyName("modified_time")]
    public string? ModifiedTime { get; set; }

    /// <summary>
    /// <para>文件所有者（该参数不会返回）</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_b13d41c02edc52ce66aaae67bf1abcef</para>
    /// </summary>
    [JsonPropertyName("owner_id")]
    public string? OwnerId { get; set; }
}