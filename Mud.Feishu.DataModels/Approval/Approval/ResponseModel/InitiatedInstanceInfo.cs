// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Approval;

/// <summary>
/// 用户已发起审批列表中的审批实例信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Approval")]
public class InitiatedInstanceInfo
{
    /// <summary>
    /// <para>审批实例状态</para>
    /// </summary>
    [JsonPropertyName("instance_status")]
    public string? InstanceStatus { get; set; }

    /// <summary>
    /// <para>审批定义 Code</para>
    /// </summary>
    [JsonPropertyName("definition_code")]
    public string? DefinitionCode { get; set; }

    /// <summary>
    /// <para>发起人 ID</para>
    /// </summary>
    [JsonPropertyName("initiator")]
    public string? Initiator { get; set; }

    /// <summary>
    /// <para>发起人姓名</para>
    /// </summary>
    [JsonPropertyName("initiator_name")]
    public string? InitiatorName { get; set; }

    /// <summary>
    /// <para>审批实例 Code</para>
    /// </summary>
    [JsonPropertyName("instance_code")]
    public string? InstanceCode { get; set; }

    /// <summary>
    /// <para>审批定义分组 ID</para>
    /// </summary>
    [JsonPropertyName("definition_group_id")]
    public string? DefinitionGroupId { get; set; }

    /// <summary>
    /// <para>审批定义分组名称</para>
    /// </summary>
    [JsonPropertyName("definition_group_name")]
    public string? DefinitionGroupName { get; set; }

    /// <summary>
    /// <para>审批定义名称</para>
    /// </summary>
    [JsonPropertyName("definition_name")]
    public string? DefinitionName { get; set; }

    /// <summary>
    /// <para>摘要</para>
    /// </summary>
    [JsonPropertyName("summaries")]
    public ApprovalSummaryPairInfo[]? Summaries { get; set; }

    /// <summary>
    /// <para>三方审批实例 ID</para>
    /// </summary>
    [JsonPropertyName("instance_external_id")]
    public string? InstanceExternalId { get; set; }

    /// <summary>
    /// <para>三方审批跳转链接</para>
    /// </summary>
    [JsonPropertyName("link")]
    public string? Link { get; set; }
}
