// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Payroll;

namespace Mud.Feishu;


/// <summary>
/// 飞书薪酬发放（Payroll）「外部算薪数据」SDK 是一组服务端 OpenAPI 的封装，用于按数据源批量保存（创建或更新）与批量查询外部算薪数据记录。本接口全部端点为 payroll/v1，仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/payroll-v1/datasource_record/save"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Payroll")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1PayrollDatasourceRecord : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 创建 / 更新外部算薪数据
    /// <para>参照数据源配置字段格式，批量保存（创建或更新）数据记录；记录的唯一标志通过业务主键判断（employment_id + payroll_period），已存在则覆盖更新（只更新传入字段），返回产生数据变更的记录条数。注意：发生报错时全部保存失败；单个数据源只允许串行批量写入，需调用端做好并发控制。</para>
    /// <para>限频：10 次/秒。所需权限：payroll:external_datasource_record:write（Payroll 外部数据记录写入权限），并需确保应用拥有写入员工所在薪资组的数据授权。</para>
    /// <para><see href="https://open.feishu.cn/document/payroll-v1/datasource_record/save">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（source_code 数据源 code 必填；records 记录列表必填 1~50 条，每条含 active_status 与 field_values）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回变更的记录条数（affect_counts，数字类型的字符串）</returns>
    [Post("/open-apis/payroll/v1/datasource_records/save")]
    Task<FeishuApiResult<SaveDatasourceRecordsResult>?> SaveDatasourceRecordsAsync(
        [Body] SaveDatasourceRecordsRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 批量查询外部算薪数据记录
    /// <para>支持通过 payroll_period（必传）、employment_id（可选）等预置字段，批量查询指定数据源下的数据记录列表。</para>
    /// <para>限频：10 次/秒。所需权限：payroll:external_datasource_record:read（Payroll 外部数据记录查询权限），并需确保应用拥有员工所在薪资组的数据授权。</para>
    /// <para><see href="https://open.feishu.cn/document/payroll-v1/datasource_record/query">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（source_code 数据源 code 必填；selected_fields 指定查询的字段 code 可选；field_filters 查询条件列表可选，多条件 And 关系）</param>
    /// <param name="page_size">分页大小（必填），取值范围 1~100</param>
    /// <param name="page_token">分页标记，第一次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回数据记录分页列表（records：active_status/field_values，page_token、has_more）</returns>
    [Post("/open-apis/payroll/v1/datasource_records/query")]
    Task<FeishuApiResult<QueryDatasourceRecordsResult>?> QueryDatasourceRecordsAsync(
        [Body] QueryDatasourceRecordsRequest request,
        [Query("page_size")] int page_size,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);
}
