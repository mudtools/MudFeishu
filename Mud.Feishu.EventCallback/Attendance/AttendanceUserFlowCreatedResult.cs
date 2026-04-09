// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.DataModels.Attendance;


/// <summary>
/// 用户打卡成功后，推送该用户的打卡流水消息。
/// <para>事件类型:attendance.user_flow.created_v1</para>
/// <para>使用时请继承：<see cref="AttendanceUserFlowCreatedEventHandler"/></para>
/// <para>文档地址：<see href="https://open.feishu.cn/document/server-docs/attendance-v1/event/user-attendance-records-event"/> </para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.AttendanceUserFlowCreated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom)]
public class AttendanceUserFlowCreatedResult : IEventResult
{
    /// <summary>
    /// 打卡 Wi-Fi 的 MAC 地址
    /// </summary>
    [JsonPropertyName("bssid")]
    public string? Bssid { get; set; }

    /// <summary>
    /// 打卡时间，精确到秒的时间戳
    /// </summary>
    [JsonPropertyName("check_time")]
    public string? CheckTime { get; set; }

    /// <summary>
    /// 打卡备注
    /// </summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// 飞书管理后台 > 组织架构 > 成员与部门 > 成员详情中的用户 ID
    /// </summary>
    [JsonPropertyName("employee_id")]
    public string? EmployeeId { get; set; }

    /// <summary>
    /// 飞书管理后台 > 组织架构 > 成员与部门 > 成员详情中的工号
    /// </summary>
    [JsonPropertyName("employee_no")]
    public string? EmployeeNo { get; set; }

    /// <summary>
    /// 是否为外勤打卡
    /// </summary>
    [JsonPropertyName("is_field")]
    public bool IsField { get; set; }

    /// <summary>
    /// 是否为 Wi-Fi 打卡
    /// </summary>
    [JsonPropertyName("is_wifi")]
    public bool IsWifi { get; set; }

    /// <summary>
    /// 打卡纬度。注意：目前暂不支持返回经纬度
    /// </summary>
    [JsonPropertyName("latitude")]
    public float Latitude { get; set; }

    /// <summary>
    /// 打卡位置名称信息
    /// </summary>
    [JsonPropertyName("location_name")]
    public string? LocationName { get; set; }

    /// <summary>
    /// 打卡经度。注意：目前暂不支持返回经纬度
    /// </summary>
    [JsonPropertyName("longitude")]
    public float Longitude { get; set; }

    /// <summary>
    /// 打卡照片列表
    /// </summary>
    [JsonPropertyName("photo_urls")]
    public object[]? PhotoUrls { get; set; }

    /// <summary>
    /// 打卡记录 ID
    /// </summary>
    [JsonPropertyName("record_id")]
    public string? RecordId { get; set; }

    /// <summary>
    /// 打卡 Wi-Fi 的 SSID
    /// </summary>
    [JsonPropertyName("ssid")]
    public string? Ssid { get; set; }

    /// <summary>
    /// 记录生成方式，可用值：【0（用户自己打卡），1（管理员修改），2（用户补卡），3（系统自动生成），4（下班免打卡），5（考勤机打卡），6（极速打卡），7（考勤开放平台导入）】
    /// </summary>
    [JsonPropertyName("type")]
    public int Type { get; set; }

    /// <summary>
    /// 疑似作弊打卡行为，0：无疑似作弊，1： 疑似使用作弊软件，2：疑似使用他人的设备打卡，3：疑似多人使用同一设备打卡
    /// </summary>
    [JsonPropertyName("risk_result")]
    public int RiskResult { get; set; }
}
