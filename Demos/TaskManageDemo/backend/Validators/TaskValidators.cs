// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentValidation;
using TaskManageDemo.Backend.Models.DTOs;

namespace TaskManageDemo.Backend.Validators;

/// <summary>
/// 创建任务请求验证器
/// </summary>
public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Summary)
            .NotEmpty().WithMessage("任务标题不能为空")
            .MaximumLength(500).WithMessage("任务标题长度不能超过500个字符");

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("任务描述长度不能超过5000个字符");

        RuleFor(x => x.DueTime)
            .Must(BeValidDueTime).WithMessage("截止时间必须晚于当前时间")
            .When(x => x.DueTime.HasValue);

        RuleFor(x => x.Priority)
            .InclusiveBetween(0, 4).WithMessage("优先级必须在0-4之间");

        RuleFor(x => x.AssigneeIds)
            .Must(HaveValidAssigneeCount).WithMessage("负责人数量不能超过10个")
            .When(x => x.AssigneeIds != null);
    }

    private static bool BeValidDueTime(DateTime? dueTime)
    {
        return dueTime > DateTime.UtcNow;
    }

    private static bool HaveValidAssigneeCount(List<string>? assigneeIds)
    {
        return assigneeIds == null || assigneeIds.Count <= 10;
    }
}

/// <summary>
/// 更新任务请求验证器
/// </summary>
public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Summary)
            .NotEmpty().WithMessage("任务标题不能为空")
            .MaximumLength(500).WithMessage("任务标题长度不能超过500个字符")
            .When(x => x.Summary != null);

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("任务描述长度不能超过5000个字符");

        RuleFor(x => x.DueTime)
            .Must(BeValidDueTime).WithMessage("截止时间必须晚于当前时间")
            .When(x => x.DueTime.HasValue);
    }

    private static bool BeValidDueTime(DateTime? dueTime)
    {
        return dueTime > DateTime.UtcNow;
    }
}

/// <summary>
/// 任务查询参数验证器
/// </summary>
public class TaskQueryParametersValidator : AbstractValidator<TaskQueryParameters>
{
    public TaskQueryParametersValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("页码必须大于等于1");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("每页数量必须在1-100之间");

        RuleFor(x => x.Keyword)
            .MaximumLength(200).WithMessage("搜索关键词长度不能超过200个字符")
            .When(x => !string.IsNullOrEmpty(x.Keyword));

        RuleFor(x => x.Priority)
            .InclusiveBetween(0, 4).WithMessage("优先级必须在0-4之间")
            .When(x => x.Priority.HasValue);
    }
}
