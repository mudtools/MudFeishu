// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive.Folder;

/// <summary>
/// 获取文件夹元数据响应体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Drive")]
public class GetFolderMetaResult
{
    /// <summary>
    /// <para>文件夹的 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>文件夹的标题</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>文件夹的 token</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    /// <summary>
    /// <para>文件夹的创建者 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("createUid")]
    public string? CreateUid { get; set; }

    /// <summary>
    /// <para>文件夹的最后编辑者 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("editUid")]
    public string? EditUid { get; set; }

    /// <summary>
    /// <para>文件夹的上级目录 ID。“0” 表示当前文件夹无上级目录</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("parentId")]
    public string? ParentId { get; set; }

    /// <summary>
    /// <para>文件夹为个人文件夹时，为文件夹的所有者 ID；文件夹为共享文件夹时，为文件夹树 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("ownUid")]
    public string? OwnUid { get; set; }
}
