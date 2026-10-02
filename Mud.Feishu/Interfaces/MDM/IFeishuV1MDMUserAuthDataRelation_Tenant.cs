// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.MDM;

namespace Mud.Feishu;


/// <summary>
/// 飞书主数据管理（MDM）「用户数据维度」SDK 用于为指定应用下的用户绑定或解绑一类数据维度（支持批量对多个用户同时增量授权/解除授权）。本接口全部端点为 mdm/v1，仅支持 tenant_access_token 调用。
/// <para><see href="https://open.feishu.cn/document/server-docs/mdm-v1/user_auth_data_relation/bind">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "MDM")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1MDMUserAuthDataRelation : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 用户数据维度绑定
    /// <para>通过该接口，可为指定应用下的用户绑定一类数据维度，支持批量给多个用户同时增量授权。</para>
    /// <para>限频：100 次/分钟。所需权限：mdm:user_auth（绑定及解绑用户数据维度权限）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mdm-v1/user_auth_data_relation/bind">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（root_dimension_type 数据类型编码必填；sub_dimension_types 数据编码列表必填 1~200 个；authorized_user_ids 授权人的 lark id 必填 1~200 个；uams_app_id uams 系统中应用 id 必填）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/mdm/v1/user_auth_data_relations/bind")]
    Task<FeishuNullDataApiResult?> BindUserAuthDataRelationAsync(
        [Body] BindUserAuthDataRelationRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 用户数据维度解绑
    /// <para>通过该接口，可为指定应用下的指定用户解除一类数据维度。</para>
    /// <para>限频：100 次/分钟。所需权限：mdm:user_auth（绑定及解绑用户数据维度权限）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mdm-v1/user_auth_data_relation/unbind">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（root_dimension_type 数据类型编码必填；sub_dimension_types 数据编码列表必填 1~200 个；authorized_user_ids 授权人的 lark id 必填 1~200 个；uams_app_id uams 系统中应用 id 必填）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/mdm/v1/user_auth_data_relations/unbind")]
    Task<FeishuNullDataApiResult?> UnbindUserAuthDataRelationAsync(
        [Body] UnbindUserAuthDataRelationRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);
}
