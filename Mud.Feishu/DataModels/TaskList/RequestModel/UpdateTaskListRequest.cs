// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.TaskList;

/// <summary>
///  更新任务列表请求体。
/// </summary>
public class UpdateTaskListRequest
{
    /// <summary>
    /// <para>要更新清单的数据</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("tasklist")]
    public TaskListData Tasklist { get; set; } = new();

    /// <summary>
    /// <para>要更新的字段名，支持</para>
    /// <para>必填：是</para>
    /// <para>可选值：<list type="bullet">
    /// <item>name：更新清单名</item>
    /// <item>owner：更新清单所有者</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("update_fields")]
    public string[] UpdateFields { get; set; } = [];

    /// <summary>
    /// <para>该字段表示如果更新了新的所有者，则将原所有者设为指定的新的角色。仅在更新清单所有者时生效。支持"editor", "viewer"和"none"。默认为"none"。</para>
    /// <para>如果不设置或设为"none"，原清单所有者将不具有任何清单的角色。如果没有通过其他渠道（比如通过协作群组间接授权），原清单所有者将失去对清单的所有权限。</para>
    /// <para>必填：否</para>
    /// <para>示例值：editor</para>
    /// <para>可选值：<list type="bullet">
    /// <item>editor：原所有者变为可编辑角色</item>
    /// <item>viewer：原所有者变为可阅读角色</item>
    /// <item>none：原所有者直接退出清单</item>
    /// </list></para>
    /// <para>默认值：none</para>
    /// </summary>
    [JsonPropertyName("origin_owner_to_role")]
    public string? OriginOwnerToRole { get; set; }
}
