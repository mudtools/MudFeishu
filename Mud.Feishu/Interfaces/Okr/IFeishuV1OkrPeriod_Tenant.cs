// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Okr;

namespace Mud.Feishu;


/// <summary>
/// 飞书 OKR「周期与周期规则」SDK 是一组服务端 OpenAPI 的封装，用于按周期规则创建周期、修改周期显示状态、获取租户下的 OKR 周期列表与周期规则列表。本接口全部端点为 okr/v1，仅支持 tenant_access_token 调用。
/// <para><see href="https://open.feishu.cn/document/server-docs/okr-v1/period/list">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Okr")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1OkrPeriod : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 创建 OKR 周期
    /// <para>按照指定的周期规则创建一个新的 OKR 周期（如按规则生成 2022-01 起始的考核周期）。</para>
    /// <para>限频：100 次/分钟。所需权限：okr:okr（更新 OKR）。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/okr-v1/period/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">创建周期请求体（period_rule_id 周期规则 id 必填；start_month 周期起始年月必填，格式 2022-01）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回新建周期信息（period_id、start_month、end_month）</returns>
    [Post("/open-apis/okr/v1/periods")]
    Task<FeishuApiResult<CreatePeriodResult>?> CreatePeriodAsync(
        [Body] CreatePeriodRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 修改 OKR 周期状态
    /// <para>修改某个 OKR 周期的显示状态为「正常 / 失效 / 隐藏」。该修改对租户内所有用户生效，请谨慎调用。</para>
    /// <para>限频：100 次/分钟。所需权限：okr:okr（更新 OKR）。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/okr-v1/period/patch">接口文档</see></para>
    /// </summary>
    /// <param name="period_id">周期 id，示例值：6969864184272078374</param>
    /// <param name="request">修改周期状态请求体（status 周期显示状态必填：1 正常、2 失效、3 隐藏）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回周期 id 与修改后的显示状态</returns>
    [Patch("/open-apis/okr/v1/periods/{period_id}")]
    Task<FeishuApiResult<PatchPeriodResult>?> PatchPeriodAsync(
        [Path] string period_id,
        [Body] PatchPeriodRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取 OKR 周期列表
    /// <para>分页获取当前租户下的 OKR 周期列表（id、中英文名称、启用状态、起止时间）。</para>
    /// <para>限频：100 次/分钟。所需权限（任一即可）：okr:okr:readonly（获取 OKR 信息）、okr:okr（更新 OKR）。支持的应用类型：自建应用、商店应用。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/okr-v1/period/list">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">分页大小，默认 10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回 OKR 周期分页列表（items、page_token、has_more）</returns>
    [Get("/open-apis/okr/v1/periods")]
    Task<FeishuApiResult<ListPeriodsResult>?> ListPeriodsAsync(
        [Query("page_size")] int page_size = Consts.PageSize_10,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取 OKR 周期规则列表
    /// <para>获取当前租户下的 OKR 周期规则列表（周期类型、周期长度、每年首个开始月份），可用于创建周期时选择 period_rule_id。</para>
    /// <para>限频：100 次/分钟。所需权限（任一即可）：okr:okr:readonly（获取 OKR 信息）、okr:okr（更新 OKR）。支持的应用类型：自建应用、商店应用。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/okr-v1/period_rule/list">接口文档</see></para>
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回周期规则列表（period_rules：period_rule_id/type/length/first_month）</returns>
    [Get("/open-apis/okr/v1/period_rules")]
    Task<FeishuApiResult<ListPeriodRulesResult>?> ListPeriodRulesAsync(
        CancellationToken cancellationToken = default);
}
