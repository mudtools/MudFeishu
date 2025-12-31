// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Approval;

/// <summary>
/// 查看指定审批定义响应体
/// </summary>
public class GetApprovalResult
{
    /// <summary>
    /// <para>审批名称</para>
    /// <para>必填：是</para>
    /// <para>示例值：Payment</para>
    /// </summary>
    [JsonPropertyName("approval_name")]
    public string ApprovalName { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批定义状态</para>
    /// <para>示例值：ACTIVE</para>
    /// <para>可选值：<list type="bullet">
    /// <item>ACTIVE：已启用</item>
    /// <item>INACTIVE：已停用</item>
    /// <item>DELETED：已删除</item>
    /// <item>UNKNOWN：未知</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// <para>控件参数信息，见下方 **form 字段说明** 章节。</para>
    /// <para>示例值：[{\"id\": \"widget1\", \"custom_id\": \"user_name\",\"name\": \"Item application\",\"type\": \"textarea\",\"printable\": true,\"required\": true}]</para>
    /// </summary>
    [JsonPropertyName("form")]
    public string Form { get; set; } = string.Empty;

    /// <summary>
    /// <para>节点信息</para>
    /// </summary>
    [JsonPropertyName("node_list")]
    public ApprovalNodeInfo[] NodeLists { get; set; } = [];

    /// <summary>
    /// <para>审批定义的可见人列表</para>
    /// </summary>
    [JsonPropertyName("viewers")]
    public ApprovalViewerInfo[] Viewers { get; set; } = [];


    /// <summary>
    /// <para>有数据管理权限的审批流程管理员的 open_id，由参数 with_admin_id 控制是否返回。</para>
    /// </summary>
    [JsonPropertyName("approval_admin_ids")]
    public string[]? ApprovalAdminIds { get; set; }
}
