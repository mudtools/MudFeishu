// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Security;

namespace Mud.Feishu;


/// <summary>
/// 飞书安全与合规（Security）「设备申报」SDK 用于在设备管理中审批成员自主申报的设备申请（通过或驳回）。本接口为 security_and_compliance/v2 端点，仅支持 tenant_access_token 调用。
/// <para><see href="https://open.feishu.cn/document/security_and_compliance-v1/security_and_compliance-v2/device_apply_record/update">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Security")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV2SecurityDeviceApplyRecord : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 审批设备申报
    /// <para>在设备管理中通过或驳回一条成员自主申报申请。</para>
    /// <para>限频：10 次/秒。所需权限：security_and_compliance:device_apply_record:write（审核自主申报申请）。</para>
    /// <para><see href="https://open.feishu.cn/document/security_and_compliance-v1/security_and_compliance-v2/device_apply_record/update">接口文档</see></para>
    /// </summary>
    /// <param name="device_apply_record_id">设备申报记录 ID，示例值：7088763625288187923</param>
    /// <param name="request">请求体（is_approved 是否审批通过，必填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Put("/open-apis/security_and_compliance/v2/device_apply_records/{device_apply_record_id}")]
    Task<FeishuNullDataApiResult?> UpdateDeviceApplyRecordAsync(
        [Path] string device_apply_record_id,
        [Body] UpdateDeviceApplyRecordRequest request,
        CancellationToken cancellationToken = default);
}
