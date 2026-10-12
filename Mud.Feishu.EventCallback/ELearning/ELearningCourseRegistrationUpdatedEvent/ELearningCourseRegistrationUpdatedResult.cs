// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.ELearning;

/// <summary>
/// 课程学习进度更新事件
/// <para>课程学习进度更新时触发</para>
/// <para>事件类型:elearning.course_registration.updated_v2</para>
/// <para>使用时请继承：<see cref="ELearningCourseRegistrationUpdatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/elearning-v2/course_registration/events/updated</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.ELearningCourseRegistrationUpdated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "ELearning")]
public class ELearningCourseRegistrationUpdatedResult : IEventResult
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

    /// <summary>
    /// <para>加入学习时间戳（秒）</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 取值范围：`0` ～ `4294967295`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("enroll_at")]
    public long? EnrollAt { get; set; }

    /// <summary>
    /// <para>注册类型</para>
    /// <para>**可选值有**：</para>
    /// <para>- 1：被管理员指派</para>
    /// <para>- 2：学员自主加入</para>
    /// <para>- 3：按照规则自动加入</para>
    /// <para>- 4：学员申请被批准加入</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("enroll_type")]
    public int? EnrollType { get; set; }

    /// <summary>
    /// <para>学习时长，单位：秒</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 取值范围：`0` ～ `4294967295`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("learning_duration")]
    public long? LearningDuration { get; set; }

    /// <summary>
    /// <para>完成时间戳（秒）</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 取值范围：`0` ～ `4294967295`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("finished_at")]
    public long? FinishedAt { get; set; }

    /// <summary>
    /// <para>完成状态</para>
    /// <para>**可选值有**：</para>
    /// <para>- 0：课程未开始</para>
    /// <para>- 1：课程学习中</para>
    /// <para>- 2：课程已通过</para>
    /// <para>- 3：课程不合格</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("learning_state")]
    public int? LearningState { get; set; }

    /// <summary>
    /// <para>必修章节id列表</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `65535`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("compulsory_lesson_ids")]
    public string[]? CompulsoryLessonIds { get; set; }

    /// <summary>
    /// <para>已完成的必修章节id列表</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `65535`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("learned_compulsory_lesson_ids")]
    public string[]? LearnedCompulsoryLessonIds { get; set; }

    /// <summary>
    /// <para>选修章节id列表</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `65535`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("optional_lesson_ids")]
    public string[]? OptionalLessonIds { get; set; }

    /// <summary>
    /// <para>已完成的选修章节id列表</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `65535`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("learned_optional_lesson_ids")]
    public string[]? LearnedOptionalLessonIds { get; set; }
}
