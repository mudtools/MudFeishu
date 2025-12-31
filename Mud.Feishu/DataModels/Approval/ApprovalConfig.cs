// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Approval;

/// <summary>
/// <para>审批定义配置项，用于配置对应审批定义是否可以由用户在[审批后台]进行修改。</para>
/// </summary>
public class ApprovalConfig
{
    /// <summary>
    /// <para>是否允许用户修改可见范围</para>
    /// <para>**默认值**：false</para>
    /// <para>必填：是</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("can_update_viewer")]
    public bool CanUpdateViewer { get; set; }

    /// <summary>
    /// <para>是否允许用户更新表单</para>
    /// <para>**默认值**：false</para>
    /// <para>必填：是</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("can_update_form")]
    public bool CanUpdateForm { get; set; }

    /// <summary>
    /// <para>是否允许用户更新流程定义</para>
    /// <para>**默认值**：false</para>
    /// <para>必填：是</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("can_update_process")]
    public bool CanUpdateProcess { get; set; }

    /// <summary>
    /// <para>是否允许用户更新撤回设置</para>
    /// <para>**默认值**：false</para>
    /// <para>必填：是</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("can_update_revert")]
    public bool CanUpdateRevert { get; set; }

    /// <summary>
    /// <para>审批定义的帮助文档链接</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://www.mudtools.cn/help</para>
    /// </summary>
    [JsonPropertyName("help_url")]
    public string? HelpUrl { get; set; }
}
