// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Mail;

/// <summary>
/// 创建邮箱文件夹请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class CreateUserMailboxFoldeRequest
{
    /// <summary>
    /// <para>文件夹名称</para>
    /// <para>必填：是</para>
    /// <para>示例值：newsletter 相关</para>
    /// <para>最大长度：250</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>父文件夹 id，该值为 0 表示根文件夹，id 获取方式见 [列出邮箱文件夹](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-folder/list)</para>
    /// <para>必填：是</para>
    /// <para>示例值：725627422334644</para>
    /// </summary>
    [JsonPropertyName("parent_folder_id")]
    public string ParentFolderId { get; set; } = string.Empty;
}
