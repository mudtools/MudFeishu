// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu;


/// <summary>
/// 飞书招聘（Hire）附件入口域 SDK 是一组服务端 OpenAPI 的封装，用于招聘系统附件文件的上传（创建通用附件）、附件元信息（文件名、创建时间、下载地址）查询，以及人才简历附件的 PDF 格式下载链接获取。本接口全部端点仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/hire-v1/attachment/create"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Hire")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1HireAttachment : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 创建附件
    /// <para>在招聘系统中上传附件文件，上传的附件为通用附件；文件大小不得超过 300 MB。请求体为 multipart/form-data，仅含一个文件字段 content。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:attachment（上传招聘相关附件）。支持的应用类型：自建应用、商店应用。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/attachment/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">上传请求体（content 为待上传文件的本地绝对路径，≤ 300 MB）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回上传结果（id 附件文件 ID、name 文件名、url 文件 URL，有效期 30 分钟）</returns>
    [Post("/open-apis/hire/v1/attachments")]
    Task<FeishuApiResult<CreateAttachmentResult>?> CreateAttachmentAsync(
        [FormContent] CreateAttachmentRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取附件信息
    /// <para>根据附件 ID 和附件类型获取招聘系统中附件的元信息，比如附件名称、附件创建时间、附件下载地址等。附件 ID 可通过「获取人才信息 V1」（简历/作品附件）或「创建附件」「获取 Offer 详情」「获取 Offer 信息」（通用附件）接口获取。</para>
    /// <para>限频：20 次/秒。所需权限：hire:attachment:readonly（获取附件信息）或 hire:attachment（上传招聘相关附件）。支持的应用类型：自建应用、商店应用。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/attachment/get">接口文档</see></para>
    /// </summary>
    /// <param name="attachment_id">附件 ID，示例值：6960663240925956555</param>
    /// <param name="type">附件类型（1 简历附件 / 2 作品附件 / 3 通用附件），默认 1</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回附件信息（attachment：id、url、name、mime、create_time）</returns>
    [Get("/open-apis/hire/v1/attachments/{attachment_id}")]
    Task<FeishuApiResult<GetAttachmentResult>?> GetAttachmentAsync(
        [Path] string attachment_id,
        [Query("type")] int? type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取人才简历附件 PDF 格式下载链接
    /// <para>根据人才简历附件 ID 获取该简历附件对应的 PDF 文件的下载地址。仅支持转换人才简历类型附件，不支持作品附件和通用附件；.doc/.docx/.ppt/.pptx/.txt 可转换为 PDF，转换失败时返回附件原文件下载地址。</para>
    /// <para>限频：20 次/秒。所需权限：hire:attachment（上传招聘相关附件）。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/attachment/preview">接口文档</see></para>
    /// </summary>
    /// <param name="attachment_id">人才简历附件 ID，可通过「获取人才信息」接口返回数据获取，示例值：64352523512563462</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回 PDF 文件下载链接（url，有效期 30 分钟）</returns>
    [Get("/open-apis/hire/v1/attachments/{attachment_id}/preview")]
    Task<FeishuApiResult<PreviewAttachmentResult>?> PreviewAttachmentAsync(
        [Path] string attachment_id,
        CancellationToken cancellationToken = default);
}
