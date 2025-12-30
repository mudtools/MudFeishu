// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Task;

namespace Mud.Feishu.DataModels.TaskSections;

/// <summary>
/// <para>自定义分组数据</para>
/// </summary>
public class TaskSectionsInfo
{
    /// <summary>
    /// <para>自定义分组的guid</para>
    /// <para>示例值：e6e37dcc-f75a-5936-f589-12fb4b5c80c2</para>
    /// </summary>
    [JsonPropertyName("guid")]
    public string? Guid { get; set; }

    /// <summary>
    /// <para>自定义分组的名字</para>
    /// <para>示例值：已经评审过的任务</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>资源类型</para>
    /// <para>示例值：tasklist</para>
    /// </summary>
    [JsonPropertyName("resource_type")]
    public string? ResourceType { get; set; }

    /// <summary>
    /// <para>分组是否为默认自定义分组</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("is_default")]
    public bool? IsDefault { get; set; }

    /// <summary>
    /// <para>自定义分组的创建者</para>
    /// </summary>
    [JsonPropertyName("creator")]
    public TaskMember? Creator { get; set; }

    /// <summary>
    /// <para>如果该分组归属于清单，展示清单的简要信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("tasklist")]
    public TaskListSummaryInfo? Tasklist { get; set; }

    /// <summary>
    /// <para>自定义分组创建时间戳(ms)</para>
    /// <para>必填：否</para>
    /// <para>示例值：1675742789470</para>
    /// </summary>
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    /// <summary>
    /// <para>自定义分组最近一次更新时间戳(ms)</para>
    /// <para>必填：否</para>
    /// <para>示例值：1675742789470</para>
    /// </summary>
    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }
}
