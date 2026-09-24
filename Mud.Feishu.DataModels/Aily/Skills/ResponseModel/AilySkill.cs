// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Aily;

/// <summary>
/// <para>技能信息</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Aily")]
public class AilySkill
{
    /// <summary>
    /// <para>技能 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：skill_8c71459001b2</para>
    /// <para>最大长度：32</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>技能名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：工作流技能</para>
    /// </summary>
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    /// <summary>
    /// <para>技能描述</para>
    /// <para>必填：否</para>
    /// <para>示例值：工作流技能</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>用户提问示例</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("samples")]
    public string[]? Samples { get; set; }

    /// <summary>
    /// <para>技能入参定义（JSON String）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("input_schema")]
    public string? InputSchema { get; set; }

    /// <summary>
    /// <para>技能出参定义（JSON String）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("output_schema")]
    public string? OutputSchema { get; set; }
}
