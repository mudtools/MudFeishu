// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 面试得分（面试评价子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class InterviewScore
{
    /// <summary>
    /// <para>得分 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>得分等级</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("level")]
    public int? Level { get; set; }

    /// <summary>
    /// <para>得分中文名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("zh_name")]
    public string? ZhName { get; set; }

    /// <summary>
    /// <para>得分中文描述</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("zh_description")]
    public string? ZhDescription { get; set; }

    /// <summary>
    /// <para>得分英文名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("en_name")]
    public string? EnName { get; set; }

    /// <summary>
    /// <para>得分英文描述</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("en_description")]
    public string? EnDescription { get; set; }
}
