// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Security;

namespace Mud.Feishu;


/// <summary>
/// 飞书安全与合规（Security）「设备管理」SDK 是一组服务端 OpenAPI 的封装，用于在设备管理中新增、查询（分页/单个）、更新、删除设备记录。本接口全部端点为 security_and_compliance/v2，仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/security_and_compliance-v1/security_and_compliance-v2/device_record/create"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Security")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV2SecurityDeviceRecord : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 新增设备
    /// <para>在设备管理中新增一台设备，新增设备的类型为管理员导入；设备特征需与操作系统匹配（如 Android 传 android_id、iOS 传 idfv、OpenHarmony 传 aaid）。</para>
    /// <para>限频：10 次/秒。所需权限：security_and_compliance:device_record:write（新增、更新、删除设备）。</para>
    /// <para><see href="https://open.feishu.cn/document/security_and_compliance-v1/security_and_compliance-v2/device_record/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（device_system 操作系统必填 1~6；device_ownership 设备归属必填 0~2；device_status 可信状态必填 0~2；serial_number/disk_serial_number/uuid/mac_address 适用于 Windows/macOS/Linux；android_id 适用于 Android；idfv 适用于 iOS；aaid 适用于 OpenHarmony）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回新增设备的设备认证编码（device_record_id）</returns>
    [Post("/open-apis/security_and_compliance/v2/device_records")]
    Task<FeishuApiResult<CreateDeviceRecordResult>?> CreateDeviceRecordAsync(
        [Body] CreateDeviceRecordRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询设备信息
    /// <para>分页查询设备列表信息，支持按设备认证编码、设备名称、各类设备特征、归属、可信状态、MDM 信息等筛选；查询参数采用查询对象模式 <see cref="ListDeviceRecordsQuery"/>，见 AGENTS.md API-2。</para>
    /// <para>限频：10 次/秒。所需权限：security_and_compliance:device_record:read（获取设备信息）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/security_and_compliance-v1/security_and_compliance-v2/device_record/list">接口文档</see></para>
    /// </summary>
    /// <param name="query">分页大小（page_size 必填，1~100，默认 100）、分页标记与设备特征等可选查询参数</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回设备分页列表（items、page_token、has_more）</returns>
    [Get("/open-apis/security_and_compliance/v2/device_records")]
    Task<FeishuApiResult<ListDeviceRecordsResult>?> ListDeviceRecordsAsync(
        [Query] ListDeviceRecordsQuery? query = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取设备信息
    /// <para>在设备管理中获取设备的设备参数、设备归属、设备状态等信息。</para>
    /// <para>限频：50 次/秒。所需权限：security_and_compliance:device_record:read（获取设备信息）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/security_and_compliance-v1/security_and_compliance-v2/device_record/get">接口文档</see></para>
    /// </summary>
    /// <param name="device_record_id">设备认证编码，通过查询设备信息接口获取</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回设备记录（device_record：设备认证编码/版本号/设备名称与型号/各类设备特征/归属/可信状态/认证方式/设备指纹/MDM 信息等）</returns>
    [Get("/open-apis/security_and_compliance/v2/device_records/{device_record_id}")]
    Task<FeishuApiResult<GetDeviceRecordResult>?> GetDeviceRecordAsync(
        [Path] string device_record_id,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新设备
    /// <para>在设备管理中修改一台设备的设备归属、设备状态等信息。</para>
    /// <para>限频：10 次/秒。所需权限：security_and_compliance:device_record:write（新增、更新、删除设备）。</para>
    /// <para><see href="https://open.feishu.cn/document/security_and_compliance-v1/security_and_compliance-v2/device_record/update">接口文档</see></para>
    /// </summary>
    /// <param name="device_record_id">设备认证编码</param>
    /// <param name="request">请求体（device_ownership 设备归属必填 0~2；device_status 可信状态必填 0~2）</param>
    /// <param name="version">版本号（必填），需与当前设备记录的版本一致</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Put("/open-apis/security_and_compliance/v2/device_records/{device_record_id}")]
    Task<FeishuNullDataApiResult?> UpdateDeviceRecordAsync(
        [Path] string device_record_id,
        [Body] UpdateDeviceRecordRequest request,
        [Query("version")] string version,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除设备
    /// <para>在设备管理中删除一台设备。</para>
    /// <para>限频：10 次/秒。所需权限：security_and_compliance:device_record:write（新增、更新、删除设备）。</para>
    /// <para><see href="https://open.feishu.cn/document/security_and_compliance-v1/security_and_compliance-v2/device_record/delete">接口文档</see></para>
    /// </summary>
    /// <param name="device_record_id">设备认证编码</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Delete("/open-apis/security_and_compliance/v2/device_records/{device_record_id}")]
    Task<FeishuNullDataApiResult?> DeleteDeviceRecordAsync(
        [Path] string device_record_id,
        CancellationToken cancellationToken = default);
}
