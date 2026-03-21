// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using TaskManageDemo.Backend.Models.DTOs;

namespace TaskManageDemo.Backend.Services.Search;

/// <summary>
/// 任务搜索服务接口
/// </summary>
public interface ITaskSearchService
{
    /// <summary>
    /// 搜索任务
    /// </summary>
    Task<PagedResponse<TaskDto>> SearchAsync(
        TaskSearchParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取搜索建议
    /// </summary>
    Task<List<string>> GetSearchSuggestionsAsync(
        string query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取高级筛选结果
    /// </summary>
    Task<PagedResponse<TaskDto>> AdvancedFilterAsync(
        AdvancedFilterParameters parameters,
        CancellationToken cancellationToken = default);
}
