// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Acs;

namespace Mud.Feishu;


/// <summary>
/// 飞书智能门禁（ACS）权限组 SDK 是一组服务端 OpenAPI 的封装，用于创建/更新权限组、删除权限组、查询设备权限组信息，以及将设备绑定到权限组。本接口全部端点仅支持 user_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/acs-v1/rule_external/create"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Acs")]
[Token(FeishuTokenTypes.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV1AcsRuleExternal : IFeishuAppContextSwitcher, ICurrentUserId
{
    /// <summary>
    /// 创建或更新权限组
    /// <para>rule_id 为空时创建权限组，不为空时更新对应权限组；权限组可包含设备、员工与访客名单及开门时间段。</para>
    /// <para>限频：5 次/秒。所需权限：acs:device:write（门禁机设备写入权限）或 acs:users（任一即可）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。</para>
    /// <para><see href="https://open.feishu.cn/document/acs-v1/rule_external/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">权限组信息（rule 必填：name 权限组名称、devices 设备列表（0~5000）、users/visitors 成员列表（0~10000，user_type 必填：1 员工/2 部门/10 全体员工/11 访客）、opening_time 开门时间段、is_temp 是否临时权限组等）</param>
    /// <param name="rule_id">权限组 ID：为空创建，不为空则更新，示例值：7298933941867135276</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回权限组 ID（rule_id）</returns>
    [Post("/open-apis/acs/v1/rule_external")]
    Task<FeishuApiResult<CreateOrUpdateRuleExternalResult>?> CreateOrUpdateRuleExternalAsync(
        [Body] CreateOrUpdateRuleExternalRequest request,
        [Query("rule_id")] string? rule_id = null,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除权限组
    /// <para>按权限组 ID 删除权限组。</para>
    /// <para>限频：5 次/秒。所需权限：acs:device:write（门禁机设备写入权限）或 acs:users（任一即可）。</para>
    /// <para><see href="https://open.feishu.cn/document/acs-v1/rule_external/delete">接口文档</see></para>
    /// </summary>
    /// <param name="rule_id">权限组 ID，示例值：7298933941867135276</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Delete("/open-apis/acs/v1/rule_external")]
    Task<FeishuNullDataApiResult?> DeleteRuleExternalAsync(
        [Query("rule_id")] string rule_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 设备绑定权限组
    /// <para>将门禁设备绑定到指定的权限组列表。</para>
    /// <para>限频：5 次/秒。所需权限：acs:device:write（门禁机设备写入权限）或 acs:users（任一即可）。</para>
    /// <para><see href="https://open.feishu.cn/document/acs-v1/rule_external/device_bind">接口文档</see></para>
    /// </summary>
    /// <param name="request">绑定请求体（device_id 设备 ID 必填；rule_ids 权限组 ID 列表必填，0~10000 个）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/acs/v1/rule_external/device_bind")]
    Task<FeishuNullDataApiResult?> DeviceBindRuleExternalAsync(
        [Body] DeviceBindRuleExternalRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取权限组信息
    /// <para>按设备 ID（可选）查询设备权限组信息，包含权限组名称、设备、员工/访客名单与开门时间段等。</para>
    /// <para>限频：5 次/秒。所需权限：acs:devices:readonly（查看智能门禁设备列表）或 acs:users（任一即可）；字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/acs-v1/rule_external/get">接口文档</see></para>
    /// </summary>
    /// <param name="device_id">设备 ID，示例值：7296700518380863767</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回设备权限组信息列表（rules）</returns>
    [Get("/open-apis/acs/v1/rule_external")]
    Task<FeishuApiResult<GetRuleExternalResult>?> GetRuleExternalListAsync(
        [Query("device_id")] string? device_id = null,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);
}
