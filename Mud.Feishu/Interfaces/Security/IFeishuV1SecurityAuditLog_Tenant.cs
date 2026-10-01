// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Security;

namespace Mud.Feishu;


/// <summary>
/// 飞书安全与合规（Security）「行为审计日志」SDK 用于查询成员的操作行为日志（时间、地点、操作对象等），管理员可借此发现违规操作以保护企业数据和信息安全。实际端点路径为 admin/v1/audit_infos，仅支持 tenant_access_token 调用。查询时请适当缩短查询时间范围并控制查询频次。
/// <para><see href="https://open.feishu.cn/document/server-docs/security_and_compliance-v1/audit_log/audit_data_get">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Security")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1SecurityAuditLog : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取行为审计日志数据
    /// <para>查询成员的操作行为日志，支持按事件名称、事件模块、操作者、操作对象、用户类型、时间范围（起止相差不超过 30 天）等筛选；查询参数采用查询对象模式 <see cref="GetAuditLogDataQuery"/>，见 AGENTS.md API-2。</para>
    /// <para>限频：100 次/分钟。所需权限：admin:audit_info:readonly（获取行为审计日志）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/security_and_compliance-v1/audit_log/audit_data_get">接口文档</see></para>
    /// </summary>
    /// <param name="query">时间范围（oldest/latest，秒级时间戳）、事件与操作者/对象筛选、分页参数等可选查询参数</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回行为审计日志分页列表（items：事件 ID/名称/模块/操作者/操作对象/接收者/环境信息等，page_token、has_more）</returns>
    [Get("/open-apis/admin/v1/audit_infos")]
    Task<FeishuApiResult<GetAuditLogDataResult>?> GetAuditLogDataAsync(
        [Query] GetAuditLogDataQuery? query = null,
        CancellationToken cancellationToken = default);
}
