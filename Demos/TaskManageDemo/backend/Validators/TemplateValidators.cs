// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentValidation;
using TaskManageDemo.Backend.Services.Templates;

namespace TaskManageDemo.Backend.Validators;

/// <summary>
/// 创建任务模板请求验证器
/// </summary>
public class CreateTaskTemplateRequestValidator : AbstractValidator<CreateTaskTemplateRequest>
{
    public CreateTaskTemplateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("模板名称不能为空")
            .MaximumLength(200).WithMessage("模板名称长度不能超过200个字符");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("模板描述长度不能超过1000个字符");

        RuleFor(x => x.DefaultSummary)
            .MaximumLength(500).WithMessage("默认任务标题长度不能超过500个字符")
            .When(x => x.DefaultSummary != null);

        RuleFor(x => x.DefaultPriority)
            .InclusiveBetween(0, 4).WithMessage("默认优先级必须在0-4之间");
    }
}

/// <summary>
/// 更新任务模板请求验证器
/// </summary>
public class UpdateTaskTemplateRequestValidator : AbstractValidator<UpdateTaskTemplateRequest>
{
    public UpdateTaskTemplateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("模板名称不能为空")
            .MaximumLength(200).WithMessage("模板名称长度不能超过200个字符")
            .When(x => x.Name != null);

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("模板描述长度不能超过1000个字符");

        RuleFor(x => x.DefaultSummary)
            .MaximumLength(500).WithMessage("默认任务标题长度不能超过500个字符")
            .When(x => x.DefaultSummary != null);
    }
}
