// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu;


/// <summary>
/// 飞书招聘（Hire）地点 SDK 是一组服务端 OpenAPI 的封装，用于按地点码查询地点与获取地点（工作地/面试地）列表。本接口全部端点仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/location/list"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Hire")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1HireLocation : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 查询地点列表
    /// <para>根据地点类型（国家/省份/城市/区县）与地点码批量查询地点信息，获取地点名称（中文、英文、拼音）。</para>
    /// <para>限频：5 次/秒。所需权限：hire:location:readonly（获取地点信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/location/query">接口文档</see></para>
    /// </summary>
    /// <param name="request">查询请求体（location_type 必填：1 国家 / 2 省份 / 3 城市 / 4 区县；code_list 地点码列表，可选，最大 100 个，不填则查询全部）</param>
    /// <param name="page_size">每页数量，必填，取值范围 1～100</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回地点信息分页列表（items、has_more、page_token）</returns>
    [Post("/open-apis/hire/v1/locations/query")]
    Task<FeishuApiResult<QueryLocationResult>?> QueryLocationAsync(
        [Body] QueryLocationRequest request,
        [Query("page_size")] int page_size,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取地点列表
    /// <para>获取飞书招聘内置的工作地/面试地地点列表，返回区县、城市、省份、国家的层级编码与名称。</para>
    /// <para>限频：特殊限频（详见接口文档）。所需权限：hire:location:readonly（获取地点信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/location/list">接口文档</see></para>
    /// </summary>
    /// <param name="usage">地点用途，必填：position_location（工作地）/ interview_location（面试地）</param>
    /// <param name="page_size">每页数量，取值范围 1～100</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回地点分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/locations")]
    Task<FeishuApiResult<GetLocationListResult>?> GetLocationListAsync(
        [Query("usage")] string usage,
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);
}
