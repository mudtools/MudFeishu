// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Bitable;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// <para>飞书多维表格高级权限允许用户针对单一数据表设置哪些用户可以查看、编辑指定的行，或是设置针对某用户可以编辑的列。。</para>
/// <para>高级权限接口分为 自定义角色 和 协作者 两部分，多维表格的 所有者 或者 有可管理权限 的用户可通过接口设置高级权限，管理高级权限的协作者。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/advanced-permission/advanced-permission-guide"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(TokenType.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1BitableRole : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 新增自定义角色
    /// <para>新增多维表格高级权限中自定义的角色。</para>
    /// <para><see href="https://open.feishu.cn/document/docs/bitable-v1/advanced-permission/app-role/create-2">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>   
    /// <param name="createRoleRequest">新增自定义角色请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/base/v2/apps/{app_token}/roles")]
    Task<FeishuApiResult<CreateRoleResult>?> CreateRoleAsync(
        [Path] string app_token,
        [Body] CreateRoleRequest createRoleRequest,
        CancellationToken cancellationToken = default);


}