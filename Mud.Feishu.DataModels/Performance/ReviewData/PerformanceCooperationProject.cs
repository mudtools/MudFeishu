// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 绩效详情数据（v2）中的合作项目信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceCooperationProject
{
    /// <summary>
    /// <para>合作项目 ID</para>
    /// <para>示例值：7309457114076807188</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>合作项目的名称（下划线形态 zh_cn / en_us）</para>
    /// </summary>
    [JsonPropertyName("name")]
    public I18nName? Name { get; set; }

    /// <summary>
    /// <para>项目角色</para>
    /// </summary>
    [JsonPropertyName("roles")]
    public PerformanceCooperationRole[]? Roles { get; set; }

    /// <summary>
    /// <para>评估人项目角色</para>
    /// </summary>
    [JsonPropertyName("user_roles")]
    public PerformanceCooperationUserRole[]? UserRoles { get; set; }

    /// <summary>
    /// <para>被评估人项目角色</para>
    /// </summary>
    [JsonPropertyName("underling_roles")]
    public PerformanceCooperationUserRole[]? UnderlingRoles { get; set; }
}
