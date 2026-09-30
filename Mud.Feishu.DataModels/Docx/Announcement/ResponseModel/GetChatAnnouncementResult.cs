// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Docx;

/// <summary>
/// 获取群公告基本信息响应体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Docx")]
public class GetChatAnnouncementResult
{
    /// <summary>
    /// <para>群公告当前版本号</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("revision_id")]
    public int? RevisionId { get; set; }

    /// <summary>
    /// <para>群公告生成的时间戳（秒）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1609296809</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public int? CreateTime { get; set; }

    /// <summary>
    /// <para>群公告更新的时间戳（秒）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1609296809</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public int? UpdateTime { get; set; }

    /// <summary>
    /// <para>群公告所有者 ID，ID 值与 owner_id_type 中的 ID 类型对应</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_7d8a6e6df7621556ce0d21922b676706ccs</para>
    /// </summary>
    [JsonPropertyName("owner_id")]
    public string? OwnerId { get; set; }

    /// <summary>
    /// <para>群公告所有者的 ID 类型</para>
    /// <para>必填：否</para>
    /// <para>可选值：<list type="bullet">
    /// <item>user_id：标识一个用户在某个租户内的身份。同一个用户在租户 A 和租户 B 内的 User ID 是不同的。在同一个租户内，一个用户的 User ID 在所有应用（包括商店应用）中都保持一致。</item>
    /// <item>union_id：标识一个用户在某个应用开发商下的身份。同一用户在同一开发商下的应用中的 Union ID 是相同的，在不同开发商下的应用中的 Union ID 是不同的。</item>
    /// <item>open_id：标识一个用户在某个应用中的身份。同一个用户在不同应用中的 Open ID 不同。</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("owner_id_type")]
    public string? OwnerIdType { get; set; }

    /// <summary>
    /// <para>群公告最新修改者 ID，ID 值与 modifier_id_type 中的 ID 类型对应</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_7d8a6e6df7621556ce0d21922b676706ccs</para>
    /// </summary>
    [JsonPropertyName("modifier_id")]
    public string? ModifierId { get; set; }

    /// <summary>
    /// <para>群公告最新修改者 ID 类型</para>
    /// <para>必填：否</para>
    /// <para>可选值：<list type="bullet">
    /// <item>user_id：标识一个用户在某个租户内的身份。同一个用户在租户 A 和租户 B 内的 User ID 是不同的。在同一个租户内，一个用户的 User ID 在所有应用（包括商店应用）中都保持一致。</item>
    /// <item>union_id：标识一个用户在某个应用开发商下的身份。同一用户在同一开发商下的应用中的 Union ID 是相同的，在不同开发商下的应用中的 Union ID 是不同的。</item>
    /// <item>open_id：标识一个用户在某个应用中的身份。同一个用户在不同应用中的 Open ID 不同。</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("modifier_id_type")]
    public string? ModifierIdType { get; set; }

    /// <summary>
    /// <para>群公告类型</para>
    /// <para>必填：否</para>
    /// <para>可选值：<list type="bullet">
    /// <item>docx：新版本群公告</item>
    /// <item>doc：旧版本群公告</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("announcement_type")]
    public string? AnnouncementType { get; set; }

    /// <summary>
    /// <para>群公告生成的时间戳（秒）（该字段暂未提供使用）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("create_time_v2")]
    public string? CreateTimeV2 { get; set; }

    /// <summary>
    /// <para>群公告更新的时间戳（秒）（该字段暂未提供使用）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("update_time_v2")]
    public string? UpdateTimeV2 { get; set; }
}
