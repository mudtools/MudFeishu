// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalQuery;


/// <summary>
/// <para>审批定义信息</para>
/// </summary>
public record InstanceSearchApproval
{
    /// <summary>
    /// <para>审批定义 Code</para>
    /// <para>必填：否</para>
    /// <para>示例值：EB828003-9FFE-4B3F-AA50-2E199E2ED943</para>
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// <para>审批定义名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：approval</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>是否为第三方审批</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("is_external")]
    public bool? IsExternal { get; set; }

    /// <summary>
    /// <para>第三方审批信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("external")]
    public InstanceSearchApprovalExternal? External { get; set; }


    /// <summary>
    /// <para>审批定义 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：7090754740375519252</para>
    /// </summary>
    [JsonPropertyName("approval_id")]
    public string? ApprovalId { get; set; }

    /// <summary>
    /// <para>审批定义图标信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }
}
