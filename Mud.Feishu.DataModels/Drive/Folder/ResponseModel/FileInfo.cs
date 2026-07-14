// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive.Folder;

/// <summary>
/// <para>文件夹中的文件清单列表</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Drive")]
public class FileInfo
{
    /// <summary>
    /// <para>文件标识</para>
    /// <para>必填：是</para>
    /// <para>示例值：fldbcO1UuPz8VwnpPx5a9abcef</para>
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// <para>文件名</para>
    /// <para>必填：是</para>
    /// <para>示例值：title</para>
    /// <para>最大长度：250</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>文件类型。可选值有：</para>
    /// <para>- `doc`：旧版文档</para>
    /// <para>- `sheet`：表格</para>
    /// <para>- `mindnote`：思维导图</para>
    /// <para>- `bitable`：多维表格</para>
    /// <para>- `file`：文件</para>
    /// <para>- `docx`：新版文档</para>
    /// <para>- `folder`：文件夹</para>
    /// <para>- `shortcut`: 快捷方式</para>
    /// <para>必填：是</para>
    /// <para>示例值：docx</para>
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// <para>父文件夹标识</para>
    /// <para>必填：否</para>
    /// <para>示例值：fldbcO1UuPz8VwnpPx5a9abcef</para>
    /// </summary>
    [JsonPropertyName("parent_token")]
    public string? ParentToken { get; set; }

    /// <summary>
    /// <para>文件在浏览器中的 URL 链接</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://feishu.cn/folder/fldbcO1UuPz8VwnpPx5a9abcef</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// <para>快捷方式类型文件的信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("shortcut_info")]
    public FileShortcutInfo? ShortcutInfo { get; set; }

    /// <summary>
    /// <para>文件创建时间，秒级时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1686125119</para>
    /// </summary>
    [JsonPropertyName("created_time")]
    public string? CreatedTime { get; set; }

    /// <summary>
    /// <para>文件最近修改时间，秒级时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1686125119</para>
    /// </summary>
    [JsonPropertyName("modified_time")]
    public string? ModifiedTime { get; set; }

    /// <summary>
    /// <para>文件所有者的 ID。ID 类型由查询参数中的 `user_id_type` 决定</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_b13d41c02edc52ce66aaae67bf1abcef</para>
    /// </summary>
    [JsonPropertyName("owner_id")]
    public string? OwnerId { get; set; }
}
