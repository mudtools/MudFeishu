// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Bitable;

/// <summary>
/// 多维表格字段变更
/// <para>当用户有新文档记录变更操作会触发此事件。</para>
/// <para>事件类型:drive.file.bitable_field_changed_v1</para>
/// <para>使用时请继承：<see cref="BitableFieldChangedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/docs/drive-v1/event/list/bitable_field_changed</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.BitableFieldChanged, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
public class BitableFieldChangedResult : IEventResult
{
    /// <summary>
    /// <para>云文档类型</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("file_type")]
    public string? FileType { get; set; }

    /// <summary>
    /// <para>多维表格 token</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("file_token")]
    public string? FileToken { get; set; }

    /// <summary>
    /// <para>多维表格数据表 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("table_id")]
    public string? TableId { get; set; }

    /// <summary>
    /// <para>用户 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("operator_id")]
    public UserIdInfo? OperatorId { get; set; }

    /// <summary>
    /// <para>字段变更操作类型列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("action_list")]
    public BitableTableFieldAction[]? ActionList { get; set; }


    /// <summary>
    /// <para>多维表格数据表的版本号</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("revision")]
    public int? Revision { get; set; }

    /// <summary>
    /// <para>订阅用户 ID 列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("subscriber_id_list")]
    public UserIdInfo[]? SubscriberIdList { get; set; }

    /// <summary>
    /// <para>字段变更时间</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public int? UpdateTime { get; set; }
}