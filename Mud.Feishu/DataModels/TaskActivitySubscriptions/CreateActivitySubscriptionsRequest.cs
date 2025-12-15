// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.TaskActivitySubscriptions;

/// <summary>
/// 创建动态订阅请求体
/// </summary>
public class CreateActivitySubscriptionsRequest
{
    /// <summary>
    /// <para>订阅名称，不能为空，最大50个字符。</para>
    /// <para>必填：是</para>
    /// <para>示例值：我的订阅</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>订阅者列表</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("subscribers")]
    public TaskListMember[] Subscribers { get; set; } = [];

    /// <summary>
    /// <para>订阅的event key列表。每个event key用一个数字表示。目前支持下列event key：</para>
    /// <para>- 100: 任务添加入清单</para>
    /// <para>- 101: 任务从清单被移除</para>
    /// <para>- 103: 任务被完成</para>
    /// <para>- 104: 任务恢复为未完成</para>
    /// <para>- 109: 任务添加了负责人</para>
    /// <para>- 110: 任务更新了负责人</para>
    /// <para>- 111: 任务移除了负责人</para>
    /// <para>- 119: 任务添加了附件</para>
    /// <para>- 121: 任务中添加了新评论</para>
    /// <para>- 122: 任务中对评论进行回复</para>
    /// <para>- 129: 任务设置了新的开始时间</para>
    /// <para>- 130: 任务设置了新的截止时间</para>
    /// <para>- 131: 任务同时设置了新的开始/截止时间</para>
    /// <para>- 132: 任务同时移除了开始/截止时间</para>
    /// <para>该字段可以设置为空数组（即不对任何event进行通知）；输入的`include_keys`的元素不能重复。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("include_keys")]
    public int[] IncludeKeys { get; set; } = [];

    /// <summary>
    /// <para>该订阅是否为停用</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("disabled")]
    public bool? Disabled { get; set; }
}
