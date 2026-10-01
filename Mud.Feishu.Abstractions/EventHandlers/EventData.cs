// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书事件 Header 数据（v2.0 版本事件的 header 部分）
/// <para>v1.0 事件无独立 header，SDK 会构造<b>合成 Header</b>：字段取自根级 uuid/token/ts 与 event.*，<see cref="Schema"/> 恒为 null（R-E1/AD-2）</para>
/// </summary>
public class FeishuEventHeader : IEventHeader
{
    /// <inheritdoc />
    /// <remarks>v1.0 事件的合成 Header 中此属性恒为 null——请以 <c>Schema == null</c> 判别 v1.0 格式。</remarks>
    [JsonPropertyName("schema")]
    public string? Schema { get; set; }

    /// <inheritdoc />
    [JsonPropertyName("event_id")]
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// 事件 Token（用于验证事件来源）
    /// <para>v2.0 事件取自 header.token；v1.0 事件由 SDK 从根级 token 解析（R-E1/E-P1-1）</para>
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    /// <summary>
    /// 事件创建时间戳（原始字符串：v2.0 为 header.create_time 原文；v1.0 为根级 ts 原文）
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
    /// 事件创建时间（毫秒时间戳；R-E1/E-P2-2 统一口径，跨格式跨通道可比）
    /// </summary>
    [JsonPropertyName("create_time")]
    public long CreateTime { get; set; }

    /// <summary>
    /// 事件内容（JSON 原文）
    /// <para>SDK 生产路径恒写入 JSON 字符串；<c>object?</c> 仅为源兼容保留（历史 WS 通道曾写入 JsonElement），
    /// 读取请使用 <see cref="FeishuEventDataExtensions.GetEventRawJson"/>，下一 major 收敛为 <c>string?</c></para>
    /// </summary>
    [JsonPropertyName("event")]
    public object? Event { get; set; }

    /// <summary>
    /// 事件 Header 数据
    /// <para>v2.0 事件为完整 header；v1.0 事件为 SDK 构造的合成 Header（字段取自根级 uuid/token/ts 与 event.*，
    /// <see cref="FeishuEventHeader.Schema"/> 恒为 null，R-E1/AD-2）。判定 v1.0 请用 <c>Schema == null</c>，
    /// 不要用 <c>Header == null</c>（自本版本起 v1.0 的 Header 不再为 null）</para>
    /// </summary>
    [JsonIgnore]
    public FeishuEventHeader? Header { get; set; }

    /// <summary>
    /// 事件格式版本
    /// <para>"2.0" 表示 v2.0 版本事件，null 表示 v1.0 版本</para>
    /// </summary>
    [JsonIgnore]
    public string? Schema => Header?.Schema;

    /// <summary>
    /// 运行时上下文项袋，用于在拦截器与处理器之间传递非序列化辅助对象（如 Activity）。
    /// </summary>
    [JsonIgnore]
    public Dictionary<string, object?>? Items { get; set; }
}