// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace TaskManageDemo.Backend.Models.DTOs;

/// <summary>
/// 分页请求基类
/// </summary>
public abstract class PagedRequest
{
    /// <summary>
    /// 默认页码
    /// </summary>
    public const int DefaultPage = 1;

    /// <summary>
    /// 默认每页大小
    /// </summary>
    public const int DefaultPageSize = 20;

    /// <summary>
    /// 最小每页大小
    /// </summary>
    public const int MinPageSize = 1;

    /// <summary>
    /// 最大每页大小
    /// </summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// 最大页码
    /// </summary>
    public const int MaxPage = 10000;

    private int _page = DefaultPage;
    private int _pageSize = DefaultPageSize;

    /// <summary>
    /// 页码（从 1 开始）
    /// </summary>
    [Range(1, MaxPage, ErrorMessage = "页码必须在 1 到 {2} 之间")]
    public int Page
    {
        get => _page;
        set => _page = Math.Clamp(value, 1, MaxPage);
    }

    /// <summary>
    /// 每页大小
    /// </summary>
    [Range(MinPageSize, MaxPageSize, ErrorMessage = "每页大小必须在 {1} 到 {2} 之间")]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, MinPageSize, MaxPageSize);
    }

    /// <summary>
    /// 排序字段
    /// </summary>
    [StringLength(50, ErrorMessage = "排序字段名称不能超过 {1} 个字符")]
    public string? SortBy { get; set; }

    /// <summary>
    /// 是否降序排序
    /// </summary>
    public bool SortDescending { get; set; }

    /// <summary>
    /// 计算跳过的记录数
    /// </summary>
    public int Skip => (Page - 1) * PageSize;

    /// <summary>
    /// 获取分页参数验证结果
    /// </summary>
    public (bool IsValid, List<string> Errors) Validate()
    {
        var errors = new List<string>();

        if (Page < 1 || Page > MaxPage)
        {
            errors.Add($"页码必须在 1 到 {MaxPage} 之间");
        }

        if (PageSize < MinPageSize || PageSize > MaxPageSize)
        {
            errors.Add($"每页大小必须在 {MinPageSize} 到 {MaxPageSize} 之间");
        }

        return (errors.Count == 0, errors);
    }
}

/// <summary>
/// 任务搜索请求
/// </summary>
public class TaskSearchRequest : PagedRequest
{
    /// <summary>
    /// 搜索关键词
    /// </summary>
    [StringLength(100, ErrorMessage = "搜索关键词不能超过 {1} 个字符")]
    public string? Keyword { get; set; }

    /// <summary>
    /// 任务状态
    /// </summary>
    [RegularExpression("^(all|pending|completed)$", ErrorMessage = "状态必须是 all、pending 或 completed")]
    public string? Status { get; set; }

    /// <summary>
    /// 优先级（1-4）
    /// </summary>
    [Range(1, 4, ErrorMessage = "优先级必须在 1 到 4 之间")]
    public int? Priority { get; set; }

    /// <summary>
    /// 负责人 ID
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "负责人 ID 必须大于 0")]
    public int? AssigneeId { get; set; }

    /// <summary>
    /// 任务清单 ID
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "任务清单 ID 必须大于 0")]
    public int? TaskListId { get; set; }

    /// <summary>
    /// 截止日期开始
    /// </summary>
    public DateTime? DueDateFrom { get; set; }

    /// <summary>
    /// 截止日期结束
    /// </summary>
    public DateTime? DueDateTo { get; set; }

    /// <summary>
    /// 是否包含已完成任务
    /// </summary>
    public bool IncludeCompleted { get; set; }
}

/// <summary>
/// 用户搜索请求
/// </summary>
public class UserSearchRequest : PagedRequest
{
    /// <summary>
    /// 搜索关键词
    /// </summary>
    [StringLength(50, ErrorMessage = "搜索关键词不能超过 {1} 个字符")]
    public string? Keyword { get; set; }

    /// <summary>
    /// 部门 ID
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "部门 ID 必须大于 0")]
    public int? DepartmentId { get; set; }

    /// <summary>
    /// 是否包含子部门
    /// </summary>
    public bool IncludeChildren { get; set; }
}

/// <summary>
/// 分页响应
/// </summary>
public class PagedResponse<T>
{
    /// <summary>
    /// 数据项列表
    /// </summary>
    public List<T> Items { get; set; } = [];

    /// <summary>
    /// 总记录数
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// 当前页码
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// 每页大小
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 0;

    /// <summary>
    /// 是否有下一页
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// 是否有上一页
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// 创建空的分页响应
    /// </summary>
    public static PagedResponse<T> Empty() => new();

    /// <summary>
    /// 创建分页响应
    /// </summary>
    public static PagedResponse<T> Create(List<T> items, int total, int page, int pageSize)
    {
        return new PagedResponse<T>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
