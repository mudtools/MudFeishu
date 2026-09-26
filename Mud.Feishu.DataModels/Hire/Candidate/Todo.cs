// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 招聘待办事项信息（按 type 返回对应类别的待办）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class Todo
{
    /// <summary>
    /// <para>简历评估待办，仅当 type = evaluation 时返回</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("evaluation")]
    public TodoCommon? Evaluation { get; set; }

    /// <summary>
    /// <para>Offer 待办，仅当 type = offer 时返回</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("offer")]
    public TodoCommon? Offer { get; set; }

    /// <summary>
    /// <para>笔试待办，仅当 type = exam 时返回</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("exam")]
    public TodoCommon? Exam { get; set; }

    /// <summary>
    /// <para>面试待办，仅当 type = interview 时返回</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("interview")]
    public TodoCommon? Interview { get; set; }
}
