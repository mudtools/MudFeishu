// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Acs;

namespace Mud.Feishu;


/// <summary>
/// 飞书智能门禁（ACS）访客 SDK 是一组服务端 OpenAPI 的封装，用于以用户身份添加访客与删除访客。本接口全部端点仅支持 user_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/acs-v1/visitor/create"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Acs")]
[Token(FeishuTokenTypes.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV1AcsVisitor : IFeishuAppContextSwitcher, ICurrentUserId
{
    /// <summary>
    /// 添加访客
    /// <para>添加智能门禁访客，返回访客 ID；访客信息中 user_type 必填（示例值 11 表示访客）。</para>
    /// <para>限频：5 次/秒。所需权限：acs:users（查看、更新智能门禁用户，任一即可）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。</para>
    /// <para><see href="https://open.feishu.cn/document/acs-v1/visitor/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">访客信息（user 必填：user_type 必填（1 员工/2 部门/10 全体员工/11 访客）、user_id、user_name、phone_num、department_id）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回访客的 ID（visitor_id）</returns>
    [Post("/open-apis/acs/v1/visitors")]
    Task<FeishuApiResult<CreateVisitorResult>?> CreateVisitorAsync(
        [Body] CreateVisitorRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除访客
    /// <para>按访客 ID 删除智能门禁访客。</para>
    /// <para>限频：5 次/秒。所需权限：acs:users（查看、更新智能门禁用户，任一即可）；字段权限：contact:user.employee_id:readonly。</para>
    /// <para><see href="https://open.feishu.cn/document/acs-v1/visitor/delete">接口文档</see></para>
    /// </summary>
    /// <param name="visitor_id">访客 ID，示例值：6939433228970082566</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Delete("/open-apis/acs/v1/visitors/{visitor_id}")]
    Task<FeishuNullDataApiResult?> DeleteVisitorAsync(
        [Path] string visitor_id,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);
}
