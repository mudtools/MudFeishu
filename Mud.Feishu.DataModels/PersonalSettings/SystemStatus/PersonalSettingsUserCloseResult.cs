// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.PersonalSettings;

/// <summary>
/// 批量关闭系统状态的单个结果（system_status_user_close_result_entity）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "PersonalSettings")]
public class PersonalSettingsUserCloseResult
{
    /// <summary>
    /// <para>用户 ID</para>
    /// <para>示例值：ou_53edd3282dbc2fdbe5c593cfa5ce82ab</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>关闭结果：success（成功）/ fail（失败）/ invisible_user_id（用户 ID 不可见）/ invalid_user_id（用户 ID 无效）/ resign_user_id（用户离职）</para>
    /// <para>示例值：success</para>
    /// </summary>
    [JsonPropertyName("result")]
    public string? Result { get; set; }
}
