// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Drive;

/// <summary>
/// 文件编辑
/// <para>文件编辑（包括多维表格字段和记录变更）时，将触发此事件。订阅的云文档被成功编辑后，将会触发此事件。</para>
/// <para>事件类型:drive.file.edit_v1</para>
/// <para>使用时请继承：<see cref="DriveFileReadEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/docs/drive-v1/event/list/file-edited</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.DriveFileEdit, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
public class DriveFileEditResult : IEventResult
{
    /// <summary>
    /// <para>用户的 Union ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("union_id")]
    public string? UnionId { get; set; }

    /// <summary>
    /// <para>云文档类型，支持以下枚举：</para>
    /// <para>- doc：旧版文档。已不推荐使用</para>
    /// <para>- docx：新版文档</para>
    /// <para>- sheet：电子表格</para>
    /// <para>- bitable：多维表格</para>
    /// <para>- slides：幻灯片</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("file_type")]
    public string? FileType { get; set; }

    /// <summary>
    /// <para>云文档 token</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("file_token")]
    public string? FileToken { get; set; }

    /// <summary>
    /// <para>操作人 ID 列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("operator_id_list")]
    public UserIdInfo[]? OperatorIdList { get; set; }

    /// <summary>
    /// <para>订阅用户 ID 列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("subscriber_id_list")]
    public UserIdInfo[]? SubscriberIdList { get; set; }
}