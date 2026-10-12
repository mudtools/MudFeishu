// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Acs;

/// <summary>
/// 门禁用户信息变更
/// <para>订阅此事件可获取智能门禁的用户变更信息。</para>
/// <para>事件类型:acs.user.updated_v1</para>
/// <para>使用时请继承：<see cref="AcsUserUpdatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/acs-v1/user/events/updated</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.AcsUserUpdated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Acs")]
public class AcsUserUpdatedResult : IEventResult
{
    /// <summary>
    /// <para>用户 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public UserIdInfo? UserId { get; set; }

    /// <summary>
    /// <para>卡号</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("card")]
    public int? Card { get; set; }

    /// <summary>
    /// <para>是否上传人脸图片</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("face_uploaded")]
    public bool? FaceUploaded { get; set; }
}
