// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 背调附加调查项
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class EcoBackgroundCheckPackageAdditionalItem
{
    /// <summary>
    /// <para>账号下已有的附加调查项 ID（创建时由调用方自定义）</para>
    /// <para>必填：是</para>
    /// <para>示例值：ext001</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>附加调查项名称</para>
    /// <para>必填：是</para>
    /// <para>示例值：工作履历信息验证X2</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>附加调查项描述</para>
    /// <para>必填：否</para>
    /// <para>示例值：详细调查</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
