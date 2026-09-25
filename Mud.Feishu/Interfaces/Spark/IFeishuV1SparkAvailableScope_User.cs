// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Spark;

namespace Mud.Feishu;


/// <summary>
/// 飞书妙搭（Spark）产品使用权限 SDK 是一组服务端 OpenAPI 的封装，用于获取与修改妙搭产品的企业级使用权限配置（可用范围模式 + 允许/禁止名单）。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app-available_scope/open_api_get_miaoda_available_scope"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Spark")]
[Token(FeishuTokenTypes.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV1SparkAvailableScope : IFeishuAppContextSwitcher, ICurrentUserId
{
    /// <summary>
    /// 获取妙搭产品使用权限
    /// <para>获取当前生效的产品使用权限配置（可用范围模式 + 允许/禁止的部门/成员名单）。</para>
    /// <para>限频：20 次/秒。所需权限：spark:admin:read（获取妙搭企业级管理配置）。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app-available_scope/open_api_get_miaoda_available_scope">接口文档</see></para>
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回使用权限范围类型与部门/成员名单</returns>
    [Get("/open-apis/spark/v1/available_scope")]
    Task<FeishuApiResult<GetAvailableScopeResult>?> GetAvailableScopeAsync(
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 修改妙搭产品使用权限
    /// <para>配置允许（或禁止）使用产品的部门/成员范围，设置立即生效。</para>
    /// <para>限频：20 次/秒。所需权限：spark:admin:write（更新妙搭企业级管理配置）。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app-available_scope/open_api_update_miaoda_available_scope">接口文档</see></para>
    /// </summary>
    /// <param name="request">更新请求体（available_scope 必填：ALLOW_ALL/DENY_ALL/ALLOW_PART/DENY_PART；department_ids、user_ids 仅 ALLOW_PART/DENY_PART 时生效，各 0～1000）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>修改成功时 data 为空对象</returns>
    [Put("/open-apis/spark/v1/available_scope")]
    Task<FeishuNullDataApiResult?> UpdateAvailableScopeAsync(
        [Body] UpdateAvailableScopeRequest request,
        CancellationToken cancellationToken = default);
}
