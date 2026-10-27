// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Slides;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 飞书幻灯片页面截图接口，用于按页面获取整页截图，或在不落文档的前提下把一段 slide XML 渲染为图片。
/// <para><see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=list&amp;project=slides_ai&amp;resource=xml_presentation.slide_image&amp;version=v1">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1SlidesSlideImage : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取幻灯片页面截图
    /// <para>按 slide_ids 或 slide_numbers 批量获取页面截图，响应中的 data 为图片 Base64 文本。</para>
    /// <para>所需权限：slides:presentation:screenshot（获取幻灯片截图）。Base64 体积可达兆级，消费方不得把原文直接写入模型上下文。</para>
    /// <para><see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=list&amp;project=slides_ai&amp;resource=xml_presentation.slide_image&amp;version=v1">接口文档</see></para>
    /// </summary>
    /// <param name="xml_presentation_id">演示文稿唯一标识，示例值：zTqAwsEb4clrjOLd3drAcNZabcef</param>
    /// <param name="request">截图请求体（slide_ids 页面 short ID 列表；slide_numbers 页号列表；两者至少提供一个）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回页面截图列表（slide_id、format、data、slide_number）、总数量与版本标识</returns>
    [Post("/open-apis/slides_ai/v1/xml_presentations/{xml_presentation_id}/slide_images")]
    Task<FeishuApiResult<ListSlideImagesResult>?> ListSlideImagesAsync(
        [Path] string xml_presentation_id,
        [Body] ListSlideImagesRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 渲染页面截图为图片
    /// <para>把传入的 slide XML 直接渲染为图片，不依赖已存在的演示文稿。</para>
    /// <para>所需权限：slides:presentation:screenshot（获取幻灯片截图）。响应中的 data 为图片 Base64 文本，体积可达兆级，消费方不得把原文直接写入模型上下文。</para>
    /// <para><see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=render&amp;project=slides_ai&amp;resource=xml_presentation.slide_image&amp;version=v1">接口文档</see></para>
    /// </summary>
    /// <param name="request">渲染请求体（content 必填：单个完整 &lt;slide&gt; XML）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回渲染出的页面截图（slide_id、format、data、slide_number）</returns>
    [Post("/open-apis/slides_ai/v1/slide_image/render")]
    Task<FeishuApiResult<RenderSlideImageResult>?> RenderSlideImageAsync(
        [Body] RenderSlideImageRequest request,
        CancellationToken cancellationToken = default);
}
