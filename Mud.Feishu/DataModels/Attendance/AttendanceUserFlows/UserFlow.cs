// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceUserFlows;

/// <summary>
/// 打卡流水记录
/// </summary>
public class UserFlow
{
    /// <summary>
    /// <para>用户 ID。与employee_type对应</para>
    /// <para>必填：是</para>
    /// <para>示例值：abd754f7</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// <para>记录创建者 ID。与employee_type对应</para>
    /// <para>必填：是</para>
    /// <para>示例值：abd754f7</para>
    /// </summary>
    [JsonPropertyName("creator_id")]
    public string CreatorId { get; set; } = string.Empty;

    /// <summary>
    /// <para>打卡位置名称信息</para>
    /// <para>必填：是</para>
    /// <para>示例值：西溪八方城</para>
    /// </summary>
    [JsonPropertyName("location_name")]
    public string LocationName { get; set; } = string.Empty;

    /// <summary>
    /// <para>打卡时间，精确到秒的时间戳（只支持导入打卡时间在2022年1月1日之后的数据）</para>
    /// <para>必填：是</para>
    /// <para>示例值：1611476284</para>
    /// </summary>
    [JsonPropertyName("check_time")]
    public string CheckTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>打卡备注</para>
    /// <para>必填：是</para>
    /// <para>示例值：上班打卡</para>
    /// </summary>
    [JsonPropertyName("comment")]
    public string Comment { get; set; } = string.Empty;

    /// <summary>
    /// <para>打卡记录 ID，导入时此参数无效</para>
    /// <para>必填：否</para>
    /// <para>示例值：6709359313699356941</para>
    /// </summary>
    [JsonPropertyName("record_id")]
    public string? RecordId { get; set; }

    /// <summary>
    /// <para>打卡 Wi-Fi 的 SSID</para>
    /// <para>必填：否</para>
    /// <para>示例值：b0:b8:67:5c:1d:72</para>
    /// </summary>
    [JsonPropertyName("ssid")]
    public string? Ssid { get; set; }

    /// <summary>
    /// <para>打卡 Wi-Fi 的 MAC 地址</para>
    /// <para>必填：否</para>
    /// <para>示例值：b0:b8:67:5c:1d:72</para>
    /// </summary>
    [JsonPropertyName("bssid")]
    public string? Bssid { get; set; }

    /// <summary>
    /// <para>是否为外勤打卡。默认为false，非外勤打卡</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("is_field")]
    public bool? IsField { get; set; }

    /// <summary>
    /// <para>是否为 Wi-Fi 打卡。默认为false，非Wi-Fi打卡</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("is_wifi")]
    public bool? IsWifi { get; set; }

    /// <summary>
    /// <para>记录的生成方式。举例：type=0表示「开放平台导入」的「用户打卡」流水；type=1表示「开放平台导入」的「管理员修改」流水。若不设置type，则默认是0。</para>
    /// <para>必填：否</para>
    /// <para>示例值：7</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：用户打卡</item>
    /// <item>1：管理员修改</item>
    /// <item>2：用户补卡</item>
    /// <item>3：系统自动生成</item>
    /// <item>4：下班免打卡</item>
    /// <item>5：考勤机</item>
    /// <item>6：极速打卡</item>
    /// <item>7：考勤开放平台导入</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public int? Type { get; set; }

    /// <summary>
    /// <para>打卡照片列表（该字段目前不支持）</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://time.mudtools.cn/manage/download/6840389754748502021</para>
    /// </summary>
    [JsonPropertyName("photo_urls")]
    public string[]? PhotoUrls { get; set; }

    /// <summary>
    /// <para>打卡设备ID，（只支持小程序打卡，导入时无效）</para>
    /// <para>必填：否</para>
    /// <para>示例值：99e0609ee053448596502691a81428654d7ded64c7bd85acd982d26b3636c37d</para>
    /// </summary>
    [JsonPropertyName("device_id")]
    public string? DeviceId { get; set; }

    /// <summary>
    /// <para>打卡结果，作为入参时无效</para>
    /// <para>必填：否</para>
    /// <para>示例值：Invalid</para>
    /// <para>可选值：<list type="bullet">
    /// <item>NoNeedCheck：无需打卡</item>
    /// <item>SystemCheck：系统打卡</item>
    /// <item>Normal：正常</item>
    /// <item>Early：早退</item>
    /// <item>Late：迟到</item>
    /// <item>SeriousLate：严重迟到</item>
    /// <item>Lack：缺卡</item>
    /// <item>Invalid：无效</item>
    /// <item>None：无状态</item>
    /// <item>Todo：尚未打卡</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("check_result")]
    public string? CheckResult { get; set; }

    /// <summary>
    /// <para>用户导入的外部打卡记录ID，用于和外部数据对比，如果不传，在查询的时候不方便区分</para>
    /// <para>必填：否</para>
    /// <para>示例值：record_123</para>
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// <para>唯一幂等键，不传的话无法实现幂等处理</para>
    /// <para>必填：否</para>
    /// <para>示例值：****_***</para>
    /// </summary>
    [JsonPropertyName("idempotent_id")]
    public string? IdempotentId { get; set; }
}
