// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroupNotice;

/// <summary>
/// 表示获取群公告结果的响应模型
/// </summary>
/// <remarks>
/// 该类包含了从飞书API获取的群公告详细信息，包括版本ID、创建时间、更新时间、所有者信息等。
/// 主要用于解析查询群公告接口返回的数据。
/// </remarks>
public class GetAnnouncementResult
{
    /// <summary>
    /// 版本ID，用于标识群公告的版本
    /// </summary>
    /// <remarks>
    /// 每次修改群公告都会生成新的版本ID，可用于版本管理和回滚操作
    /// </remarks>
    [JsonPropertyName("revision_id")]
    public int RevisionId { get; set; }

    /// <summary>
    /// 公告创建时间，使用Unix时间戳格式（毫秒）
    /// </summary>
    /// <remarks>
    /// 表示群公告首次创建的时间戳，从1970年1月1日00:00:00 UTC开始计算的毫秒数
    /// </remarks>
    [JsonPropertyName("create_time")]
    public long CreateTime { get; set; }

    /// <summary>
    /// 公告最后更新时间，使用Unix时间戳格式（毫秒）
    /// </summary>
    /// <remarks>
    /// 表示群公告最后一次修改的时间戳，从1970年1月1日00:00:00 UTC开始计算的毫秒数
    /// </remarks>
    [JsonPropertyName("update_time")]
    public long UpdateTime { get; set; }

    /// <summary>
    /// 公告创建者的ID
    /// </summary>
    /// <remarks>
    /// 表示最初创建该群公告的用户或应用的唯一标识符
    /// </remarks>
    [JsonPropertyName("owner_id")]
    public string? OwnerId { get; set; }

    /// <summary>
    /// 公告创建者的ID类型
    /// </summary>
    /// <remarks>
    /// 指示owner_id的类型，可能的值包括：
    /// - "user": 表示用户ID
    /// - "app": 表示应用ID
    /// - "open_id": 表示开放平台用户ID
    /// </remarks>
    [JsonPropertyName("owner_id_type")]
    public string? OwnerIdType { get; set; }

    /// <summary>
    /// 最后修改公告的ID
    /// </summary>
    /// <remarks>
    /// 表示最后一次修改该群公告的用户或应用的唯一标识符
    /// </remarks>
    [JsonPropertyName("modifier_id")]
    public string? ModifierId { get; set; }

    /// <summary>
    /// 最后修改者的ID类型
    /// </summary>
    /// <remarks>
    /// 指示modifier_id的类型，可能的值包括：
    /// - "user": 表示用户ID
    /// - "app": 表示应用ID
    /// - "open_id": 表示开放平台用户ID
    /// </remarks>
    [JsonPropertyName("modifier_id_type")]
    public string? ModifierIdType { get; set; }

    /// <summary>
    /// 公告类型
    /// </summary>
    /// <remarks>
    /// 指定群公告的类型，可能的值包括：
    /// - "text": 文本类型公告
    /// - "rich_text": 富文本类型公告
    /// - "image": 图片类型公告
    /// - "file": 文件类型公告
    /// </remarks>
    [JsonPropertyName("announcement_type")]
    public string? AnnouncementType { get; set; }

    /// <summary>
    /// 公告创建时间的字符串格式（备用字段）
    /// </summary>
    /// <remarks>
    /// 提供公告创建时间的可读格式，通常为ISO 8601格式的日期时间字符串
    /// 作为create_time字段的补充，提供更友好的时间表示
    /// </remarks>
    [JsonPropertyName("create_time_v2")]
    public string? CreateTimeV2 { get; set; }

    /// <summary>
    /// 公告更新时间的字符串格式（备用字段）
    /// </summary>
    /// <remarks>
    /// 提供公告更新时间的可读格式，通常为ISO 8601格式的日期时间字符串
    /// 作为update_time字段的补充，提供更友好的时间表示
    /// </remarks>
    [JsonPropertyName("update_time_v2")]
    public string? UpdateTimeV2 { get; set; }
}
