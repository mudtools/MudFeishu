// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FeishuFileServer.Models.DTOs;

namespace FeishuFileServer.Services;

/// <summary>
/// 文件夹服务接口
/// 提供文件夹的创建、更新、删除、查询和内容管理功能
/// </summary>
public interface IFolderService
{
    /// <summary>
    /// 创建文件夹
    /// <para>在飞书云盘创建文件夹，并在本地数据库创建记录</para>
    /// </summary>
    /// <param name="request">创建文件夹请求，包含文件夹名称和父文件夹令牌</param>
    /// <param name="userId">用户ID，可选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的文件夹信息</returns>
    /// <exception cref="Exception">当飞书API调用失败时抛出</exception>
    Task<FolderResponse> CreateFolderAsync(FolderCreateRequest request, int? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新文件夹
    /// <para>更新文件夹的名称</para>
    /// </summary>
    /// <param name="folderToken">文件夹令牌</param>
    /// <param name="request">更新文件夹请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的文件夹信息</returns>
    /// <exception cref="KeyNotFoundException">当文件夹不存在时抛出</exception>
    Task<FolderResponse> UpdateFolderAsync(string folderToken, FolderUpdateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除文件夹
    /// <para>从飞书云盘和本地数据库删除文件夹，同时标记子文件夹和文件为已删除</para>
    /// </summary>
    /// <param name="folderToken">文件夹令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <exception cref="KeyNotFoundException">当文件夹不存在时抛出</exception>
    Task DeleteFolderAsync(string folderToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取文件夹信息
    /// </summary>
    /// <param name="folderToken">文件夹令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件夹信息，不存在时返回 null</returns>
    Task<FolderResponse?> GetFolderAsync(string folderToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取文件夹列表
    /// <para>支持按父文件夹和用户筛选，支持分页</para>
    /// </summary>
    /// <param name="parentFolderToken">父文件夹令牌，可选</param>
    /// <param name="userId">用户ID，可选</param>
    /// <param name="page">页码，从1开始</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件夹列表响应</returns>
    Task<FolderListResponse> GetFoldersAsync(string? parentFolderToken = null, int? userId = null, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取文件夹内容
    /// <para>获取文件夹内的子文件夹和文件列表</para>
    /// </summary>
    /// <param name="folderToken">文件夹令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件夹内容响应</returns>
    Task<FolderContentsResponse> GetFolderContentsAsync(string folderToken, CancellationToken cancellationToken = default);
}
