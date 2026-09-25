// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu;


/// <summary>
/// 飞书招聘（Hire）职位广告发布记录 SDK 是一组服务端 OpenAPI 的封装，用于按招聘渠道查询职位发布记录。本接口全部端点仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/search"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Hire")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1HireJobPublishRecord : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 查询职位发布记录
    /// <para>按招聘渠道分页查询已发布到官网/拉勾等渠道的职位广告记录（查询参数采用查询对象模式 <see cref="JobPublishRecordSearchQuery"/>，见 AGENTS.md API-2）。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:job:readonly（获取职位信息）或 hire:job（更新职位）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/search">接口文档</see></para>
    /// </summary>
    /// <param name="request">查询请求体（job_channel_id 渠道 ID，如官网渠道取 "2"、拉勾渠道取 "3"，可通过获取招聘渠道列表接口获取）</param>
    /// <param name="query">分页与各类 ID 类型查询参数</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回职位发布记录分页列表（items、has_more、page_token）</returns>
    [Post("/open-apis/hire/v1/job_publish_records/search")]
    Task<FeishuApiResult<SearchJobPublishRecordResult>?> SearchJobPublishRecordAsync(
        [Body] SearchJobPublishRecordRequest request,
        [Query] JobPublishRecordSearchQuery? query = null,
        CancellationToken cancellationToken = default);
}
