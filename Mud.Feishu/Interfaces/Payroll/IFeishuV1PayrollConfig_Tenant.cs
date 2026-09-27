// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Payroll;

namespace Mud.Feishu;


/// <summary>
/// 飞书薪酬发放（Payroll）「基础配置」SDK 是一组服务端 OpenAPI 的封装，用于批量查询算薪项、获取薪资组基本信息以及获取外部数据源配置信息。本接口全部端点为 payroll/v1，仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/payroll-v1/acct_item/list"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Payroll")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1PayrollConfig : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 批量查询算薪项
    /// <para>批量查询算薪项（含名称、分类、数据类型、小数位数与启用状态）。</para>
    /// <para>限频：5 次/秒。所需权限：payroll:payroll_calculation_item:read（获取算薪项信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/payroll-v1/acct_item/list">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">分页大小（必填），取值范围 1~100</param>
    /// <param name="page_token">分页标记，第一次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回算薪项分页列表（items：id/i18n_names/category_id/data_type/decimal_places/active_status，page_token、has_more）</returns>
    [Get("/open-apis/payroll/v1/acct_items")]
    Task<FeishuApiResult<ListAcctItemsResult>?> ListAcctItemsAsync(
        [Query("page_size")] int page_size,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取薪资组基本信息
    /// <para>返回所有薪资组的基本信息（薪资组 ID、名称、编码、状态等），不含薪资组下的员工信息。</para>
    /// <para>限频：20 次/分钟。所需权限：payroll:pay_groups:read（获取薪资组信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/payroll-v1/paygroup/list">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">分页大小，默认 100，取值范围 1~2000</param>
    /// <param name="page_token">分页标记，第一次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回薪资组分页列表（items：pay_group_id/name/code/status，page_token、has_more）</returns>
    [Get("/open-apis/payroll/v1/paygroups")]
    Task<FeishuApiResult<ListPaygroupsResult>?> ListPaygroupsAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取外部数据源配置信息
    /// <para>批量查询飞书人事后台「设置-算薪数据设置-外部数据源设置」中的数据源设置列表。注意：停用的数据源、字段不能保存数据。</para>
    /// <para>限频：10 次/秒。所需权限：payroll:external_datasource_configuration:read（Payroll 外部数据源设置读权限）。</para>
    /// <para><see href="https://open.feishu.cn/document/payroll-v1/datasource/list">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">分页大小（必填），取值范围 1~100</param>
    /// <param name="page_token">分页标记，第一次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回数据源分页列表（datasources：code/i18n_names/active_status/fields/i18n_description/data_period_type，page_token、has_more）</returns>
    [Get("/open-apis/payroll/v1/datasources")]
    Task<FeishuApiResult<ListDatasourcesResult>?> ListDatasourcesAsync(
        [Query("page_size")] int page_size,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);
}
