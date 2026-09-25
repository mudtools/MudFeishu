// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu;


/// <summary>
/// 飞书招聘（Hire）职位广告 SDK 是一组服务端 OpenAPI 的封装，用于将职位广告发布至招聘渠道。本接口全部端点仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/publish"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Hire")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1HireAdvertisement : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 发布职位广告
    /// <para>将指定职位广告发布至所选招聘渠道（如官网招聘渠道）。</para>
    /// <para>限频：10 次/秒。所需权限：hire:advertisement（获取或更新招聘广告信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/publish">接口文档</see></para>
    /// </summary>
    /// <param name="advertisement_id">职位广告 ID，来自职位创建响应中的 default_job_post.id</param>
    /// <param name="request">发布请求体（job_channel_id 渠道 ID，可通过获取招聘渠道列表接口获取，如官网渠道为 "3"）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/hire/v1/advertisements/{advertisement_id}/publish")]
    Task<FeishuNullDataApiResult?> PublishAdvertisementAsync(
        [Path] string advertisement_id,
        [Body] PublishAdvertisementRequest request,
        CancellationToken cancellationToken = default);
}
