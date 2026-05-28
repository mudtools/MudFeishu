// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Mail;

/// <summary>
/// 邮箱文件信息业务操作响应体
/// </summary>
public class UserMailboxFoldeOopsResult
{
    /// <summary>
    /// <para>邮件文件夹的完整信息，包含文件夹ID、名称、归属层级、类型及未读统计数据。</para>
    /// <para>必填：否</para>
    /// <para>示例值：{"id":"fld_1234567890abcdef","name":"收件箱","parent_folder_id":"0","folder_type":1,"unread_message_count":12,"unread_thread_count":8}</para>
    /// </summary>
    [JsonPropertyName("folder")]
    public MailFolder? Folder { get; set; }


}
