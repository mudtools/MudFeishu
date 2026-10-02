// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;

/// <summary>
/// 会议室级别配置（会议室配置接口的查询与设置实体）
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/vc-v1/room_config/query"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class RoomLevelConfig
{
    /// <summary>
    /// <para>会议室背景图。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("room_background")]
    public string? RoomBackground { get; set; }

    /// <summary>
    /// <para>签到板背景图。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("display_background")]
    public string? DisplayBackground { get; set; }

    /// <summary>
    /// <para>数字标牌配置。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("digital_signage")]
    public RoomLevelDigitalSignage? DigitalSignage { get; set; }

    /// <summary>
    /// <para>Room 飞天盒数字标牌配置。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("room_box_digital_signage")]
    public RoomLevelDigitalSignage? RoomBoxDigitalSignage { get; set; }

    /// <summary>
    /// <para>会议室状态配置。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("room_status")]
    public RoomLevelStatus? RoomStatus { get; set; }
}

/// <summary>
/// 会议室配置中的数字标牌配置
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class RoomLevelDigitalSignage
{
    /// <summary>
    /// <para>是否覆盖子节点配置。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("if_cover_child_scope")]
    public bool? IfCoverChildScope { get; set; }

    /// <summary>
    /// <para>是否开启数字标牌。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("enable")]
    public bool? Enable { get; set; }

    /// <summary>
    /// <para>是否静音。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mute")]
    public bool? Mute { get; set; }

    /// <summary>
    /// <para>开始展示的时间（分钟，自当日 0 点起算）。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("start_display")]
    public int? StartDisplay { get; set; }

    /// <summary>
    /// <para>停止展示的时间（分钟，自当日 0 点起算）。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("stop_display")]
    public int? StopDisplay { get; set; }

    /// <summary>
    /// <para>素材列表。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("materials")]
    public RoomLevelDigitalSignageMaterial[]? Materials { get; set; }
}

/// <summary>
/// 会议室配置中的数字标牌素材
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class RoomLevelDigitalSignageMaterial
{
    /// <summary>
    /// <para>素材 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>素材名称。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>素材地址。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// <para>素材封面图地址。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("cover")]
    public string? Cover { get; set; }

    /// <summary>
    /// <para>素材的 MD5 值。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("md5")]
    public string? Md5 { get; set; }

    /// <summary>
    /// <para>素材类型。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("material_type")]
    public int? MaterialType { get; set; }

    /// <summary>
    /// <para>素材播放时长（秒）。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("duration")]
    public int? Duration { get; set; }
}

/// <summary>
/// 会议室配置中的会议室状态配置
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class RoomLevelStatus
{
    /// <summary>
    /// <para>会议室状态。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// <para>日程状态。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("schedule_status")]
    public string? ScheduleStatus { get; set; }

    /// <summary>
    /// <para>是否发送停用通知。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("disable_notice")]
    public bool? DisableNotice { get; set; }

    /// <summary>
    /// <para>是否发送恢复通知。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("resume_notice")]
    public bool? ResumeNotice { get; set; }

    /// <summary>
    /// <para>停用开始时间（秒级时间戳）。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("disable_start_time")]
    public string? DisableStartTime { get; set; }

    /// <summary>
    /// <para>停用结束时间（秒级时间戳）。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("disable_end_time")]
    public string? DisableEndTime { get; set; }

    /// <summary>
    /// <para>停用原因。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("disable_reason")]
    public string? DisableReason { get; set; }

    /// <summary>
    /// <para>联系人 ID 列表。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("contact_ids")]
    public string[]? ContactIds { get; set; }
}

/// <summary>
/// 设置会议室级别配置请求体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/vc-v1/room_config/set"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class SetRoomConfigRequest
{
    /// <summary>
    /// <para>设置配置的节点范围。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("scope")]
    public int? Scope { get; set; }

    /// <summary>
    /// <para>国家/地区 ID，scope 为 2 或 3 时必填。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("country_id")]
    public string? CountryId { get; set; }

    /// <summary>
    /// <para>城市 ID，scope 为 3 时必填。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("district_id")]
    public string? DistrictId { get; set; }

    /// <summary>
    /// <para>楼宇 ID，scope 为 4 或 5 时必填。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("building_id")]
    public string? BuildingId { get; set; }

    /// <summary>
    /// <para>楼层名称，scope 为 5 时必填。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("floor_name")]
    public string? FloorName { get; set; }

    /// <summary>
    /// <para>会议室 ID，scope 为 6 时必填。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("room_id")]
    public string? RoomId { get; set; }

    /// <summary>
    /// <para>会议室级别的配置内容。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("room_config")]
    public RoomLevelConfig? RoomConfig { get; set; }
}

/// <summary>
/// 设置签到板/会议室部署访问码请求体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/vc-v1/room_config/set_checkboard_access_code"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class SetAccessCodeRequest
{
    /// <summary>
    /// <para>设置配置的节点范围。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("scope")]
    public int? Scope { get; set; }

    /// <summary>
    /// <para>国家/地区 ID，scope 为 2 或 3 时必填。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("country_id")]
    public string? CountryId { get; set; }

    /// <summary>
    /// <para>城市 ID，scope 为 3 时必填。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("district_id")]
    public string? DistrictId { get; set; }

    /// <summary>
    /// <para>楼宇 ID，scope 为 4 或 5 时必填。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("building_id")]
    public string? BuildingId { get; set; }

    /// <summary>
    /// <para>楼层名称，scope 为 5 时必填。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("floor_name")]
    public string? FloorName { get; set; }

    /// <summary>
    /// <para>会议室 ID，scope 为 6 时必填。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("room_id")]
    public string? RoomId { get; set; }

    /// <summary>
    /// <para>部署访问码的有效天数。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("valid_day")]
    public int? ValidDay { get; set; }
}

/// <summary>
/// 设置部署访问码响应体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/vc-v1/room_config/set_room_access_code"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class SetAccessCodeResult
{
    /// <summary>
    /// <para>部署访问码。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("access_code")]
    public string? AccessCode { get; set; }
}
