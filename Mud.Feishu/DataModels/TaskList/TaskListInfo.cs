// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Task;

namespace Mud.Feishu.DataModels.TaskList;

/// <summary>
/// 创建的清单数据
/// </summary>
public class TaskListInfo
{
    /// <summary>
    /// <para>清单的全局唯一ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：cc371766-6584-cf50-a222-c22cd9055004</para>
    /// </summary>
    [JsonPropertyName("guid")]
    public string? Guid { get; set; }

    /// <summary>
    /// <para>清单名</para>
    /// <para>必填：否</para>
    /// <para>示例值：年会总结工作任务清单</para>
    /// <para>最大长度：300</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>清单创建者</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("creator")]
    public TaskMember? Creator { get; set; }

    /// <summary>
    /// <para>清单所有者</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("owner")]
    public TaskMember? Owner { get; set; }

    /// <summary>
    /// <para>清单成员</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("members")]
    public TaskMember[]? Members { get; set; }

    /// <summary>
    /// <para>该清单分享的applink</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://applink.feishu.cn/client/todo/task_list?guid=b45b360f-1961-4058-b338-7f50c96e1b52</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// <para>清单创建时间戳(ms)</para>
    /// <para>必填：否</para>
    /// <para>示例值：1675742789470</para>
    /// </summary>
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    /// <summary>
    /// <para>清单最后一次更新时间戳（ms)</para>
    /// <para>必填：否</para>
    /// <para>示例值：1675742789470</para>
    /// </summary>
    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }
}
