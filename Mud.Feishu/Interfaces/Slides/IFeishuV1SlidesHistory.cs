// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Slides;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 飞书幻灯片历史版本接口，用于列出演示文稿历史版本、按历史版本发起回滚任务并查询任务状态。
/// <para><see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=list&amp;project=slides_ai&amp;resource=xml_presentation.history&amp;version=v1">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1SlidesHistory : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 列出演示文稿历史版本
    /// <para>分页列出演示文稿的历史版本条目，history_version_id 是回滚入参。</para>
    /// <para>所需权限：slides:presentation:read（读取幻灯片）。分页范围 1～20，默认 20。</para>
    /// <para><see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=list&amp;project=slides_ai&amp;resource=xml_presentation.history&amp;version=v1">接口文档</see></para>
    /// </summary>
    /// <param name="xml_presentation_id">演示文稿唯一标识，示例值：zTqAwsEb4clrjOLd3drAcNZabcef</param>
    /// <param name="page_size">分页数量，范围 1～20，默认 20</param>
    /// <param name="page_token">分页游标，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回历史版本条目（entries）、是否还有更多（has_more）与下一页游标</returns>
    [Get("/open-apis/slides_ai/v1/xml_presentations/{xml_presentation_id}/histories")]
    Task<FeishuApiResult<ListPresentationHistoriesResult>?> ListPresentationHistoriesAsync(
        [Path] string xml_presentation_id,
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 回滚演示文稿历史版本
    /// <para>按 history_version_id 发起异步回滚任务，响应成功仅表示任务已提交，需再查询任务状态确认结果。</para>
    /// <para>所需权限：slides:presentation:update 或 slides:presentation:write_only（至少其一）。回滚会覆盖当前内容，失败块由 failed_block_tokens 反映。</para>
    /// <para><see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=revert&amp;project=slides_ai&amp;resource=xml_presentation.history&amp;version=v1">接口文档</see></para>
    /// </summary>
    /// <param name="xml_presentation_id">演示文稿唯一标识，示例值：zTqAwsEb4clrjOLd3drAcNZabcef</param>
    /// <param name="request">回滚请求体（history_version_id 必填：由历史版本列表接口返回）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回回滚任务标识（task_id）、当前状态与建议轮询间隔（poll_after_ms）</returns>
    [Post("/open-apis/slides_ai/v1/xml_presentations/{xml_presentation_id}/history/revert")]
    Task<FeishuApiResult<RevertPresentationHistoryResult>?> RevertPresentationHistoryAsync(
        [Path] string xml_presentation_id,
        [Body] RevertPresentationHistoryRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询历史回滚任务状态
    /// <para>按 task_id 查询回滚任务执行状态，用于确认回滚是否完成或部分失败。</para>
    /// <para>所需权限：slides:presentation:read（读取幻灯片）。</para>
    /// <para><see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=revert_status&amp;project=slides_ai&amp;resource=xml_presentation.history&amp;version=v1">接口文档</see></para>
    /// </summary>
    /// <param name="xml_presentation_id">演示文稿唯一标识，示例值：zTqAwsEb4clrjOLd3drAcNZabcef</param>
    /// <param name="task_id">回滚任务 ID，取自回滚接口返回的 task_id，示例值：task_xxx</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回任务状态（status）、失败块列表（failed_block_tokens）与回滚目标版本 id</returns>
    [Get("/open-apis/slides_ai/v1/xml_presentations/{xml_presentation_id}/history/revert_status")]
    Task<FeishuApiResult<GetHistoryRevertStatusResult>?> GetHistoryRevertStatusAsync(
        [Path] string xml_presentation_id,
        [Query("task_id")] string task_id,
        CancellationToken cancellationToken = default);
}
