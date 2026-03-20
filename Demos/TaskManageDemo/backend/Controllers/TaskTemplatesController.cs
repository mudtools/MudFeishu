// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Services.Templates;

namespace TaskManageDemo.Backend.Controllers;

/// <summary>
/// 任务模板控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TaskTemplatesController : ControllerBase
{
    private readonly ITaskTemplateService _templateService;
    private readonly ILogger<TaskTemplatesController> _logger;

    /// <summary>
    /// 初始化任务模板控制器
    /// </summary>
    public TaskTemplatesController(
        ITaskTemplateService templateService,
        ILogger<TaskTemplatesController> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有模板
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<TaskTemplateDto>>>> GetAllTemplates(CancellationToken cancellationToken)
    {
        var templates = await _templateService.GetAllTemplatesAsync(cancellationToken);
        return ApiResponse<List<TaskTemplateDto>>.Ok(templates);
    }

    /// <summary>
    /// 获取模板详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<TaskTemplateDto>>> GetTemplate(int id, CancellationToken cancellationToken)
    {
        var template = await _templateService.GetTemplateByIdAsync(id, cancellationToken);
        if (template == null)
        {
            return ApiResponse<TaskTemplateDto>.Fail("模板不存在");
        }

        return ApiResponse<TaskTemplateDto>.Ok(template);
    }

    /// <summary>
    /// 创建模板
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<TaskTemplateDto>>> CreateTemplate(
        [FromBody] CreateTaskTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var template = await _templateService.CreateTemplateAsync(request, cancellationToken);
        return ApiResponse<TaskTemplateDto>.Ok(template, "模板创建成功");
    }

    /// <summary>
    /// 更新模板
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<TaskTemplateDto>>> UpdateTemplate(
        int id,
        [FromBody] UpdateTaskTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var template = await _templateService.UpdateTemplateAsync(id, request, cancellationToken);
        if (template == null)
        {
            return ApiResponse<TaskTemplateDto>.Fail("模板不存在");
        }

        return ApiResponse<TaskTemplateDto>.Ok(template, "模板更新成功");
    }

    /// <summary>
    /// 删除模板
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTemplate(int id, CancellationToken cancellationToken)
    {
        var success = await _templateService.DeleteTemplateAsync(id, cancellationToken);
        if (!success)
        {
            return ApiResponse<bool>.Fail("模板不存在");
        }

        return ApiResponse<bool>.Ok(true, "模板删除成功");
    }

    /// <summary>
    /// 从模板创建任务
    /// </summary>
    [HttpPost("{id}/tasks")]
    public async Task<ActionResult<ApiResponse<TaskDto>>> CreateTaskFromTemplate(
        int id,
        [FromBody] CreateTaskFromTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var task = await _templateService.CreateTaskFromTemplateAsync(id, request, cancellationToken);
        if (task == null)
        {
            return ApiResponse<TaskDto>.Fail("从模板创建任务失败");
        }

        return ApiResponse<TaskDto>.Ok(task, "任务创建成功");
    }
}
