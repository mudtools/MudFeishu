// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;

/// <summary>
/// <para>告警记录</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class AlertInfo
{
    /// <summary>
    /// <para>告警ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：7115030004018184212</para>
    /// </summary>
    [JsonPropertyName("alert_id")]
    public string? AlertId { get; set; }

    /// <summary>
    /// <para>触发告警规则的会议室/服务器具体的名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：XX层级</para>
    /// </summary>
    [JsonPropertyName("resource_scope")]
    public string? ResourceScope { get; set; }

    /// <summary>
    /// <para>触发告警规则的监控对象</para>
    /// <para>必填：否</para>
    /// <para>示例值：2</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：飞书会议室</item>
    /// <item>2：飞书会议室签到板</item>
    /// <item>3：飞书投屏盒子</item>
    /// <item>4：飞书投屏</item>
    /// <item>5：sip会议室系统</item>
    /// <item>6：erc节点</item>
    /// <item>7：飞书传感器</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("monitor_target")]
    public int? MonitorTarget { get; set; }

    /// <summary>
    /// <para>告警规则的规则描述</para>
    /// <para>必填：否</para>
    /// <para>示例值：连续1个周期（共1分钟），控制器电量 &lt; 50%，则告警</para>
    /// </summary>
    [JsonPropertyName("alert_strategy")]
    public string? AlertStrategy { get; set; }

    /// <summary>
    /// <para>告警通知发生时间（unix时间，单位秒）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1656914944</para>
    /// </summary>
    [JsonPropertyName("alert_time")]
    public string? AlertTime { get; set; }

    /// <summary>
    /// <para>告警等级：严重/警告/提醒</para>
    /// <para>必填：否</para>
    /// <para>示例值：2</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：提醒</item>
    /// <item>1：警告</item>
    /// <item>2：严重</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("alert_level")]
    public int? AlertLevel { get; set; }

    /// <summary>
    /// <para>告警联系人</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("contacts")]
    public AlertContact[]? Contacts { get; set; }

    /// <summary>
    /// <para>通知方式</para>
    /// <para>必填：否</para>
    /// <para>示例值：[0,1]</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：飞书机器人</item>
    /// <item>1：邮件</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("notifyMethods")]
    public int[]? NotifyMethods { get; set; }

    /// <summary>
    /// <para>规则名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：签到板断开连接</para>
    /// </summary>
    [JsonPropertyName("alertRule")]
    public string? AlertRule { get; set; }

    /// <summary>
    /// <para>处理时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：1656914944</para>
    /// </summary>
    [JsonPropertyName("process_time")]
    public string? ProcessTime { get; set; }

    /// <summary>
    /// <para>恢复时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：1656914944</para>
    /// </summary>
    [JsonPropertyName("recover_time")]
    public string? RecoverTime { get; set; }

    /// <summary>
    /// <para>处理状态：待处理/处理中/已恢复</para>
    /// <para>必填：否</para>
    /// <para>示例值：2</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：待处理（deprecated）</item>
    /// <item>1：待处理</item>
    /// <item>2：处理中</item>
    /// <item>3：已恢复（deprecated）</item>
    /// <item>4：已恢复</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("process_status")]
    public int? ProcessStatus { get; set; }

    /// <summary>
    /// <para>告警规则ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：100</para>
    /// </summary>
    [JsonPropertyName("alert_rule_id")]
    public string? AlertRuleId { get; set; }

    /// <summary>
    /// <para>触发告警规则的会议室ID，当触发告警规则的是会议室时返回该信息</para>
    /// <para>必填：否</para>
    /// <para>示例值：omm_4de32cf10a4358788ff4e09e37ebbf9b</para>
    /// </summary>
    [JsonPropertyName("monitor_target_room_id")]
    public string? MonitorTargetRoomId { get; set; }

    /// <summary>
    /// <para>触发告警规则的会议室主机Mac地址，当monitor_target=1时返回该信息</para>
    /// <para>必填：否</para>
    /// <para>示例值：52:60:19:9c:97:21</para>
    /// </summary>
    [JsonPropertyName("monitor_target_room_mac")]
    public string? MonitorTargetRoomMac { get; set; }
}
