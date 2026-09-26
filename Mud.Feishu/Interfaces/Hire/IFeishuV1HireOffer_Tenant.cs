// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu;


/// <summary>
/// 飞书招聘（Hire）Offer 域 SDK 是一组服务端 OpenAPI 的封装，用于招聘配置中的 Offer 申请表列表与模板 Schema 查询、申请表自定义字段更新、Offer 审批模板查询。本接口全部端点仅支持 tenant_access_token 调用（投递流程中的 Offer 创建/更新/查询/列表/状态变更见 <see cref="IFeishuTenantV1HireCandidate"/>）。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-development-guide"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Hire")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1HireOffer : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取 Offer 申请表列表
    /// <para>分页获取 Offer 申请表列表，返回申请表 ID、名称与创建时间。</para>
    /// <para>限频：10 次/秒。所需权限：hire:offer_schema:readonly（获取 Offer 申请表信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/offer-settings/offer_application_form/list">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">每页数量，默认 1</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回 Offer 申请表分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/offer_application_forms")]
    Task<FeishuApiResult<GetOfferApplicationFormListResult>?> GetOfferApplicationFormListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取 Offer 申请表模板信息
    /// <para>按申请表 ID 获取 Offer 申请表模板的完整 Schema 信息，包括模块、字段、选项、公式与联动显示配置。公式（formula）类型字段由其他字段计算得出，创建 Offer 时无需也不可传入。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:offer_schema:readonly（获取 Offer 申请表信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/offer-settings/offer_application_form/get">接口文档</see></para>
    /// </summary>
    /// <param name="offer_application_form_id">Offer 申请表 ID，示例值：237186812432</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回 Offer 申请表详情（offer_apply_form）</returns>
    [Get("/open-apis/hire/v1/offer_application_forms/{offer_application_form_id}")]
    Task<FeishuApiResult<GetOfferApplicationFormResult>?> GetOfferApplicationFormAsync(
        [Path] string offer_application_form_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新 Offer 申请表自定义字段
    /// <para>更新 Offer 申请表自定义字段的名称与选项配置；不支持修改字段类型，公式类型字段不支持更新。每次更新后所有申请表的 schema_id 会升级为新版本。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:offer_selection_object（更新 Offer 自定义字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/offer-settings/offer_application_form/update">接口文档</see></para>
    /// </summary>
    /// <param name="offer_custom_field_id">Offer 申请表自定义字段 ID，可通过获取 Offer 申请表模板信息接口获取，示例值：6906755946257615112</param>
    /// <param name="request">更新请求体（name 必填，zh_cn/en_us 至少一个；仅单选/多选字段需传 config.options）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Put("/open-apis/hire/v1/offer_custom_fields/{offer_custom_field_id}")]
    Task<FeishuNullDataApiResult?> UpdateOfferCustomFieldAsync(
        [Path] string offer_custom_field_id,
        [Body] UpdateOfferCustomFieldRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取 Offer 审批模板列表
    /// <para>分页获取 Offer 审批模板列表，返回模板名称、创建时间、备注与适用部门。</para>
    /// <para>限频：10 次/秒。所需权限：hire:offer_approval_template:readonly（获取 Offer 审批流程配置）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/offer-settings/offer_approval_template/list">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">每页数量，最大 200，默认 10</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="department_id_type">部门 ID 类型（open_department_id/department_id/people_admin_department_id），默认 people_admin_department_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回 Offer 审批模板分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/offer_approval_templates")]
    Task<FeishuApiResult<GetOfferApprovalTemplateListResult>?> GetOfferApprovalTemplateListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        [Query("department_id_type")] string? department_id_type = null,
        CancellationToken cancellationToken = default);
}
