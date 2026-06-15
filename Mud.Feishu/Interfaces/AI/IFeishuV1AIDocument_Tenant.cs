// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.AI;

namespace Mud.Feishu;


/// <summary>
/// 飞书AI文档接口包括智能文档处理（支持17种证件识别），如：简历信息解析、机动车发票识别、健康证识别、中国护照识别等能力。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/ai/document_ai-v1/resume/parse"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "AI", InheritedFrom = nameof(FeishuV1AIDocument))]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuTenantV1AIDocument : IFeishuV1AIDocument
{

    /// <summary>
    /// 识别文件中的简历信息。
    /// <para>简历信息解析接口，支持PDF/DOCX/PNG/JPG四种文件类型的一次性的识别。文件大小需要小于30M。</para>
    /// <para><see href="https://open.feishu.cn/document/ai/document_ai-v1/resume/parse">接口文档</see></para>
    /// </summary>
    /// <param name="request">上传用于AI处理的文件请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/document_ai/v1/resume/parse")]
    Task<FeishuApiResult<ParseResumeResult>?> ParseResumeAsync(
      [FormContent] FileUploadRequest request,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 识别文件中的港澳居民来往内地通行证信息。
    /// <para>港澳居民来往内地通行证识别接口，支持PDF/DOCX/PNG/JPG四种文件类型的一次性的识别。文件大小需要小于30M。</para>
    /// <para><see href="https://open.feishu.cn/document/ai/document_ai-v1/hkm_mainland_travel_permit/recognize">接口文档</see></para>
    /// </summary>
    /// <param name="request">上传用于AI处理的文件请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/document_ai/v1/hkm_mainland_travel_permit/recognize")]
    Task<FeishuApiResult<RecognizeHkmMainlandTravelPermitResult>?> RecognizeHkmMainlandTravelPermitAsync(
      [FormContent] FileUploadRequest request,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 识别文件中的台湾居民来往大陆通行证信息。
    /// <para>台湾居民来往大陆通行证识别接口，支持PDF/DOCX/PNG/JPG四种文件类型的一次性的识别。文件大小需要小于30M。</para>
    /// <para><see href="https://open.feishu.cn/document/ai/document_ai-v1/tw_mainland_travel_permit/recognize">接口文档</see></para>
    /// </summary>
    /// <param name="request">上传用于AI处理的文件请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/document_ai/v1/tw_mainland_travel_permit/recognize")]
    Task<FeishuApiResult<RecognizeTwMainlandTravelPermitResult>?> RecognizeTwMainlandTravelPermitAsync(
      [FormContent] FileUploadRequest request,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 识别文件中的中国护照信息。
    /// <para>中国护照识别接口，支持PDF/DOCX/PNG/JPG四种文件类型的一次性的识别。文件大小需要小于30M。</para>
    /// <para><see href="https://open.feishu.cn/document/ai/document_ai-v1/chinese_passport/recognize">接口文档</see></para>
    /// </summary>
    /// <param name="request">上传用于AI处理的文件请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/document_ai/v1/chinese_passport/recognize")]
    Task<FeishuApiResult<RecognizeChinesePassportResult>?> RecognizeChinesePassportAsync(
      [FormContent] FileUploadRequest request,
      CancellationToken cancellationToken = default);
}
