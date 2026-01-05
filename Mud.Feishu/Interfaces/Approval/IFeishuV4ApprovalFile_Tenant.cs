// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.ApprovalFile;

namespace Mud.Feishu;

/// <summary>
/// 原生审批文件提供的 API 主要用于上传审批表单控件内的文件。
/// <para>当审批表单中有图片或者附件控件时，开发者需要在调用创建审批实例前，将传入图片或附件控件的文件上传到审批系统，系统会返回文件的 code，该 code 用于创建审批实例时为图片或附件控件赋值。</para>
/// 接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/approval-v4/file/overview"/>
/// </summary>
[HttpClientApi(TokenManage = nameof(ITenantTokenManager), RegistryGroupName = "Approval")]
[Header(Consts.Authorization)]
public interface IFeishuTenantV2ApprovalFile
{
    /// <summary>
    /// 当审批表单中有图片或者附件控件时，开发者需要在调用创建审批实例前，
    /// <para>将传入图片或附件控件的文件通过本接口上传到审批系统，接口会返回文件的 code，该 code 用于创建审批实例时为图片或附件控件赋值。</para>
    /// </summary>
    /// <param name="uploadFileRequest">文件上传请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/approval/openapi/v2/file/upload")]
    [IgnoreImplement]
    Task<FeishuApiResult<FileUploadResult>?> UploadFileAsync(
      UploadFileRequest uploadFileRequest,
      CancellationToken cancellationToken = default);
}
