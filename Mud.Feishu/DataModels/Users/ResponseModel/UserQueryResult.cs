// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 用户查询结果，包含手机号或者邮箱对应的用户 ID 信息。
/// </summary>
public class UserQueryResult
{
    /// <summary>
    /// 自定义用户的 user_id。长度不能超过 64 字符。
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// 邮箱。
    /// <para>当设置非中国大陆的手机号时，必须同时设置邮箱。</para>
    /// <para>在当前租户下，邮箱不可重复。</para>
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// 手机号。
    /// </summary>
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    /// <summary>
    /// 用户状态信息。
    /// </summary>
    [JsonPropertyName("status")]
    public UserStatus? Status { get; set; }
}
