// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using TaskManageDemo.Backend.Data;
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Models.Entities;
using TaskManageDemo.Backend.Services.Feishu;

namespace TaskManageDemo.Backend.Services.Templates;

/// <summary>
/// 任务模板服务实现
/// </summary>
public class TaskTemplateService : ITaskTemplateService
{
    private readonly TaskManageDbContext _dbContext;
    private readonly IFeishuTaskService _taskService;
    private readonly ILogger<TaskTemplateService> _logger;

    /// <summary>
    /// 初始化任务模板服务
    /// </summary>
    public TaskTemplateService(
        TaskManageDbContext dbContext,
        IFeishuTaskService taskService,
        ILogger<TaskTemplateService> logger)
    {
        _dbContext = dbContext;
        _taskService = taskService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有模板
    /// </summary>
    public async Task<List<TaskTemplateDto>> GetAllTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var templates = await _dbContext.TaskTemplates
            .Where(t => t.IsPublic)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        return templates.Select(MapToDto).ToList();
    }

    /// <summary>
    /// 获取模板详情
    /// </summary>
    public async Task<TaskTemplateDto?> GetTemplateByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var template = await _dbContext.TaskTemplates.FindAsync([id], cancellationToken);
        return template == null ? null : MapToDto(template);
    }

    /// <summary>
    /// 创建模板
    /// </summary>
    public async Task<TaskTemplateDto> CreateTemplateAsync(CreateTaskTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var template = new TaskTemplate
        {
            Name = request.Name,
            Description = request.Description,
            DefaultSummary = request.DefaultSummary,
            DefaultDescription = request.DefaultDescription,
            DefaultPriority = request.DefaultPriority ?? 0,
            DefaultDueDays = request.DefaultDueDays,
            CheckItems = request.CheckItems != null
                ? System.Text.Json.JsonSerializer.Serialize(request.CheckItems)
                : null,
            IsPublic = request.IsPublic,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.TaskTemplates.Add(template);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("创建任务模板: {Name}", template.Name);
        return MapToDto(template);
    }

    /// <summary>
    /// 更新模板
    /// </summary>
    public async Task<TaskTemplateDto?> UpdateTemplateAsync(int id, UpdateTaskTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var template = await _dbContext.TaskTemplates.FindAsync([id], cancellationToken);
        if (template == null) return null;

        if (request.Name != null) template.Name = request.Name;
        if (request.Description != null) template.Description = request.Description;
        if (request.DefaultSummary != null) template.DefaultSummary = request.DefaultSummary;
        if (request.DefaultDescription != null) template.DefaultDescription = request.DefaultDescription;
        if (request.DefaultPriority.HasValue) template.DefaultPriority = request.DefaultPriority.Value;
        if (request.DefaultDueDays.HasValue) template.DefaultDueDays = request.DefaultDueDays;
        if (request.CheckItems != null)
            template.CheckItems = System.Text.Json.JsonSerializer.Serialize(request.CheckItems);
        if (request.IsPublic.HasValue) template.IsPublic = request.IsPublic.Value;

        template.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("更新任务模板: {Id}", id);
        return MapToDto(template);
    }

    /// <summary>
    /// 删除模板
    /// </summary>
    public async Task<bool> DeleteTemplateAsync(int id, CancellationToken cancellationToken = default)
    {
        var template = await _dbContext.TaskTemplates.FindAsync([id], cancellationToken);
        if (template == null) return false;

        _dbContext.TaskTemplates.Remove(template);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("删除任务模板: {Id}", id);
        return true;
    }

    /// <summary>
    /// 从模板创建任务
    /// </summary>
    public async Task<TaskDto?> CreateTaskFromTemplateAsync(int templateId, CreateTaskFromTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var template = await _dbContext.TaskTemplates.FindAsync([templateId], cancellationToken);
        if (template == null)
        {
            _logger.LogWarning("模板不存在: {Id}", templateId);
            return null;
        }

        var summary = ApplyTemplateVariables(
            request.SummaryOverride ?? template.DefaultSummary ?? string.Empty,
            request.Variables);

        var description = ApplyTemplateVariables(
            request.DescriptionOverride ?? template.DefaultDescription ?? string.Empty,
            request.Variables);

        var dueTime = request.DueTimeOverride ??
                      (template.DefaultDueDays.HasValue
                          ? DateTime.UtcNow.AddDays(template.DefaultDueDays.Value)
                          : null);

        var assigneeIds = request.AssigneeIdsOverride;

        var taskGuid = await _taskService.CreateTaskAsync(
            summary,
            description,
            assigneeIds,
            dueTime,
            null,
            cancellationToken);

        if (string.IsNullOrEmpty(taskGuid))
        {
            _logger.LogWarning("从模板创建任务失败: {TemplateId}", templateId);
            return null;
        }

        var task = new Models.Entities.TaskSync
        {
            TaskGuid = taskGuid,
            Summary = summary,
            Description = description,
            Priority = template.DefaultPriority,
            DueTime = dueTime,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("从模板创建任务成功: Template={TemplateId}, Task={TaskGuid}", templateId, taskGuid);

        return new TaskDto
        {
            Id = task.Id,
            TaskGuid = task.TaskGuid,
            Summary = task.Summary,
            Description = task.Description,
            Priority = task.Priority,
            DueTime = task.DueTime,
            CreatedAt = task.CreatedAt
        };
    }

    private static string ApplyTemplateVariables(string template, Dictionary<string, string>? variables)
    {
        if (variables == null || string.IsNullOrEmpty(template)) return template;

        var result = template;
        foreach (var (key, value) in variables)
        {
            result = result.Replace($"{{{key}}}", value);
        }

        return result;
    }

    private static TaskTemplateDto MapToDto(TaskTemplate template)
    {
        return new TaskTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            DefaultSummary = template.DefaultSummary,
            DefaultDescription = template.DefaultDescription,
            DefaultPriority = template.DefaultPriority,
            DefaultDueDays = template.DefaultDueDays,
            IsPublic = template.IsPublic,
            CreatedAt = template.CreatedAt
        };
    }
}
