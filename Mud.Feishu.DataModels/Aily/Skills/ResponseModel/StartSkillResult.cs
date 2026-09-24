// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Aily;

/// <summary>
/// <para>调用技能（Start Skill）结果</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Aily")]
public class StartSkillResult
{
    /// <summary>
    /// <para>技能的输出（JSON String）；按开发者在 Workflow 技能「结束节点」配置的响应参数进行输出</para>
    /// <para>必填：否</para>
    /// <para>示例值：{"message_status":true,"input_message":""}</para>
    /// </summary>
    [JsonPropertyName("output")]
    public string? Output { get; set; }

    /// <summary>
    /// <para>技能的执行状态</para>
    /// <para>必填：否</para>
    /// <para>示例值：success</para>
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
