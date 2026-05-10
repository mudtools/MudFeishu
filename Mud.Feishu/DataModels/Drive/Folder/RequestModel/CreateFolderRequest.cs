// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive.Folder;

/// <summary>
/// 新建文件夹请求体
/// </summary>
public class CreateFolderRequest
{
    /// <summary>
    /// <para>文件夹名称</para>
    /// <para>**长度限制**： 1~256 个字节</para>
    /// <para>必填：是</para>
    /// <para>示例值：产品优化项目</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>父文件夹的 token。参数为空字符串时，表示在根目录下创建文件夹。你可参考[获取文件夹中的文件清单](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/drive-v1/file/list)获取某个文件夹的 token。了解更多，参考[文件夹概述](https://open.feishu.cn/document/ukTMukTMukTM/ugTNzUjL4UzM14CO1MTN/folder-overview)。</para>
    /// <para>必填：是</para>
    /// <para>示例值：fldbcO1UuPz8VwnpPx5a92abcef</para>
    /// </summary>
    [JsonPropertyName("folder_token")]
    public string FolderToken { get; set; } = string.Empty;
}
