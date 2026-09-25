// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.TrustParty;

/// <summary>
/// 关联组织用户状态
/// </summary>
[HttpJsonSerializable(SerializerClassName = "TrustParty")]
public class UserStatus
{
    /// <summary>
    /// 是否冻结
    /// </summary>
    [JsonPropertyName("is_frozen")]
    public bool? IsFrozen { get; set; }

    /// <summary>
    /// 是否离职
    /// </summary>
    [JsonPropertyName("is_resigned")]
    public bool? IsResigned { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    [JsonPropertyName("is_activated")]
    public bool? IsActivated { get; set; }

    /// <summary>
    /// 是否主动退出，主动退出一段时间后用户会自动转为已离职
    /// </summary>
    [JsonPropertyName("is_exited")]
    public bool? IsExited { get; set; }

    /// <summary>
    /// 是否未加入，需要用户自主确认才能加入团队
    /// </summary>
    [JsonPropertyName("is_unjoin")]
    public bool? IsUnjoin { get; set; }
}
