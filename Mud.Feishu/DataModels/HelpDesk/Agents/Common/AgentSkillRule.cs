// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.HelpDesk;

/// <summary>
/// <para>技能rules</para>
/// </summary>
public class AgentSkillRule
{
    /// <summary>
    /// <para>rule id, 参考[获取客服技能rules](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/agent_skill_rule/list) 用于获取rules options</para>
    /// <para>必填：否</para>
    /// <para>示例值：test-skill-id</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>运算符比较, 参考[客服技能运算符选项](https://open.feishu.cn/document/ukTMukTMukTM/ucDOyYjL3gjM24yN4IjN/operator-options)</para>
    /// <para>必填：否</para>
    /// <para>示例值：8</para>
    /// </summary>
    [JsonPropertyName("selected_operator")]
    public int? SelectedOperator { get; set; }

    /// <summary>
    /// <para>rule 操作数的值</para>
    /// <para>必填：否</para>
    /// <para>示例值：{\"selected_departments\":[{\"id\":\"部门ID\",\"name\":\"IT\"}]}</para>
    /// </summary>
    [JsonPropertyName("operand")]
    public Dictionary<string, object>? Operand { get; set; }

    /// <summary>
    /// <para>rule 类型，1-知识库，2-工单信息，3-用户飞书信息</para>
    /// <para>必填：否</para>
    /// <para>示例值：3</para>
    /// </summary>
    [JsonPropertyName("category")]
    public int? Category { get; set; }
}