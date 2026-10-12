// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Helpdesk;

/// <summary>
/// 工单更新前信息，仅包含发生变更的字段。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Helpdesk")]
public class HelpdeskTicketEventUpdateInfo
{
    /// <summary>
    /// <para>工单阶段。枚举值有：</para>
    /// <para>- 1：机器人</para>
    /// <para>- 2：人工</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("stage")]
    public int? Stage { get; set; }

    /// <summary>
    /// <para>工单状态。枚举值有：</para>
    /// <para>- 1：已创建</para>
    /// <para>- 2：处理中</para>
    /// <para>- 3：排队中</para>
    /// <para>- 4：待处理</para>
    /// <para>- 5：待用户回复</para>
    /// <para>- 50：机器人关闭</para>
    /// <para>- 51：客服关闭</para>
    /// <para>- 52：用户自己关闭</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// <para>工单更新时间</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("updated_at")]
    public long? UpdatedAt { get; set; }
}
