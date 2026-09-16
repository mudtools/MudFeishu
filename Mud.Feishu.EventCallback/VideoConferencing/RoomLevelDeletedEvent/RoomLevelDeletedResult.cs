// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.VideoConferencing;

/// <summary>
/// 会议室层级删除
/// <para>发生在删除会议室层级时【仅通过Open API预约的会议会产生此类事件】</para>
/// <para>事件类型:vc.room_level.deleted_v1</para>
/// <para>使用时请继承：<see cref="RoomLevelDeletedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/vc-v1/room_level/events/deleted</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.RoomLevelDeletedEvent, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class RoomLevelDeletedResult : IEventResult
{
    /// <summary>
    /// <para>层级ID</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`1` ～ `100` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("room_level_id")]
    public string? RoomLevelId { get; set; }

    /// <summary>
    /// <para>是否删除所有子层级</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("delete_child")]
    public bool? DeleteChild { get; set; }

}
