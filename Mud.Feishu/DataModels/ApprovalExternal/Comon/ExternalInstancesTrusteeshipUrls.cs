// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalExternal;

/// <summary>
/// <para>单据托管回调接入方的接口 URL 地址。</para>
/// </summary>
public record ExternalInstancesTrusteeshipUrls
{
    /// <summary>
    /// <para>获取表单 schema 相关数据的 URL 地址。</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://#{your_domain}/api/form_detail</para>
    /// </summary>
    [JsonPropertyName("form_detail_url")]
    public string? FormDetailUrl { get; set; }

    /// <summary>
    /// <para>表示获取审批操作区数据的 URL 地址。</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://#{your_domain}/api/action_definition</para>
    /// </summary>
    [JsonPropertyName("action_definition_url")]
    public string? ActionDefinitionUrl { get; set; }

    /// <summary>
    /// <para>获取审批记录相关数据的 URL 地址。</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://#{your_domain}/api/approval_node</para>
    /// </summary>
    [JsonPropertyName("approval_node_url")]
    public string? ApprovalNodeUrl { get; set; }

    /// <summary>
    /// <para>进行审批操作时回调的 URL 地址。</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://#{your_domain}/api/action_callback</para>
    /// </summary>
    [JsonPropertyName("action_callback_url")]
    public string? ActionCallbackUrl { get; set; }

    /// <summary>
    /// <para>获取托管动态数据 URL 地址。使用该接口时，必须要保证历史托管单据的数据中都同步了该接口地址。如果历史单据中没有该接口，需要重新同步历史托管单据的数据来更新该 URL。该接口用于飞书审批前端和业务进行交互使用，只有使用审批前端的特定组件（由飞书审批前端提供的组件，并且需要和业务进行接口交互的组件）才会需要。</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://#{your_domain}/api/pull_business_data</para>
    /// </summary>
    [JsonPropertyName("pull_business_data_url")]
    public string? PullBusinessDataUrl { get; set; }
}