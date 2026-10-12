// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHrV2;

/// <summary>
/// CoreHrV2OnboardingTaskChange
/// <para>入职任务状态变更</para>
/// <para>**数据校验规则**：</para>
/// <para>- 长度范围：`0` ～ `1000`</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "CoreHrV2")]
public class CoreHrV2OnboardingTaskChange
{
    /// <summary>
    /// <para>变更后任务状态</para>
    /// <para>**可选值有**：</para>
    /// <para>- uninitialized：未初始化（内部状态无需关注）</para>
    /// <para>- not_started：未开始</para>
    /// <para>- in_progress：进行中</para>
    /// <para>- in_review：审核中/审批中</para>
    /// <para>- rejected：已拒绝</para>
    /// <para>- failed：失败</para>
    /// <para>- skipped：自动跳过</para>
    /// <para>- completed：已完成</para>
    /// <para>- terminated：已终止</para>
    /// <para>- initiating：初始化中</para>
    /// <para>- exception：异常</para>
    /// <para>- manual_skipped：手动跳过</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("after_status")]
    public string? AfterStatus { get; set; }

    /// <summary>
    /// <para>任务标识码</para>
    /// <para>如果是系统内置的任务，标识码与名称对应关系如下：</para>
    /// <para>其中『创建账户SSO』为隐藏的任务节点，在『个人信息』前自动执行</para>
    /// <para>| 名称 | task code |</para>
    /// <para>| ---- | --- |</para>
    /// <para>| 职位信息 | 1 |</para>
    /// <para>| 个人信息 | 2 |</para>
    /// <para>| 创建账户SSO | 3 |</para>
    /// <para>| 签到 | 4 |</para>
    /// <para>| 签署入职文件 | 9 |</para>
    /// <para>如果标识码不在上面，说明是自定义任务节点，如：3095697a-065f-4627-a47c-46fe958a6754，名称的获取方式如下：</para>
    /// <para>- 通过pre_hire_id调用[搜索待入职人员信息](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/pre_hire/search)接口或[查询待入职](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/pre_hire/query)</para>
    /// <para>- 查询字段fields中添加onboarding_info.onboarding_task_list</para>
    /// <para>- 查询后返回的onboarding_task_list结构体中则包含code和名字的对应关系，示例如下</para>
    /// <para>```</para>
    /// <para>{</para>
    /// <para>"code": 0,</para>
    /// <para>"data": {</para>
    /// <para>"has_more": false,</para>
    /// <para>"items": [</para>
    /// <para>{</para>
    /// <para>"onboarding_info": {</para>
    /// <para>"onboarding_task_list": [</para>
    /// <para>{</para>
    /// <para>"task_code": "2",</para>
    /// <para>"task_name": "填写个人信息",</para>
    /// <para>"task_status": "in_progress"</para>
    /// <para>},</para>
    /// <para>{</para>
    /// <para>"task_code": "3095697a-065f-4627-a47c-46fe958a6754",</para>
    /// <para>"task_name": "修改入职日期",</para>
    /// <para>"task_status": "uninitialized"</para>
    /// <para>},</para>
    /// <para>{</para>
    /// <para>"task_code": "d37b9d7c-232d-4a55-98fa-541318234ede",</para>
    /// <para>"task_name": "工签补充任务",</para>
    /// <para>"task_status": "uninitialized"</para>
    /// <para>}</para>
    /// <para>]</para>
    /// <para>},</para>
    /// <para>"pre_hire_id": "7186491930107561516"</para>
    /// <para>}</para>
    /// <para>]</para>
    /// <para>},</para>
    /// <para>"msg": "success"</para>
    /// <para>}</para>
    /// <para>```</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("task_code")]
    public string? TaskCode { get; set; }
}
