// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu;


/// <summary>
/// 飞书招聘（Hire）职位类别 SDK 是一组服务端 OpenAPI 的封装，用于获取招聘系统预置的职位类别列表。本接口全部端点仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/list-4"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Hire")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1HireJobType : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取职位类别列表
    /// <para>分页获取招聘系统预置的职位类别列表，按创建时间升序返回，并包含节点的父子层级关系（parent_id），可用于构建职位类别树。</para>
    /// <para>限频：20 次/秒。所需权限：hire:job:readonly（获取职位信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/list-4">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">每页数量，默认 10</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回职位类别分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/job_types")]
    Task<FeishuApiResult<GetJobTypeListResult>?> GetJobTypeListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);
}
