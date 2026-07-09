// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.AI;

namespace Mud.Feishu;


/// <summary>
/// 飞书AI机器翻译接口，包括识别文本语种、翻译文本。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/ai/translation-v1/detect"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "AI")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1AITranslation : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 识别文本语种。
    /// <para>机器翻译 (MT)，支持 100 多种语言识别，返回符合 ISO 639-1 标准。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/ai/translation-v1/detect">接口文档</see></para>
    /// </summary>
    /// <param name="request">识别文本语种请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/translation/v1/text/detect")]
    Task<FeishuApiResult<DetectTextResult>?> DetectTextAsync(
      [Body] DetectTextRequest request,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 翻译文本。
    /// <para>机器翻译 (MT)，支持以下语种互译：</para>
    /// <para>"zh": 汉语；</para>
    /// <para>"zh-Hant": 繁体汉语；</para>
    /// <para>"en": 英语；</para>
    /// <para>"ja": 日语；</para>
    /// <para>"ru": 俄语；</para>
    /// <para>"de": 德语；</para>
    /// <para>"fr": 法语；</para>
    /// <para>"it": 意大利语；</para>
    /// <para>"pl": 波兰语；</para>
    /// <para>"th": 泰语；</para>
    /// <para>"hi": 印地语；</para>
    /// <para>"id": 印尼语；</para>
    /// <para>"es": 西班牙语；</para>
    /// <para>"pt": 葡萄牙语；</para>
    /// <para>"ko": 朝鲜语；</para>
    /// <para>"vi": 越南语；</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/ai/translation-v1/translate">接口文档</see></para>
    /// </summary>
    /// <param name="request">上传用于AI处理的文件请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/translation/v1/text/translate")]
    Task<FeishuApiResult<TranslateTextResult>?> TranslateTextAsync(
      [Body] TranslateTextRequest request,
      CancellationToken cancellationToken = default);
}
