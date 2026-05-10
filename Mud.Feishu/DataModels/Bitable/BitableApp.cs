// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// 多维表格应用
/// </summary>
public class BitableApp
{
    /// <summary>
    /// <para>多维表格 App 名称。最长为 255 个字符。</para>
    /// <para>必填：否</para>
    /// <para>示例值：一篇新的多维表格</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>多维表格 App 归属文件夹。默认为空，表示多维表格将被创建在云空间根目录。</para>
    /// <para>**注意**：</para>
    /// <para>请确保调用身份拥有在该文件夹中的编辑权限。若应用使用的是 `tenant_access_token` 权限，此处仅可指定应用创建的文件夹。</para>
    /// <para>必填：否</para>
    /// <para>示例值：fldcnqquW1svRIYVT2Np6Iabcef</para>
    /// </summary>
    [JsonPropertyName("folder_token")]
    public string? FolderToken { get; set; }

    /// <summary>
    /// <para>文档时区。</para>
    /// <para>必填：否</para>
    /// <para>示例值：Asia/Macau</para>
    /// </summary>
    [JsonPropertyName("time_zone")]
    public string? TimeZone { get; set; }
}