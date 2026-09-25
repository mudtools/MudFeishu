// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spark;

/// <summary>
/// 申请访问配置（含审批人）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Spark")]
public class ApplyConfig
{
    /// <summary>
    /// <para>控制审批流程是否启用。设为 true 时，提交申请后将触发配置的审批流程；设为 false 时，申请将直接通过无需审批</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }

    /// <summary>
    /// <para>审批人 open_id 列表，仅支持配置一个用户 open_id</para>
    /// <para>必填：否</para>
    /// <para>示例值：["ou_75e71dde59ef1cbbaf25875b20cc54ef"]</para>
    /// </summary>
    [JsonPropertyName("approvers")]
    public string[]? Approvers { get; set; }
}
