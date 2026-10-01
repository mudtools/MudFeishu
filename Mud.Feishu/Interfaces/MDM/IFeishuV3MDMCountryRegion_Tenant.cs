// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.MDM;

namespace Mud.Feishu;


/// <summary>
/// 飞书主数据管理（MDM）「国家/地区」SDK 是一组服务端 OpenAPI 的封装，用于通过 mdmcode 批量查询以及分页批量查询国家/地区主数据（行政编码、多语言名称、大洲归属等，支持多语言并定期更新）。本接口全部端点为 mdm/v3，仅支持 tenant_access_token 调用。
/// <para><see href="https://open.feishu.cn/document/mdm-v1/mdm-v3/country_region/get">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "MDM")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV3MDMCountryRegion : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 通过 mdmcode 批量查询国家/地区信息
    /// <para>通过 mdmcode 批量查询国家/地区信息，一次最多 100 个编码。</para>
    /// <para>限频：10 次/秒。所需权限：mdm:country_region:read（查询国家/地区的公共数据）。</para>
    /// <para><see href="https://open.feishu.cn/document/mdm-v1/mdm-v3/country_region/get">接口文档</see></para>
    /// </summary>
    /// <param name="fields">需要的查询字段集（必填，1~100 个），可选字段见国家/地区资源定义，示例值：["name"]</param>
    /// <param name="ids">主数据编码集（必填，1~100 个），格式为 "MDCT+8位数字"，示例值：["MDCT00000001"]</param>
    /// <param name="languages">希望返回的语言种类（必填，0~100 个）：zh-CN（中文）/ en-US（英文）/ ja-JP（日文），示例值：["en-US"]</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回国家/地区目录列表（data：mdm_code/name/full_name（多语言）/alpha_3_code/alpha_2_code/numeric_code/global_code/continents/status）</returns>
    [Get("/open-apis/mdm/v3/batch_country_region")]
    Task<FeishuApiResult<GetBatchCountryRegionResult>?> GetBatchCountryRegionAsync(
        [Query("fields")] string[] fields,
        [Query("ids")] string[] ids,
        [Query("languages")] string[] languages,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 分页批量查询国家/地区
    /// <para>分页批量查询国家/地区，支持通过 filter 过滤参数（字段 + 运算符 + 值，同一层级多个条件由 logic 指定「与/或」）筛选。</para>
    /// <para>限频：10 次/秒。所需权限：mdm:country_region:read（查询国家/地区的公共数据）。</para>
    /// <para><see href="https://open.feishu.cn/document/mdm-v1/mdm-v3/country_region/list">接口文档</see></para>
    /// </summary>
    /// <param name="query">语言种类与字段集（languages/fields 必填）、limit（1~1000）、offset（0~100000）、return_count、page_token 等查询参数，见 <see cref="ListCountryRegionsQuery"/></param>
    /// <param name="request">请求体（filter 过滤参数，可选）：logic（0=and，1=or）与 expressions（field/operator/value）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回国家/地区目录列表（data）、总数（total）与下一次分页参数（next_page_token）</returns>
    [Get("/open-apis/mdm/v3/country_regions")]
    Task<FeishuApiResult<ListCountryRegionsResult>?> ListCountryRegionsAsync(
        [Query] ListCountryRegionsQuery query,
        [Body] ListCountryRegionsRequest? request = null,
        CancellationToken cancellationToken = default);
}
