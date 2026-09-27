// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.PersonalSettings;

/// <summary>
/// 批量开启/关闭系统状态的用户参数（system_status_user_open_param）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "PersonalSettings")]
public class PersonalSettingsUserOpenParam
{
    /// <summary>
    /// <para>用户 ID，传入的 ID 类型由 user_id_type 决定，推荐使用 OpenID</para>
    /// <para>必填：是</para>
    /// <para>示例值：ou_53edd3282dbc2fdbe5c593cfa5ce82ab</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>结束时间，传入的应为秒单位的时间戳，距当前的时间跨度不能超过 365 天</para>
    /// <para>必填：是</para>
    /// <para>示例值：1665990378</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }
}
