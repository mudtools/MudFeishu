// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceGroups;

/// <summary>
/// <para>考勤组信息</para>
/// </summary>
public class AttendanceGroupsInfo
{
    /// <summary>
    /// <para>考勤组 ID。</para>
    /// <para>必填：否</para>
    /// <para>示例值：6919358128597097404</para>
    /// </summary>
    [JsonPropertyName("group_id")]
    public string? GroupId { get; set; }

    /// <summary>
    /// <para>考勤组名称</para>
    /// <para>必填：是</para>
    /// <para>示例值：开心考勤</para>
    /// </summary>
    [JsonPropertyName("group_name")]
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// <para>时区</para>
    /// <para>必填：是</para>
    /// <para>示例值：Asia/Shanghai</para>
    /// </summary>
    [JsonPropertyName("time_zone")]
    public string TimeZone { get; set; } = string.Empty;

    /// <summary>
    /// <para>绑定的部门 ID（与「need_punch_members」同时使用时，以当前字段为准）。对应dept_type</para>
    /// <para>必填：否</para>
    /// <para>示例值：od-fcb45c28a45311afd440b7869541fce8</para>
    /// </summary>
    [JsonPropertyName("bind_dept_ids")]
    public string[]? BindDeptIds { get; set; }

    /// <summary>
    /// <para>排除的部门 ID（该字段已下线）</para>
    /// <para>必填：否</para>
    /// <para>示例值：od-fcb45c28a45311afd440b7869541fce8</para>
    /// </summary>
    [JsonPropertyName("except_dept_ids")]
    public string[]? ExceptDeptIds { get; set; }

    /// <summary>
    /// <para>绑定的用户 ID（与「need_punch_members」同时使用时，以当前字段为准），对应employee_type</para>
    /// <para>必填：否</para>
    /// <para>示例值：52aa1fa1</para>
    /// </summary>
    [JsonPropertyName("bind_user_ids")]
    public string[]? BindUserIds { get; set; }

    /// <summary>
    /// <para>排除的用户 ID（该字段已下线）</para>
    /// <para>必填：否</para>
    /// <para>示例值：52aa1fa1</para>
    /// </summary>
    [JsonPropertyName("except_user_ids")]
    public string[]? ExceptUserIds { get; set; }

    /// <summary>
    /// <para>考勤主负责人 ID 列表，必选字段（需至少拥有考勤组管理员权限），对应employee_type</para>
    /// <para>必填：是</para>
    /// <para>示例值：2bg4a9be</para>
    /// </summary>
    [JsonPropertyName("group_leader_ids")]
    public string[] GroupLeaderIds { get; set; } = [];

    /// <summary>
    /// <para>考勤子负责人 ID 列表，对应employee_type</para>
    /// <para>必填：否</para>
    /// <para>示例值：52aa1fa1</para>
    /// </summary>
    [JsonPropertyName("sub_group_leader_ids")]
    public string[]? SubGroupLeaderIds { get; set; }

    /// <summary>
    /// <para>是否允许外勤打卡</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("allow_out_punch")]
    public bool? AllowOutPunch { get; set; }

    /// <summary>
    /// <para>外勤打卡需审批（需要允许外勤打卡才能设置生效）</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("out_punch_need_approval")]
    public bool? OutPunchNeedApproval { get; set; }

    /// <summary>
    /// <para>外勤打卡需审批，先打卡后审批（需要允许外勤打卡才能设置生效）</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("out_punch_need_post_approval")]
    public bool? OutPunchNeedPostApproval { get; set; }

    /// <summary>
    /// <para>外勤打卡需填写备注（需要允许外勤打卡才能设置生效）</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("out_punch_need_remark")]
    public bool? OutPunchNeedRemark { get; set; }

    /// <summary>
    /// <para>外勤打卡需拍照（需要允许外勤打卡才能设置生效）</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("out_punch_need_photo")]
    public bool? OutPunchNeedPhoto { get; set; }

    /// <summary>
    /// <para>外勤打卡允许员工隐藏详细地址（需要允许外勤打卡才能设置生效）</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("out_punch_allowed_hide_addr")]
    public bool? OutPunchAllowedHideAddr { get; set; }

    /// <summary>
    /// <para>外勤打卡允许微调地址（需要允许外勤打卡才能设置生效）</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("out_punch_allowed_adjust_addr")]
    public bool? OutPunchAllowedAdjustAddr { get; set; }

    /// <summary>
    /// <para>微调范围，默认为 50 米</para>
    /// <para>必填：否</para>
    /// <para>示例值：50</para>
    /// </summary>
    [JsonPropertyName("adjust_range")]
    public int? AdjustRange { get; set; }

    /// <summary>
    /// <para>是否允许 PC 端打卡</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("allow_pc_punch")]
    public bool? AllowPcPunch { get; set; }

    /// <summary>
    /// <para>是否限制补卡</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("allow_remedy")]
    public bool? AllowRemedy { get; set; }

    /// <summary>
    /// <para>是否限制补卡次数</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("remedy_limit")]
    public bool? RemedyLimit { get; set; }

    /// <summary>
    /// <para>补卡次数</para>
    /// <para>必填：否</para>
    /// <para>示例值：3</para>
    /// </summary>
    [JsonPropertyName("remedy_limit_count")]
    public int? RemedyLimitCount { get; set; }

    /// <summary>
    /// <para>是否限制补卡时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("remedy_date_limit")]
    public bool? RemedyDateLimit { get; set; }

    /// <summary>
    /// <para>补卡时间，几天内补卡</para>
    /// <para>必填：否</para>
    /// <para>示例值：3</para>
    /// </summary>
    [JsonPropertyName("remedy_date_num")]
    public int? RemedyDateNum { get; set; }

    /// <summary>
    /// <para>允许缺卡补卡（需要允许补卡才能设置生效）</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("allow_remedy_type_lack")]
    public bool? AllowRemedyTypeLack { get; set; }

    /// <summary>
    /// <para>允许迟到补卡（需要允许补卡才能设置生效）</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("allow_remedy_type_late")]
    public bool? AllowRemedyTypeLate { get; set; }

    /// <summary>
    /// <para>允许早退补卡（需要允许补卡才能设置生效）</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("allow_remedy_type_early")]
    public bool? AllowRemedyTypeEarly { get; set; }

    /// <summary>
    /// <para>允许正常补卡（需要允许补卡才能设置生效）</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("allow_remedy_type_normal")]
    public bool? AllowRemedyTypeNormal { get; set; }

    /// <summary>
    /// <para>是否展示累计时长</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("show_cumulative_time")]
    public bool? ShowCumulativeTime { get; set; }

    /// <summary>
    /// <para>是否展示加班时长</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("show_over_time")]
    public bool? ShowOverTime { get; set; }

    /// <summary>
    /// <para>是否隐藏员工打卡详情</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("hide_staff_punch_time")]
    public bool? HideStaffPunchTime { get; set; }

    /// <summary>
    /// <para>是否隐藏打卡规则（仅灰度租户有效，如需使用请联系技术支持）</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("hide_clock_in_rule")]
    public bool? HideClockInRule { get; set; }

    /// <summary>
    /// <para>是否开启人脸识别打卡</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("face_punch")]
    public bool? FacePunch { get; set; }

    /// <summary>
    /// <para>人脸识别打卡规则</para>
    /// <para>**可选值有：**</para>
    /// <para>* 1：每次打卡均需人脸识别</para>
    /// <para>* 2：疑似作弊打卡时需要人脸识别</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("face_punch_cfg")]
    public int? FacePunchCfg { get; set; }

    /// <summary>
    /// <para>人脸打卡规则， false：开启活体验证 true：0动作验证，仅在 face_punch_cfg = 1 时有效</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("face_live_need_action")]
    public bool? FaceLiveNeedAction { get; set; }

    /// <summary>
    /// <para>人脸识别失败时是否允许普通拍照打卡</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("face_downgrade")]
    public bool? FaceDowngrade { get; set; }

    /// <summary>
    /// <para>人脸识别失败时是否允许替换基准图片</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("replace_basic_pic")]
    public bool? ReplaceBasicPic { get; set; }

    /// <summary>
    /// <para>防作弊打卡配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("anti_cheat_punch_config")]
    public AttendanceAntiCheatConfig? AntiCheatPunchConfig { get; set; }

    /// <summary>
    /// <para>考勤机列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("machines")]
    public AttendanceMachine[]? Machines { get; set; }

    /// <summary>
    /// <para>GPS 打卡的有效范围（不建议使用）</para>
    /// <para>必填：否</para>
    /// <para>示例值：300</para>
    /// </summary>
    [JsonPropertyName("gps_range")]
    public int? GpsRange { get; set; }

    /// <summary>
    /// <para>地址列表（仅追加，不会覆盖之前的列表）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("locations")]
    public AttendanceLocationInfo[]? Locations { get; set; }


    /// <summary>
    /// <para>考勤类型</para>
    /// <para>**可选值有：**</para>
    /// <para>* 0：固定班制</para>
    /// <para>* 2：排班制</para>
    /// <para>* 3：自由班制</para>
    /// <para>必填：是</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("group_type")]
    public int GroupType { get; set; }

    /// <summary>
    /// <para>固定班制返回.</para>
    /// <para>必填：是</para>
    /// <para>示例值：6921319402260496386</para>
    /// </summary>
    [JsonPropertyName("punch_day_shift_ids")]
    public string[] PunchDayShiftIds { get; set; } = [];

    /// <summary>
    /// <para>配置自由班制</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("free_punch_cfg")]
    public AttendanceGroupFreePunchCfg? FreePunchCfg { get; set; }


    /// <summary>
    /// <para>国家日历 ID</para>
    /// <para>**可选值有：**</para>
    /// <para>* 0：不根据国家日历排休</para>
    /// <para>* 1：中国大陆</para>
    /// <para>* 2：美国</para>
    /// <para>* 3：日本</para>
    /// <para>* 4：印度</para>
    /// <para>* 5：新加坡</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("calendar_id")]
    public int CalendarId { get; set; }

    /// <summary>
    /// <para>必须打卡的特殊日期</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("need_punch_special_days")]
    public PunchSpecialDateShift[]? NeedPunchSpecialDays { get; set; }

    /// <summary>
    /// <para>无需打卡的特殊日期</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("no_need_punch_special_days")]
    public PunchSpecialDateShift[]? NoNeedPunchSpecialDays { get; set; }

    /// <summary>
    /// <para>自由班制下工作日不打卡是否记为缺卡</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("work_day_no_punch_as_lack")]
    public bool? WorkDayNoPunchAsLack { get; set; }

    /// <summary>
    /// <para>是否立即生效，默认 false</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("effect_now")]
    public bool? EffectNow { get; set; }

    /// <summary>
    /// <para>补卡周期类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("remedy_period_type")]
    public int? RemedyPeriodType { get; set; }

    /// <summary>
    /// <para>补卡自定义周期起始日期</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("remedy_period_custom_date")]
    public int? RemedyPeriodCustomDate { get; set; }

    /// <summary>
    /// <para>打卡类型。</para>
    /// <para>位运算，即如果设置了 1 和 2 两种打卡类型，则对应的参数加和值为 3。</para>
    /// <para>**可选值**：</para>
    /// <para>* 1：GPS 打卡</para>
    /// <para>* 2：Wi-Fi 打卡</para>
    /// <para>* 4：考勤机打卡</para>
    /// <para>* 8：IP 打卡</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("punch_type")]
    public int? PunchType { get; set; }

    /// <summary>
    /// <para>生效时间，精确到秒的时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1611476284</para>
    /// </summary>
    [JsonPropertyName("effect_time")]
    public string? EffectTime { get; set; }

    /// <summary>
    /// <para>固定班次生效时间，精确到秒的时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1611476284</para>
    /// </summary>
    [JsonPropertyName("fixshift_effect_time")]
    public string? FixshiftEffectTime { get; set; }

    /// <summary>
    /// <para>参加考勤的人员、部门变动生效时间，精确到秒的时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1611476284</para>
    /// </summary>
    [JsonPropertyName("member_effect_time")]
    public string? MemberEffectTime { get; set; }

    /// <summary>
    /// <para>休息日打卡需审批</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("rest_clockIn_need_approval")]
    public bool? RestClockInNeedApproval { get; set; }

    /// <summary>
    /// <para>每次打卡均需拍照</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("clockIn_need_photo")]
    public bool? ClockInNeedPhoto { get; set; }

    /// <summary>
    /// <para>人员异动打卡设置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("member_status_change")]
    public AttendanceGroupMemberStatusChange? MemberStatusChange { get; set; }

    /// <summary>
    /// <para>请假离岗或返岗是否需打卡</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("leave_need_punch")]
    public bool? LeaveNeedPunch { get; set; }

    /// <summary>
    /// <para>请假离岗或返岗打卡规则，单位：分钟</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("leave_need_punch_cfg")]
    public AttendanceGroupLeaveNeedPunchCfg? LeaveNeedPunchCfg { get; set; }

    /// <summary>
    /// <para>外出期间是否需打卡</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("go_out_need_punch")]
    public int? GoOutNeedPunch { get; set; }

    /// <summary>
    /// <para>外出期间打卡规则，单位：分钟</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("go_out_need_punch_cfg")]
    public LeaveNeedPunchCfgSuffix? GoOutNeedPunchCfg { get; set; }


    /// <summary>
    /// <para>出差期间是否需打卡</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("travel_need_punch")]
    public int? TravelNeedPunch { get; set; }

    /// <summary>
    /// <para>出差期间打卡规则，单位：分钟</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("travel_need_punch_cfg")]
    public LeaveNeedPunchCfgSuffix? TravelNeedPunchCfg { get; set; }

    /// <summary>
    /// <para>需要打卡的人员集合（仅当不传「bind_dept_ids」和「bind_user_ids」时，才会使用该字段）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("need_punch_members")]
    public AttendancePunchMember[]? NeedPunchMembers { get; set; }


    /// <summary>
    /// <para>无需打卡的人员集合（仅当不传「bind_default_dept_ids」和「bind_default_user_ids」时，才会使用该字段）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("no_need_punch_members")]
    public AttendancePunchMember[]? NoNeedPunchMembers { get; set; }

    /// <summary>
    /// <para>是否允许保存有冲突人员的考勤组。如果 true，则冲突人员将被自动拉入到当前设置的考勤组中，并从原考勤组中移除；如果 false，则需手动调整冲突人员。默认为 false。</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("save_auto_changes")]
    public bool? SaveAutoChanges { get; set; }

    /// <summary>
    /// <para>当有新员工入职或人员异动，符合条件的人员是否自动加入考勤组</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("org_change_auto_adjust")]
    public bool? OrgChangeAutoAdjust { get; set; }

    /// <summary>
    /// <para>参与无需打卡的部门 ID 列表（与「no_need_punch_members」同时使用时，以当前字段为准），对应dept_type</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("bind_default_dept_ids")]
    public string[]? BindDefaultDeptIds { get; set; }

    /// <summary>
    /// <para>参与无需打卡的人员 ID 列表（与「no_need_punch_members」同时使用时，以当前字段为准），对应employee_type</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("bind_default_user_ids")]
    public string[]? BindDefaultUserIds { get; set; }

    /// <summary>
    /// <para>加班打卡规则</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("overtime_clock_cfg")]
    public AttendanceGroupOvertimeClockCfg? OvertimeClockCfg { get; set; }

    /// <summary>
    /// <para>节假日id，（如果考勤组使用了自定义节假日，请用此参数传入节假日id，可在假勤设置-节假日模块页面路径获取）</para>
    /// <para>必填：否</para>
    /// <para>示例值：7302191700771358252</para>
    /// </summary>
    [JsonPropertyName("new_calendar_id")]
    public string? NewCalendarId { get; set; }

    /// <summary>
    /// <para>定位不准时是否允许申请打卡</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("allow_apply_punch")]
    public bool? AllowApplyPunch { get; set; }

    /// <summary>
    /// <para>异常卡豁免配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("clock_in_abnormal_settings")]
    public AttendanceGroupClockInAbnormalSettings? ClockInAbnormalSettings { get; set; }
}