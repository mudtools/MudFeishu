// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Application;

/// <summary>
/// 查询租户授权状态响应体数据。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class GetScopesResult
{
    /// <summary>
    /// <para>应用向租户申请授权的权限列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("scopes")]
    public List<ScopeGrantStatus>? Scopes { get; set; }
}

/// <summary>
/// 权限授权状态。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class ScopeGrantStatus
{
    /// <summary>
    /// <para>权限名称</para>
    /// <para>必填：是</para>
    /// <para>示例值：user.phone:readonly</para>
    /// </summary>
    [JsonPropertyName("scope_name")]
    public string? ScopeName { get; set; }

    /// <summary>
    /// <para>授权状态。1：已授权；0：未授权</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("grant_status")]
    public int? GrantStatus { get; set; }

    /// <summary>
    /// <para>权限的令牌类型。user：用户身份；tenant：应用身份</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("scope_type")]
    public string? ScopeType { get; set; }
}
