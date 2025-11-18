using Microsoft.AspNetCore.Mvc;
using Mud.Feishu.DataModels.JobLevel;

namespace Mud.Feishu.Test.Controllers;

/// <summary>
/// 飞书职级管理控制器
/// 用于测试职级相关的API接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class JobLevelController : ControllerBase
{
    private readonly IFeishuJobLevel _jobLevelApi;

    public JobLevelController(IFeishuJobLevel jobLevelApi)
    {
        _jobLevelApi = jobLevelApi;
    }

    /// <summary>
    /// 创建职级
    /// </summary>
    /// <param name="levelRequest">职级创建请求体</param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> CreateJobLevel([FromBody] JobLevelCreateUpdateRequest levelRequest)
    {
        try
        {
            var result = await _jobLevelApi.CreateJobLevelAsync(levelRequest);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 更新职级
    /// </summary>
    /// <param name="jobLevelId">职级ID</param>
    /// <param name="levelRequest">职级更新请求体</param>
    /// <returns></returns>
    [HttpPut("{jobLevelId}")]
    public async Task<IActionResult> UpdateJobLevel(string jobLevelId, [FromBody] JobLevelCreateUpdateRequest levelRequest)
    {
        try
        {
            var result = await _jobLevelApi.UpdateJobLevelAsync(jobLevelId, levelRequest);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取职级信息
    /// </summary>
    /// <param name="jobLevelId">职级ID</param>
    /// <returns></returns>
    [HttpGet("{jobLevelId}")]
    public async Task<IActionResult> GetJobLevel(string jobLevelId)
    {
        try
        {
            var result = await _jobLevelApi.GetJobLevelByIdAsync(jobLevelId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取职级列表
    /// </summary>
    /// <param name="name">职级名称</param>
    /// <param name="pageSize">分页大小</param>
    /// <param name="pageToken">分页标记</param>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GetJobLevels(
        [FromQuery] string? name = null,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? pageToken = null)
    {
        try
        {
            var result = await _jobLevelApi.GetJobLevelListAsync(name, pageSize, pageToken);
            return Ok(result.Data.Items);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 删除职级
    /// </summary>
    /// <param name="jobLevelId">职级ID</param>
    /// <returns></returns>
    [HttpDelete("{jobLevelId}")]
    public async Task<IActionResult> DeleteJobLevel(string jobLevelId)
    {
        try
        {
            var result = await _jobLevelApi.DeleteJobLevelByIdAsync(jobLevelId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}