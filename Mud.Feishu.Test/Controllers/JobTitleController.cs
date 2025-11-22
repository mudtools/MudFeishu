// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;

namespace Mud.Feishu.Test.Controllers;

/// <summary>
/// 飞书职务管理控制器
/// 用于测试职务相关的API接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class JobTitleController : ControllerBase
{
    private readonly IFeishuV3JobTitleService _jobTitleApi;

    public JobTitleController(IFeishuV3JobTitleService jobTitleApi)
    {
        _jobTitleApi = jobTitleApi;
    }

    /// <summary>
    /// 获取当前租户下的职务列表
    /// </summary>
    /// <param name="pageSize">分页大小</param>
    /// <param name="pageToken">分页标记</param>
    /// <returns></returns>
    [HttpGet("tenant/list")]
    public async Task<IActionResult> GetTenantJobTitlesList(
        [FromQuery] int pageSize = 10,
        [FromQuery] string? pageToken = null)
    {
        try
        {
            var result = await _jobTitleApi.GetTenantJobTitlesListAsync(pageSize, pageToken);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取当前登录用户下的职务列表
    /// </summary>
    /// <param name="pageSize">分页大小</param>
    /// <param name="pageToken">分页标记</param>
    /// <returns></returns>
    [HttpGet("user/list")]
    public async Task<IActionResult> GetUserJobTitlesList(
        [FromQuery] int pageSize = 10,
        [FromQuery] string? pageToken = null)
    {
        try
        {
            var result = await _jobTitleApi.GetUserJobTitlesListAsync(pageSize, pageToken);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取指定职务的信息（租户级别）
    /// </summary>
    /// <param name="jobTitleId">职务ID</param>
    /// <returns></returns>
    [HttpGet("tenant/{jobTitleId}")]
    public async Task<IActionResult> GetTenantJobTitleById(string jobTitleId)
    {
        try
        {
            var result = await _jobTitleApi.GetTenantJobTitleByIdAsync(jobTitleId);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取指定职务的信息（用户级别）
    /// </summary>
    /// <param name="jobTitleId">职务ID</param>
    /// <returns></returns>
    [HttpGet("user/{jobTitleId}")]
    public async Task<IActionResult> GetUserJobTitleById(string jobTitleId)
    {
        try
        {
            var result = await _jobTitleApi.GetUserJobTitleByIdAsync(jobTitleId);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}