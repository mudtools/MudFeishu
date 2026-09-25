// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Acs;

/// <summary>
/// 智能门禁权限组信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Acs")]
public class AcsRule
{
    /// <summary>
    /// <para>权限组 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：34252345234523</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>权限组名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：南门</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>权限组包含的设备</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("devices")]
    public AcsExternalDevice[]? Devices { get; set; }

    /// <summary>
    /// <para>权限组包含的员工个数</para>
    /// <para>必填：否</para>
    /// <para>示例值：3</para>
    /// </summary>
    [JsonPropertyName("user_count")]
    public string? UserCount { get; set; }

    /// <summary>
    /// <para>权限组包含的员工列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("users")]
    public AcsExternalUser[]? Users { get; set; }

    /// <summary>
    /// <para>权限组包含的访客个数</para>
    /// <para>必填：否</para>
    /// <para>示例值：3</para>
    /// </summary>
    [JsonPropertyName("visitor_count")]
    public string? VisitorCount { get; set; }

    /// <summary>
    /// <para>权限组包含的访客列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("visitors")]
    public AcsExternalUser[]? Visitors { get; set; }

    /// <summary>
    /// <para>是否通知人员录入人脸</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("remind_face")]
    public bool? RemindFace { get; set; }

    /// <summary>
    /// <para>开门时间段</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("opening_time")]
    public AcsRuleOpeningTime? OpeningTime { get; set; }

    /// <summary>
    /// <para>是否为临时权限组</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("is_temp")]
    public bool? IsTemp { get; set; }
}
