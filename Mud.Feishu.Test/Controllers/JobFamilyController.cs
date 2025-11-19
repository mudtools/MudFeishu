using Microsoft.AspNetCore.Mvc;
using Mud.Feishu.DataModels.JobFamilies;

namespace Mud.Feishu.Test.Controllers;

/// <summary>
/// 飞书职位序列管理控制器
/// 用于测试职位序列相关的API接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class JobFamilyController : ControllerBase
{
    private readonly IFeishuJobFamilies _jobFamiliesApi;

    public JobFamilyController(IFeishuJobFamilies jobFamiliesApi)
    {
        _jobFamiliesApi = jobFamiliesApi;
    }

    /// <summary>
    /// 创建职位序列
    /// </summary>
    /// <param name="request">职位序列创建请求体</param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> CreateJobFamily(
        [FromBody] JobFamilyCreateUpdateRequest request)
    {
        try
        {
            var result = await _jobFamiliesApi.CreateJobFamilyAsync( request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 更新职位序列
    /// </summary>
    /// <param name="jobFamilyId">职位序列ID</param>
    /// <param name="request">职位序列更新请求体</param>
    /// <returns></returns>
    [HttpPut("{jobFamilyId}")]
    public async Task<IActionResult> UpdateJobFamily(
        string jobFamilyId,
        [FromBody] JobFamilyCreateUpdateRequest request)
    {
        try
        {
            var result = await _jobFamiliesApi.UpdateJobFamilyAsync( jobFamilyId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取指定职位序列信息
    /// </summary>
    /// <param name="jobFamilyId">职位序列ID</param>
    /// <returns></returns>
    [HttpGet("{jobFamilyId}")]
    public async Task<IActionResult> GetJobFamily(
        string jobFamilyId)
    {
        try
        {
            var result = await _jobFamiliesApi.GetJobFamilyByIdAsync( jobFamilyId);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取职位序列列表
    /// </summary>
    /// <param name="name">序列名称</param>
    /// <param name="pageSize">分页大小，默认值：10</param>
    /// <param name="pageToken">分页标记</param>
    /// <returns></returns>
    [HttpGet("list")]
    public async Task<IActionResult> GetJobFamiliesList(
        [FromQuery] string name = null,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? pageToken = null)
    {
        try
        {
            var result = await _jobFamiliesApi.GetJobFamilesListAsync(
                name, 
                pageSize, 
                pageToken);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 删除职位序列
    /// </summary>
    /// <param name="jobFamilyId">职位序列ID</param>
    /// <returns></returns>
    [HttpDelete("{jobFamilyId}")]
    public async Task<IActionResult> DeleteJobFamily(
        string jobFamilyId)
    {
        try
        {
            var result = await _jobFamiliesApi.DeleteJobFamilyByIdAsync( jobFamilyId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}