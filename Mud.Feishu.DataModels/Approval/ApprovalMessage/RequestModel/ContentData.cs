// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalMessage;

/// <summary>
/// 审批 Bot 消息的内容。当模板的内容存在 {user_id}、{department_id} 或 {summaries} 等参数时，可以通过当前参数配置对应的参数值。
/// </summary>
public class ContentData
{
    /// <summary>
    /// 审批申请人 ID。
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// 审批申请人 ID 的类型。
    /// </summary>
    [JsonPropertyName("user_id_type")]
    public string? UserIdType { get; set; }

    /// <summary>
    /// 审批申请人的名称。
    /// </summary>
    [JsonPropertyName("user_name")]
    public string? UserName { get; set; }


    /// <summary>
    /// 审批申请人所属部门的 ID。
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }

    /// <summary>
    /// 审批申请人所属的部门名称。
    /// </summary>
    [JsonPropertyName("department_name")]
    public string? DepartmenName { get; set; }

    /// <summary>
    /// 审批事由。最多可传入 5 个。
    /// </summary>
    [JsonPropertyName("summaries")]
    public List<SummaryItem> Summaries { get; set; } = [];
}