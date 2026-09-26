// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.MDM;

/// <summary>
/// 用户数据维度解绑请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "MDM")]
public class UnbindUserAuthDataRelationRequest
{
    /// <summary>
    /// <para>数据类型编码</para>
    /// <para>必填：是</para>
    /// <para>示例值：gongsi</para>
    /// </summary>
    [JsonPropertyName("root_dimension_type")]
    public string? RootDimensionType { get; set; }

    /// <summary>
    /// <para>数据编码列表（1~200 个）</para>
    /// <para>必填：是</para>
    /// <para>示例值：["zijie"]</para>
    /// </summary>
    [JsonPropertyName("sub_dimension_types")]
    public string[]? SubDimensionTypes { get; set; }

    /// <summary>
    /// <para>授权人的 lark id（1~200 个）</para>
    /// <para>必填：是</para>
    /// <para>示例值：["on_21f2db9bdbafadeb16cd77b76060d41d"]</para>
    /// </summary>
    [JsonPropertyName("authorized_user_ids")]
    public string[]? AuthorizedUserIds { get; set; }

    /// <summary>
    /// <para>uams 系统中应用 id</para>
    /// <para>必填：是</para>
    /// <para>示例值：uams-tenant-test</para>
    /// </summary>
    [JsonPropertyName("uams_app_id")]
    public string? UamsAppId { get; set; }
}
