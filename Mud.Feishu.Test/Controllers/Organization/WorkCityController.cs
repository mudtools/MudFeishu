// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;

namespace Mud.Feishu.Test.Controllers.Messages;

/// <summary>
/// 飞书工作城市管理控制器
/// 用于测试工作城市相关的API接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WorkCityController : ControllerBase
{
    private readonly IFeishuV3WorkCityService _workCityApi;

    public WorkCityController(IFeishuV3WorkCityService workCityApi)
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
            var result = await _workCityApi.GetWorkCitesList_Tenant_Async(pageSize, pageToken);
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
            var result = await _workCityApi.GetWorkCitesList_User_Async(pageSize, pageToken);
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
            var result = await _workCityApi.GetWorkCityById_Tenant_Async(workCityId);
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
            var result = await _workCityApi.GetWorkCityById_User_Async(workCityId);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}