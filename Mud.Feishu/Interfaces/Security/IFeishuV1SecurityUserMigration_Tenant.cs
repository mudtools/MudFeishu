// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Security;

namespace Mud.Feishu;


/// <summary>
/// 飞书安全与合规（Security）「数据驻留与用户迁移」SDK 是一组服务端 OpenAPI 的封装，用于获取租户可用的数据驻留地理位置列表、迁移用户数据驻留位置、查询单个/批量用户迁移状态以及取消用户迁移任务。本接口全部端点为 security_and_compliance/v1，仅支持 tenant_access_token 调用。
/// <para><see href="https://open.feishu.cn/document/server-docs/security_and_compliance-v1/user_migration/create">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Security")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1SecurityUserMigration : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取数据驻留地理位置列表
    /// <para>获取租户可用的数据驻留地理位置列表。</para>
    /// <para>限频：100 次/分钟。所需权限（开启任一即可）：security_and_compliance:multi_geo_entity.tenant:readonly（查看数据驻留租户信息）、security_and_compliance:user_migration:multi-geo（查询、更新员工的数据驻留地）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/security_and_compliance-v1/user_migration/get-2">接口文档</see></para>
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回数据驻留租户信息（tenant.available_geo_locations 可选地理位置列表）</returns>
    [Get("/open-apis/security_and_compliance/v1/multi_geo_entity/tenant")]
    Task<FeishuApiResult<GetMultiGeoEntityTenantResult>?> GetMultiGeoEntityTenantAsync(
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 迁移用户数据驻留位置
    /// <para>将用户的数据驻留位置迁移到目标地理位置，一次最多迁移 100 个用户；用户已在目标地理位置或正在迁移中时会返回对应错误码。</para>
    /// <para>限频：10 次/分钟。所需权限（开启任一即可）：security_and_compliance:user_migration（创建、更新用户数据迁移）、security_and_compliance:user_migration:multi-geo（查询、更新员工的数据驻留地）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/security_and_compliance-v1/user_migration/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（user_ids 迁移用户 ID 列表必填 1~100 个；dest_geo 迁移目标地理位置区域必填，长度 2~10 字符）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id（必填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回用户迁移列表（user_migrations：user_id/dest_geo/task_id/status/progress）</returns>
    [Post("/open-apis/security_and_compliance/v1/user_migrations")]
    Task<FeishuApiResult<CreateUserMigrationResult>?> CreateUserMigrationAsync(
        [Body] CreateUserMigrationRequest request,
        [Query("user_id_type")] string user_id_type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取单个用户迁移状态
    /// <para>通过 user_id 获取指定用户当前的迁移状态。</para>
    /// <para>限频：100 次/分钟。所需权限（开启任一即可）：security_and_compliance:user_migration、security_and_compliance:user_migration:multi-geo、security_and_compliance:user_migration:readonly（查看用户数据迁移）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/security_and_compliance-v1/user_migration/get">接口文档</see></para>
    /// </summary>
    /// <param name="user_id">用户 ID，ID 类型必须与 user_id_type 的取值一致</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id（必填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回用户迁移信息（user_migration：user_id/dest_geo/task_id/status/progress）</returns>
    [Get("/open-apis/security_and_compliance/v1/user_migrations/{user_id}")]
    Task<FeishuApiResult<GetUserMigrationResult>?> GetUserMigrationAsync(
        [Path] string user_id,
        [Query("user_id_type")] string user_id_type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 批量获取用户迁移状态
    /// <para>传入用户 ID 列表，批量获取用户迁移状态，一次最多 500 个用户。</para>
    /// <para>限频：100 次/分钟。所需权限（开启任一即可）：security_and_compliance:user_migration、security_and_compliance:user_migration:multi-geo、security_and_compliance:user_migration:readonly（查看用户数据迁移）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/security_and_compliance-v1/user_migration/search">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（user_ids 用户 ID 列表必填 1~500 个）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id（必填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回用户迁移列表（items：user_id/dest_geo/task_id/status/progress）</returns>
    [Post("/open-apis/security_and_compliance/v1/user_migrations/search")]
    Task<FeishuApiResult<SearchUserMigrationsResult>?> SearchUserMigrationsAsync(
        [Body] SearchUserMigrationsRequest request,
        [Query("user_id_type")] string user_id_type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 取消用户迁移任务
    /// <para>取消用户迁移任务，仅能对未启动迁移的用户做此操作；用户迁移状态可通过获取单个用户迁移状态接口查询。</para>
    /// <para>限频：10 次/分钟。所需权限（开启任一即可）：security_and_compliance:user_migration（创建、更新用户数据迁移）、security_and_compliance:user_migration:multi-geo（查询、更新员工的数据驻留地）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/security_and_compliance-v1/user_migration/cancel">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（user_ids 取消迁移用户 ID 列表必填 1~100 个）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id（必填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/security_and_compliance/v1/user_migrations/cancel")]
    Task<FeishuNullDataApiResult?> CancelUserMigrationAsync(
        [Body] CancelUserMigrationRequest request,
        [Query("user_id_type")] string user_id_type,
        CancellationToken cancellationToken = default);
}
