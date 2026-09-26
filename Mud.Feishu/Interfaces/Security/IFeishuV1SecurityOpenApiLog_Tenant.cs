// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Security;

namespace Mud.Feishu;


/// <summary>
/// 飞书安全与合规（Security）「OpenAPI 审计日志」SDK 用于获取 OpenAPI 审计日志数据（调用方、时间、请求与响应摘要等）。本接口为 security_and_compliance/v1 端点，仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/security_and_compliance-v1/openapi_log/list_data"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Security")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1SecurityOpenApiLog : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取 OpenAPI 审计日志数据
    /// <para>获取 OpenAPI 审计日志数据，可按 API 列表、时间范围、应用 ID 筛选并分页返回。</para>
    /// <para>限频：100 次/分钟。所需权限：security_and_compliance:audit_log.openapi_log:readonly（查看 OpenAPI 审计日志）。</para>
    /// <para><see href="https://open.feishu.cn/document/security_and_compliance-v1/openapi_log/list_data">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（api_keys API 列表最大 100 个；start_time/end_time 秒级时间戳；app_id 应用唯一标识；page_size 分页大小 1~100；page_token 分页标记）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回 openapi 日志分页列表（items：id/api_key/event_time/app_id/ip/log_detail，page_token、has_more）</returns>
    [Post("/open-apis/security_and_compliance/v1/openapi_logs/list_data")]
    Task<FeishuApiResult<ListOpenApiLogDataResult>?> ListOpenApiLogDataAsync(
        [Body] ListOpenApiLogDataRequest request,
        CancellationToken cancellationToken = default);
}
