// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Slides;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 飞书幻灯片（Slides）以 XML 协议（SML 2.0）描述演示文稿。本接口声明演示文稿级的创建与全文读取端点，页面级、截图级、历史版本端点见 <see cref="IFeishuV1SlidesSlide"/>、<see cref="IFeishuV1SlidesSlideImage"/>、<see cref="IFeishuV1SlidesHistory"/>。
/// <para><see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=create&amp;project=slides_ai&amp;resource=xml_presentation&amp;version=v1">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1SlidesPresentation : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 以 XML 格式创建演示文稿
    /// <para>创建一篇空白或带页面的演示文稿，响应返回演示文稿唯一标识（xml_presentation_id）与可访问链接。</para>
    /// <para>所需权限：slides:presentation:create（创建幻灯片）。请求体的 content 为完整 presentation XML，标题写在 &lt;title&gt; 元素内。</para>
    /// <para><see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=create&amp;project=slides_ai&amp;resource=xml_presentation&amp;version=v1">接口文档</see></para>
    /// </summary>
    /// <param name="request">创建请求体（xml_presentation.content 必填：演示文稿 XML 内容）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回演示文稿标识（xml_presentation_id）、页号列表、版本号、访问链接与 XML 语法问题（issues）</returns>
    [Post("/open-apis/slides_ai/v1/xml_presentations")]
    Task<FeishuApiResult<CreateXmlPresentationResult>?> CreateXmlPresentationAsync(
        [Body] CreateXmlPresentationRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 读取演示文稿全文
    /// <para>读取整篇演示文稿的 XML 内容，可指定版本号或要求剥离 id 属性。</para>
    /// <para>所需权限：slides:presentation:read（读取幻灯片）。</para>
    /// <para><see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=get&amp;project=slides_ai&amp;resource=xml_presentation&amp;version=v1">接口文档</see></para>
    /// </summary>
    /// <param name="xml_presentation_id">演示文稿唯一标识，示例值：zTqAwsEb4clrjOLd3drAcNZabcef</param>
    /// <param name="revision_id">演示文稿的版本号，范围 -1～2147483647，-1 表示最新版本</param>
    /// <param name="remove_attr_id">是否不获取 id 属性</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回演示文稿内容（presentation_id、revision_id、content）</returns>
    [Get("/open-apis/slides_ai/v1/xml_presentations/{xml_presentation_id}")]
    Task<FeishuApiResult<GetXmlPresentationResult>?> GetXmlPresentationAsync(
        [Path] string xml_presentation_id,
        [Query("revision_id")] int? revision_id = null,
        [Query("remove_attr_id")] bool? remove_attr_id = null,
        CancellationToken cancellationToken = default);
}
