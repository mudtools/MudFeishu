// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels;

/// <summary>
/// <para>任务信息</para>
/// </summary>
public class ExternalInstanceTask
{
    /// <summary>
    /// <para>审批实例内的审批任务 ID。自定义配置，需要确保当前企业、应用内唯一。</para>
    /// <para>**注意**：调用本接口和[同步三方审批实例]接口操作同一个三方审批实例内的任务时，需要确保所用的任务 ID 一致。</para>
    /// <para>必填：是</para>
    /// <para>示例值：112253</para>
    /// </summary>
    [JsonPropertyName("task_id")]
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// <para>任务最近更新时间，Unix 毫秒时间戳。</para>
    /// <para>必填：是</para>
    /// <para>示例值：1591603040000</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public string UpdateTime { get; set; } = string.Empty;
}