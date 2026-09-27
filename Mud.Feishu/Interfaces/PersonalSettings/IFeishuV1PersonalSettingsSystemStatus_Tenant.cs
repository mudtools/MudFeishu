// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.PersonalSettings;

namespace Mud.Feishu;


/// <summary>
/// 飞书个人设置（PersonalSettings）「系统状态」SDK 是一组服务端 OpenAPI 的封装，用于管理租户维度的系统状态（创建、删除、修改、查询）以及为用户批量开启/关闭系统状态。每个租户最多创建 10 个系统状态；操作的数据为租户维度数据，请小心操作。本接口全部端点为 personal_settings/v1，仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/personal_settings-v1/system_status/list"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "PersonalSettings")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1PersonalSettingsSystemStatus : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 创建系统状态
    /// <para>创建租户维度的系统状态；每个租户最多创建 10 个系统状态，不同系统状态的 title（及各语言 i18n_title）与优先级不能重复。</para>
    /// <para>限频：100 次/分钟。所需权限：personal_settings:status:system_status_update（获取与更新系统状态）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/personal_settings-v1/system_status/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（title 名称必填，1~20 字符；icon_key 图标必填；i18n_title/color/priority/sync_setting 可选）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回创建的系统状态（system_status：system_status_id/title/i18n_title/icon_key/color/priority/sync_setting）</returns>
    [Post("/open-apis/personal_settings/v1/system_statuses")]
    Task<FeishuApiResult<CreateSystemStatusResult>?> CreateSystemStatusAsync(
        [Body] CreateSystemStatusRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除系统状态
    /// <para>删除租户维度的系统状态；删除后并不影响正在使用该状态用户的客户端展示。</para>
    /// <para>限频：100 次/分钟。所需权限：personal_settings:status:system_status_update（获取与更新系统状态）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/personal_settings-v1/system_status/delete">接口文档</see></para>
    /// </summary>
    /// <param name="system_status_id">系统状态 ID，通过获取系统状态接口获取</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Delete("/open-apis/personal_settings/v1/system_statuses/{system_status_id}")]
    Task<FeishuNullDataApiResult?> DeleteSystemStatusAsync(
        [Path] string system_status_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 修改系统状态
    /// <para>修改租户维度系统状态；修改对已经开启过该系统状态的用户无效，用户客户端将在该状态再次可用时同步到更新后的内容。</para>
    /// <para>限频：100 次/分钟。所需权限：personal_settings:status:system_status_update（获取与更新系统状态）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/personal_settings-v1/system_status/patch">接口文档</see></para>
    /// </summary>
    /// <param name="system_status_id">系统状态 ID，通过获取系统状态接口获取</param>
    /// <param name="request">请求体（system_status 系统状态必填；update_fields 需要更新的字段必填：TITLE/I18N_TITLE/ICON/COLOR/PRIORITY/SYNC_SETTING）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回修改后的系统状态（system_status）</returns>
    [Patch("/open-apis/personal_settings/v1/system_statuses/{system_status_id}")]
    Task<FeishuApiResult<PatchSystemStatusResult>?> PatchSystemStatusAsync(
        [Path] string system_status_id,
        [Body] PatchSystemStatusRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取系统状态
    /// <para>获取租户下所有系统状态。</para>
    /// <para>限频：100 次/分钟。所需权限：personal_settings:status:system_status_update（获取与更新系统状态）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/personal_settings-v1/system_status/list">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">分页大小，默认 50，取值范围 1~50</param>
    /// <param name="page_token">分页标记，第一次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回租户系统状态分页列表（items、page_token、has_more）</returns>
    [Get("/open-apis/personal_settings/v1/system_statuses")]
    Task<FeishuApiResult<ListSystemStatusesResult>?> ListSystemStatusesAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 批量开启系统状态
    /// <para>为用户批量开启指定的系统状态，一次最多 50 个用户；结束时间距当前的时间跨度不能超过 365 天。</para>
    /// <para>限频：100 次/分钟。所需权限：personal_settings:status:system_status_update（获取与更新系统状态）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/personal_settings-v1/system_status/batch_open">接口文档</see></para>
    /// </summary>
    /// <param name="system_status_id">系统状态 ID，通过获取系统状态接口获取</param>
    /// <param name="request">请求体（user_list 开启列表必填 1~50 个，每项含 user_id 与 end_time 秒级时间戳）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回逐用户的开启结果列表（result_list：user_id/end_time/result，如 success_show/success_user_close_syn/fail 等）</returns>
    [Post("/open-apis/personal_settings/v1/system_statuses/{system_status_id}/batch_open")]
    Task<FeishuApiResult<BatchOpenSystemStatusResult>?> BatchOpenSystemStatusAsync(
        [Path] string system_status_id,
        [Body] BatchOpenSystemStatusRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 批量关闭系统状态
    /// <para>为用户批量关闭指定的系统状态，一次最多 50 个用户。</para>
    /// <para>限频：100 次/分钟。所需权限：personal_settings:status:system_status_update（获取与更新系统状态）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/personal_settings-v1/system_status/batch_close">接口文档</see></para>
    /// </summary>
    /// <param name="system_status_id">系统状态 ID，通过获取系统状态接口获取</param>
    /// <param name="request">请求体（user_list 关闭列表必填 1~50 个，每项含 user_id 与 end_time 秒级时间戳）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回逐用户的关闭结果列表（result_list：user_id/result，如 success/fail/invisible_user_id 等）</returns>
    [Post("/open-apis/personal_settings/v1/system_statuses/{system_status_id}/batch_close")]
    Task<FeishuApiResult<BatchCloseSystemStatusResult>?> BatchCloseSystemStatusAsync(
        [Path] string system_status_id,
        [Body] BatchCloseSystemStatusRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);
}
