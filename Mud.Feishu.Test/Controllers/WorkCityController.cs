using Microsoft.AspNetCore.Mvc;

namespace Mud.Feishu.Test.Controllers;

/// <summary>
/// 飞书工作城市管理控制器
/// 用于测试工作城市相关的API接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WorkCityController : ControllerBase
{
    private readonly IFeishuV3WorkCity _workCityApi;

    public WorkCityController(IFeishuV3WorkCity workCityApi)
    {
        _workCityApi = workCityApi;
    }

    /// <summary>
    /// 获取当前租户下所有工作城市列表
    /// </summary>
    /// <param name="pageSize">分页大小</param>
    /// <param name="pageToken">分页标记</param>
    /// <returns></returns>
    [HttpGet("tenant/list")]
    public async Task<IActionResult> GetTenantWorkCitiesList(
        [FromQuery] int pageSize = 10,
        [FromQuery] string? pageToken = null)
    {
        try
        {
            var result = await _workCityApi.GetTenantWorkCitesListAsync(pageSize, pageToken);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取当前登录用户下所有工作城市列表
    /// </summary>
    /// <param name="pageSize">分页大小</param>
    /// <param name="pageToken">分页标记</param>
    /// <returns></returns>
    [HttpGet("user/list")]
    public async Task<IActionResult> GetUserWorkCitiesList(
        [FromQuery] int pageSize = 10,
        [FromQuery] string? pageToken = null)
    {
        try
        {
            var result = await _workCityApi.GetUserWorkCitesListAsync(pageSize, pageToken);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取指定工作城市的信息（租户级别）
    /// </summary>
    /// <param name="workCityId">工作城市ID</param>
    /// <returns></returns>
    [HttpGet("tenant/{workCityId}")]
    public async Task<IActionResult> GetTenantWorkCityById(string workCityId)
    {
        try
        {
            var result = await _workCityApi.GetTenantWorkCityByIdAsync(workCityId);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取指定工作城市的信息（用户级别）
    /// </summary>
    /// <param name="workCityId">工作城市ID</param>
    /// <returns></returns>
    [HttpGet("user/{workCityId}")]
    public async Task<IActionResult> GetUserWorkCityById(string workCityId)
    {
        try
        {
            var result = await _workCityApi.GetUserWorkCityByIdAsync(workCityId);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}