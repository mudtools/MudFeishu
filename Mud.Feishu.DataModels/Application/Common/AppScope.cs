// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Application;

/// <summary>
/// 应用已申请的权限（scope）信息。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppScope
{
    /// <summary>
    /// <para>权限名称</para>
    /// <para>必填：是</para>
    /// <para>示例值：contact:user.base:readonly</para>
    /// </summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    /// <summary>
    /// <para>权限描述</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>权限等级。1：低风险；2：中风险；3：高风险</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("level")]
    public int? Level { get; set; }

    /// <summary>
    /// <para>权限支持的令牌类型。user：用户身份；tenant：应用身份</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("token_types")]
    public List<string>? TokenTypes { get; set; }
}
