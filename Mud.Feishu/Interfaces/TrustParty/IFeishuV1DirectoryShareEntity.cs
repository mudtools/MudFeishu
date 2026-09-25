// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.TrustParty;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 飞书共享成员范围（directory/v1/share_entities）SDK 用于查询本组织与对方关联组织之间双向共享的部门、用户组与成员范围，为配置可搜可见规则时选取主客体实体提供依据。全部端点同时支持 tenant_access_token 与 user_access_token，调用者需具备关联组织管理员权限。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/trust_party-v1/-collaboraiton-organization/list-3"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1DirectoryShareEntity : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取关联组织双方共享成员范围
    /// <para>分页查询与对方关联组织之间共享的部门、用户组与成员列表（查询参数采用查询对象模式 <see cref="ShareEntityListQuery"/>，见 AGENTS.md API-2）。仅支持自建应用。</para>
    /// <para>限频：100 次/分钟。所需权限：trust_party:collaboration_rule:read（读取关联组织协作规则）；同时需要关联组织管理员权限。</para>
    /// <para><see href="https://open.feishu.cn/document/trust_party-v1/-collaboraiton-organization/list-3">接口文档</see></para>
    /// </summary>
    /// <param name="query">对方组织 tenant key（必填）、部门/用户组 ID、是否查主体侧及分页等查询参数</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回共享部门、用户组与成员分页列表（share_departments、share_groups、share_users、has_more、page_token）</returns>
    [Get("/open-apis/directory/v1/share_entities")]
    Task<FeishuApiResult<GetShareEntityListResult>?> GetShareEntityListAsync(
        [Query] ShareEntityListQuery? query = null,
        CancellationToken cancellationToken = default);
}
