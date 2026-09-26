// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.ApprovalDistrict;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 审批地理库：获取审批的地理库数据，用于在发起审批时填写地址控件的区域信息。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/approval-v4/district/list"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV4ApprovalDistrict : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 查询地理库信息。获取审批的地理库数据，用于在发起审批时填写地址控件的区域信息。
    /// <para>官方文档：<see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=list&amp;project=approval&amp;resource=district&amp;version=v4"/></para>
    /// </summary>
    /// <param name="page_size">分页大小，即本次请求所返回的最大条目数。最大值：100，默认值：20</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="root_district_id">根区域 ID，如果不传，默认从国家层级开始查询。</param>
    /// <param name="list_type">遍历方式。可选值：parent_level：按父层级整层遍历；leaf_level：递归遍历，一直到叶子层级。</param>
    /// <param name="locale">语言。默认为 zh-CN（中文）。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/approval/v4/districts")]
    Task<FeishuApiResult<ListDistrictResult>?> GetDistrictsPageListAsync(
        [Query("page_size")] int page_size = Consts.PageSize_20,
        [Query("page_token")] string? page_token = null,
        [Query("root_district_id")] string? root_district_id = null,
        [Query("list_type")] string? list_type = null,
        [Query("locale")] string? locale = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索地理库信息。搜索审批的地理库数据，可用于在发起审批时填写地址控件的区域信息。
    /// <para>官方文档：<see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=search&amp;project=approval&amp;resource=district&amp;version=v4"/></para>
    /// </summary>
    /// <param name="searchDistrictRequest">搜索地理库信息请求体</param>
    /// <param name="locale">语言。默认为 zh-CN（中文）。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/approval/v4/districts/search")]
    Task<FeishuApiResult<SearchDistrictResult>?> SearchDistrictsAsync(
        [Body] SearchDistrictRequest searchDistrictRequest,
        [Query("locale")] string? locale = null,
        CancellationToken cancellationToken = default);
}
