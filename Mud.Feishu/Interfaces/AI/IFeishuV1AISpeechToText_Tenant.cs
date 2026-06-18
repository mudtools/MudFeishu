// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.AI;

namespace Mud.Feishu;


/// <summary>
/// 飞书AI语音转文字接口，包括将音频文件转换为文字内容。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/ai/document_ai-v1/resume/parse"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "AI")]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuTenantV1AISpeechToText : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 识别语音文件。
    /// <para>语音文件识别接口，上传整段语音文件进行一次性识别。接口适合 60 秒以内音频识别。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/ai/speech_to_text-v1/file_recognize">接口文档</see></para>
    /// </summary>
    /// <param name="request">上传用于AI处理的文件请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/speech_to_text/v1/speech/file_recognize")]
    Task<FeishuApiResult<FileRecognizeSpeechResult>?> FileRecognizeSpeechAsync(
      [Body] FileRecognizeSpeechRequest request,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 识别流式语音。
    /// <para>语音流式接口，将整个音频文件分片进行传入模型。能够实时返回数据。建议每个音频分片的大小为 100-200ms。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/ai/speech_to_text-v1/stream_recognize">接口文档</see></para>
    /// </summary>
    /// <param name="request">上传用于AI处理的文件请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/speech_to_text/v1/speech/stream_recognize")]
    Task<FeishuApiResult<StreamRecognizeSpeechResult>?> StreamRecognizeSpeechAsync(
      [Body] StreamRecognizeSpeechRequest request,
      CancellationToken cancellationToken = default);
}
