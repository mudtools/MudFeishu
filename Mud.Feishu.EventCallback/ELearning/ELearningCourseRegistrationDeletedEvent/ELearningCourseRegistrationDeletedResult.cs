// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.ELearning;

/// <summary>
/// 课程学习进度删除事件
/// <para>课程学习进度删除时触发</para>
/// <para>事件类型:elearning.course_registration.deleted_v2</para>
/// <para>使用时请继承：<see cref="ELearningCourseRegistrationDeletedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/elearning-v2/course_registration/events/deleted</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.ELearningCourseRegistrationDeleted, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "ELearning")]
public class ELearningCourseRegistrationDeletedResult : IEventResult
{
    /// <summary>
    /// <para>课程ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("course_id")]
    public string? CourseId { get; set; }

    /// <summary>
    /// <para>课程学员信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("learner")]
    public ELearningEventUser? Learner { get; set; }
}
