// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentValidation;
using TaskManageDemo.Backend.Models.DTOs;

namespace TaskManageDemo.Backend.Validators;

/// <summary>
/// 创建任务清单请求验证器
/// </summary>
public class CreateTaskListRequestValidator : AbstractValidator<CreateTaskListRequestDto>
{
    public CreateTaskListRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("清单名称不能为空")
            .MaximumLength(200).WithMessage("清单名称长度不能超过200个字符");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("清单描述长度不能超过1000个字符");
    }
}

/// <summary>
/// 更新任务清单请求验证器
/// </summary>
public class UpdateTaskListRequestValidator : AbstractValidator<UpdateTaskListRequestDto>
{
    public UpdateTaskListRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("清单名称不能为空")
            .MaximumLength(200).WithMessage("清单名称长度不能超过200个字符")
            .When(x => x.Name != null);

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("清单描述长度不能超过1000个字符");
    }
}
