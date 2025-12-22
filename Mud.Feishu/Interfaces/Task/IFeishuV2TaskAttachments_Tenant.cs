// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Interfaces.Task;

/// <summary>
/// 任务可以拥有附件。一个附件可以是任意类型的文件，如图片，PDF文档，zip文件等。
/// <para>附件不可以单独存在，必须与某种资源产生关联关系。</para>
/// <para>关联附件的资源类型只有任务。因为附件不可单独存在，因此为新任务添加附件时，必须先调用创建任务接口，完成任务创建，再调用上传附件接口上传文件，并关联到新建的任务上。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/task-v2/attachment/attachment-feature-overview"/></para>
/// </summary> 
[HttpClientApi(TokenManage = nameof(ITenantTokenManager), RegistryGroupName = "Task", InheritedFrom = nameof(FeishuV2TaskAttachments))]
[Header("Authorization")]
public interface IFeishuTenantV2TaskAttachments : IFeishuV2TaskAttachments
{
}
