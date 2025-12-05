// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Task;

/// <summary>
/// 更新任务请求参数
/// </summary>
public class UpdateTaskRequest
{
    /// <summary>
    /// <para>要更新的任务数据，只需要设置出现在`update_fields`中的字段即可。如果`update_fields`设置了要变更一个字段名，但是`task`里没设置新的值，则表示将该字段清空。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("task")]
    public UpdateTaskData? Task { get; set; }

    /// <summary>
    /// <para>设置需要修改的字段</para>
    /// <para>必填：是</para>
    /// <para>可选值：<list type="bullet">
    /// <item>summary：任务标题</item>
    /// <item>description：任务描述</item>
    /// <item>start：任务开始时间</item>
    /// <item>due：任务截止时间</item>
    /// <item>completed_at：任务完成时间</item>
    /// <item>extra：任务附属自定义数据</item>
    /// <item>custom_complete：任务自定义完成规则</item>
    /// <item>repeat_rule：任务重复规则</item>
    /// <item>mode：任务完成模式</item>
    /// <item>is_milestone：是否是里程碑任务</item>
    /// <item>custom_fields：自定义字段值</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("update_fields")]
    public string[] UpdateFields { get; set; } = [];
}


