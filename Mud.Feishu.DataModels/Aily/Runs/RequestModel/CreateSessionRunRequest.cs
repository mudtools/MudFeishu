// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Aily;

/// <summary>
/// 创建会话运行（Run）请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Aily")]
public class CreateSessionRunRequest
{
    /// <summary>
    /// <para>应用 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：spring_xxx__c</para>
    /// <para>最大长度：64</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("app_id")]
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// <para>技能 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：skill_6cc6166178ca</para>
    /// <para>最大长度：32</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("skill_id")]
    public string? SkillId { get; set; }

    /// <summary>
    /// <para>指定技能 ID 时，可一并指定的技能输入</para>
    /// <para>必填：否</para>
    /// <para>示例值：{"key": "value"}</para>
    /// <para>最大长度：255</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("skill_input")]
    public string? SkillInput { get; set; }

    /// <summary>
    /// <para>其他透传信息</para>
    /// <para>必填：否</para>
    /// <para>示例值：{}</para>
    /// <para>最大长度：255</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("metadata")]
    public string? Metadata { get; set; }
}
