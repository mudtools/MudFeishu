// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Security;

/// <summary>
/// 行为审计日志的通用扩展信息项（api_audit_drawer_info）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Security")]
public class SecurityAuditDrawerInfo
{
    /// <summary>
    /// <para>信息键</para>
    /// <para>示例值：CCM_op_status</para>
    /// </summary>
    [JsonPropertyName("info_key")]
    public string? InfoKey { get; set; }

    /// <summary>
    /// <para>信息值</para>
    /// <para>示例值：success</para>
    /// </summary>
    [JsonPropertyName("info_val")]
    public string? InfoVal { get; set; }

    /// <summary>
    /// <para>信息键的国际化标识</para>
    /// </summary>
    [JsonPropertyName("key_i18n_key")]
    public string? KeyI18nKey { get; set; }

    /// <summary>
    /// <para>信息值的国际化标识</para>
    /// </summary>
    [JsonPropertyName("val_i18n_key")]
    public string? ValI18nKey { get; set; }

    /// <summary>
    /// <para>信息值类型</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("val_type")]
    public string? ValType { get; set; }
}
