// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.AI;

namespace Mud.Feishu;


/// <summary>
/// 飞书AI光学字符识别接口，包括识别图片中的文字，按图片中的区域划分，分段返回文本列表。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/ai/document_ai-v1/resume/parse"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "AI")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1AIOpticalCharRecognition : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 识别图片中的文字。
    /// <para>可识别图片中的文字，按图片中的区域划分，分段返回文本列表。文件大小需小于5M。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/ai/optical_char_recognition-v1/basic_recognize">接口文档</see></para>
    /// </summary>
    /// <param name="request">上传用于AI处理的文件请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/optical_char_recognition/v1/image/basic_recognize")]
    Task<FeishuApiResult<BasicRecognizeImageResult>?> BasicRecognizeImageAsync(
      [Body] BasicRecognizeImageRequest request,
      CancellationToken cancellationToken = default);
}
