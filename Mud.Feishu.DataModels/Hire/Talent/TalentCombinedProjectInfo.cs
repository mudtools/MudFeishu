// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 人才项目经历（创建/更新人才请求子对象）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class TalentCombinedProjectInfo
{
    /// <summary>
    /// <para>项目经历 ID（文档标注无效字段，勿用）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>项目名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>项目角色</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    /// <summary>
    /// <para>项目链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("link")]
    public string? Link { get; set; }

    /// <summary>
    /// <para>项目描述</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("desc")]
    public string? Desc { get; set; }

    /// <summary>
    /// <para>开始时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    /// <summary>
    /// <para>结束时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }

    /// <summary>
    /// <para>自定义字段列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("customized_data")]
    public TalentCustomizedDataObjectValue[]? CustomizedData { get; set; }
}
