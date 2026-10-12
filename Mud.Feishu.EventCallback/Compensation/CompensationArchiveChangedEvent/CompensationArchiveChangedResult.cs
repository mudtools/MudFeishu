// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Compensation;

/// <summary>
/// 薪资档案变更
/// <para>当应用订阅该事件后，如果员工薪资档案发生变更（例如，通过管理后台对员工定薪、调薪、更正或删除），则会触发该事件。 你需要在应用中配置事件订阅，这样才可以在事件触发时接收到事件数据。了解事件订阅可参见事件订阅概述。</para>
/// <para>事件类型:compensation.archive.changed_v1</para>
/// <para>使用时请继承：<see cref="CompensationArchiveChangedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/compensation-v1/archive/events/changed</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CompensationArchiveChanged, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Compensation")]
public class CompensationArchiveChangedResult : IEventResult
{
    /// <summary>
    /// <para>档案变更类型。add新增、modify更正、delete删除</para>
    /// <para>**可选值有**：</para>
    /// <para>- add：新增薪酬档案</para>
    /// <para>- modify：更正薪酬档案</para>
    /// <para>- delete：删除薪酬档案</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `255` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("operate_type")]
    public string? OperateType { get; set; }

    /// <summary>
    /// <para>员工 ID。</para>
    /// <para>- 调用[【批量查询员工薪资档案】](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/compensation-v1/archive/query)接口返回员工薪资档案，需指定user_id_type为people_corehr_id来识别用户</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `255` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("employment_id")]
    public string? EmploymentId { get; set; }

    /// <summary>
    /// <para>生效时间</para>
    /// <para>- 示例值："2022-05-10"</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `255` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("effective_date")]
    public string? EffectiveDate { get; set; }

    /// <summary>
    /// <para>调前档案时间轴版本Tid。更正、删除场景下有值</para>
    /// <para>- 调用[【批量查询员工薪资档案】](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/compensation-v1/archive/query)接口返回员工薪资档案</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `255` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("before_tid")]
    public string? BeforeTid { get; set; }

    /// <summary>
    /// <para>调后档案时间轴版本Tid。新增、更正场景下有值</para>
    /// <para>- 调用[【批量查询员工薪资档案】](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/compensation-v1/archive/query)接口返回员工薪资档案</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `255` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("after_tid")]
    public string? AfterTid { get; set; }
}
