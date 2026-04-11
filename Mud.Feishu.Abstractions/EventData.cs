// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书事件 Header 数据（v2.0 版本事件的 header 部分）
/// <para>v1.0 事件无独立 header，对应字段从事件根级别解析</para>
/// </summary>
public class FeishuEventHeader : IEventHeader
{
    /// <inheritdoc />
    [JsonPropertyName("schema")]
    public string? Schema { get; set; }

    /// <inheritdoc />
    [JsonPropertyName("event_id")]
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// 事件 Token（用于验证事件来源）
    /// <para>仅 v2.0 事件包含此字段</para>
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    /// <summary>
    /// 事件创建时间戳（单位：毫秒）
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <inheritdoc />
    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = string.Empty;

    /// <inheritdoc />
    [JsonPropertyName("tenant_key")]
    public string TenantKey { get; set; } = string.Empty;

    /// <inheritdoc />
    [JsonPropertyName("app_id")]
    public string AppId { get; set; } = string.Empty;
}

/// <summary>
/// 飞书事件数据
/// </summary>
public class EventData
{
    /// <summary>
    /// 事件ID
    /// </summary>
    [JsonPropertyName("event_id")]
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// 事件类型
    /// </summary>
    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// 应用ID
    /// </summary>
    [JsonPropertyName("app_id")]
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// 租户ID
    /// </summary>
    [JsonPropertyName("tenant_key")]
    public string TenantKey { get; set; } = string.Empty;

    /// <summary>
    /// 事件创建时间（毫秒时间戳）
    /// </summary>
    [JsonPropertyName("create_time")]
    public long CreateTime { get; set; }

    /// <summary>
    /// 事件内容
    /// </summary>
    [JsonPropertyName("event")]
    public object? Event { get; set; }

    /// <summary>
    /// 事件 Header 数据（v2.0 事件的完整 header 信息）
    /// <para>v1.0 事件此属性为 null，其字段已扁平化到 EventData 根级别</para>
    /// <para>v2.0 事件此属性包含完整的 header 原始数据，包括 token 等字段</para>
    /// </summary>
    [JsonIgnore]
    public FeishuEventHeader? Header { get; set; }

    /// <summary>
    /// 事件格式版本
    /// <para>"2.0" 表示 v2.0 版本事件，null 表示 v1.0 版本</para>
    /// </summary>
    [JsonIgnore]
    public string? Schema => Header?.Schema;
}