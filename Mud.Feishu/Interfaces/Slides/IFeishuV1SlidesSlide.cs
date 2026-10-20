// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Slides;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 飞书幻灯片页面（slide）接口，用于在指定演示文稿下创建、读取、删除页面，以及对单个页面做元素级局部替换。
/// <para><see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=create&amp;project=slides_ai&amp;resource=xml_presentation.slide&amp;version=v1">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1SlidesSlide : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 创建幻灯片页面
    /// <para>在指定演示文稿下追加或插入一个页面，每次调用只创建一个页面。</para>
    /// <para>所需权限：slides:presentation:update 或 slides:presentation:write_only（至少其一）。</para>
    /// <para><see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=create&amp;project=slides_ai&amp;resource=xml_presentation.slide&amp;version=v1">接口文档</see></para>
    /// </summary>
    /// <param name="xml_presentation_id">演示文稿唯一标识，示例值：zTqAwsEb4clrjOLd3drAcNZabcef</param>
    /// <param name="request">创建请求体（slide.content 必填：单个完整 &lt;slide&gt; XML；before_slide_id 选填，不传时追加到末尾）</param>
    /// <param name="revision_id">演示文稿的版本号，范围 -1～2147483647，-1 表示最新版本</param>
    /// <param name="idempotency_key">幂等标记，重复提交同一标记不会重复建页</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回新页面标识（slide_id）、最新版本号与 XML 语法问题（issues）</returns>
    [Post("/open-apis/slides_ai/v1/xml_presentations/{xml_presentation_id}/slide")]
    Task<FeishuApiResult<CreateSlideResult>?> CreateSlideAsync(
        [Path] string xml_presentation_id,
        [Body] CreateSlideRequest request,
        [Query("revision_id")] int? revision_id = null,
        [Query("idempotency_key")] string? idempotency_key = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取单个幻灯片页面
    /// <para>按页面标识或页号获取单个页面的 XML 内容，二者至少提供一个。</para>
    /// <para>所需权限：slides:presentation:read（读取幻灯片）。</para>
    /// <para><see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=get&amp;project=slides_ai&amp;resource=xml_presentation.slide&amp;version=v1">接口文档</see></para>
    /// </summary>
    /// <param name="xml_presentation_id">演示文稿唯一标识，示例值：zTqAwsEb4clrjOLd3drAcNZabcef</param>
    /// <param name="slide_id">页面唯一标识，示例值：slidecn20m4XJ1hJXXxXXX</param>
    /// <param name="revision_id">演示文稿的版本号，范围 -1～2147483647，-1 表示最新版本</param>
    /// <param name="slide_number">幻灯片页号，范围 0～2147483647</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回页面内容（slide_id、content）与演示文稿版本号</returns>
    [Get("/open-apis/slides_ai/v1/xml_presentations/{xml_presentation_id}/slide")]
    Task<FeishuApiResult<GetSlideResult>?> GetSlideAsync(
        [Path] string xml_presentation_id,
        [Query("slide_id")] string? slide_id = null,
        [Query("revision_id")] int? revision_id = null,
        [Query("slide_number")] int? slide_number = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除幻灯片页面
    /// <para>删除指定演示文稿下的一个页面。</para>
    /// <para>所需权限：slides:presentation:update 或 slides:presentation:write_only（至少其一）。删除不可逆，回滚需走历史版本接口。</para>
    /// <para><see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=delete&amp;project=slides_ai&amp;resource=xml_presentation.slide&amp;version=v1">接口文档</see></para>
    /// </summary>
    /// <param name="xml_presentation_id">演示文稿唯一标识，示例值：zTqAwsEb4clrjOLd3drAcNZabcef</param>
    /// <param name="slide_id">待删除页面唯一标识，示例值：slidecn20m4XJ1hJXXxXXX</param>
    /// <param name="revision_id">演示文稿的版本号，范围 -1～2147483647，-1 表示最新版本</param>
    /// <param name="idempotency_key">幂等标记，重复提交同一标记不会重复删除</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回删除后的最新版本号</returns>
    [Delete("/open-apis/slides_ai/v1/xml_presentations/{xml_presentation_id}/slide")]
    Task<FeishuApiResult<DeleteSlideResult>?> DeleteSlideAsync(
        [Path] string xml_presentation_id,
        [Query("slide_id")] string slide_id,
        [Query("revision_id")] int? revision_id = null,
        [Query("idempotency_key")] string? idempotency_key = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 元素级替换页面内容
    /// <para>对指定页面做块级局部替换（block_replace）或块插入（block_insert），一次调用最多 200 条操作。</para>
    /// <para>所需权限：slides:presentation:update 或 slides:presentation:write_only（至少其一）。</para>
    /// <para>整请求失败时返回 failed_part_index 与 failed_reason，用于定位失败的那条操作。</para>
    /// <para><see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=replace&amp;project=slides_ai&amp;resource=xml_presentation.slide&amp;version=v1">接口文档</see></para>
    /// </summary>
    /// <param name="xml_presentation_id">演示文稿唯一标识，示例值：zTqAwsEb4clrjOLd3drAcNZabcef</param>
    /// <param name="slide_id">目标页面唯一标识，示例值：slidecn20m4XJ1hJXXxXXX</param>
    /// <param name="request">替换请求体（parts 必填，1～200 条；comment 操作备注；idempotency_key 幂等标记）</param>
    /// <param name="revision_id">演示文稿的版本号，范围 -1～2147483647，-1 表示最新版本</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回最新版本号，失败时附 failed_part_index 与 failed_reason</returns>
    [Post("/open-apis/slides_ai/v1/xml_presentations/{xml_presentation_id}/slide/replace")]
    Task<FeishuApiResult<ReplaceSlideResult>?> ReplaceSlideAsync(
        [Path] string xml_presentation_id,
        [Query("slide_id")] string slide_id,
        [Body] ReplaceSlideRequest request,
        [Query("revision_id")] int? revision_id = null,
        CancellationToken cancellationToken = default);
}
