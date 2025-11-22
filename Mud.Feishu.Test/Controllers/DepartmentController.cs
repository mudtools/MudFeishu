// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.Departments;
using System.ComponentModel.DataAnnotations;

namespace Mud.Feishu.Test.Controllers;

/// <summary>
/// 飞书部门管理控制器
/// 用于测试部门相关的API接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DepartmentController : ControllerBase
{
    private readonly IFeishuV3DepartmentsService _departmentApi;

    public DepartmentController(IFeishuV3DepartmentsService departmentApi)
    {
        _departmentApi = departmentApi;
    }

    /// <summary>
    /// 创建部门
    /// </summary>
    /// <param name="departmentCreateRequest">创建部门的请求体</param>
    /// <param name="userIdType">用户ID类型</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <param name="clientToken">用于幂等判断的客户端令牌</param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> CreateDepartment(
        [FromBody] DepartmentCreateRequest departmentCreateRequest,
        [FromQuery] string? userIdType = null,
        [FromQuery] string? departmentIdType = null,
        [FromQuery] string? clientToken = null)
    {
        try
        {
            var result = await _departmentApi.CreateDepartmentAsync(departmentCreateRequest, userIdType, departmentIdType, clientToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 部分更新部门信息
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="departmentPartUpdateRequest">部分更新部门的请求体</param>
    /// <param name="userIdType">用户ID类型</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <returns></returns>
    [HttpPatch("{departmentId}")]
    public async Task<IActionResult> UpdatePartDepartment(
        string departmentId,
        [FromBody] DepartmentPartUpdateRequest departmentPartUpdateRequest,
        [FromQuery] string? userIdType = null,
        [FromQuery] string? departmentIdType = null)
    {
        try
        {
            var result = await _departmentApi.UpdatePartDepartmentAsync(departmentId, departmentPartUpdateRequest, userIdType, departmentIdType);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 完全更新部门信息
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="departmentUpdateRequest">更新部门的请求体</param>
    /// <param name="userIdType">用户ID类型</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <returns></returns>
    [HttpPut("{departmentId}")]
    public async Task<IActionResult> UpdateDepartment(
        string departmentId,
        [FromBody] DepartmentUpdateRequest departmentUpdateRequest,
        [FromQuery] string? userIdType = null,
        [FromQuery] string? departmentIdType = null)
    {
        try
        {
            var result = await _departmentApi.UpdateDepartmentAsync(departmentId, departmentUpdateRequest, userIdType, departmentIdType);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 更新部门ID
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="departMentUpdateIdRequest">更新部门ID的请求体</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <returns></returns>
    [HttpPatch("{departmentId}/update-department-id")]
    public async Task<IActionResult> UpdateDepartmentId(
        string departmentId,
        [FromBody] DepartMentUpdateIdRequest departMentUpdateIdRequest,
        [FromQuery] string? departmentIdType = null)
    {
        try
        {
            var result = await _departmentApi.UpdateDepartmentIdAsync(departmentId, departMentUpdateIdRequest, departmentIdType);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 解绑部门群聊
    /// </summary>
    /// <param name="departmentRequest">部门请求体</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <returns></returns>
    [HttpPost("unbind-department-chat")]
    public async Task<IActionResult> UnbindDepartmentChat(
        [FromBody] DepartmentRequest departmentRequest,
        [FromQuery] string? departmentIdType = null)
    {
        try
        {
            var result = await _departmentApi.UnbindDepartmentChatAsync(departmentRequest, departmentIdType);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取部门信息
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="userIdType">用户ID类型</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <returns></returns>
    [HttpGet("{departmentId}")]
    public async Task<IActionResult> GetDepartment(
        string departmentId,
        [FromQuery] string? userIdType = null,
        [FromQuery] string? departmentIdType = null)
    {
        try
        {
            var result = await _departmentApi.GetDepartmentInfoByIdAsync(departmentId, userIdType, departmentIdType);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 批量获取部门信息
    /// </summary>
    /// <param name="departmentIds">部门ID数组</param>
    /// <param name="userIdType">用户ID类型</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <returns></returns>
    [HttpGet("batch")]
    public async Task<IActionResult> GetDepartmentsByIds(
        [FromQuery][Required] string[] departmentIds,
        [FromQuery] string? userIdType = null,
        [FromQuery] string? departmentIdType = null)
    {
        try
        {
            var result = await _departmentApi.GetDepartmentsByIdsAsync(departmentIds, userIdType, departmentIdType);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取子部门列表
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="fetchChild">是否递归获取子部门</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="pageToken">分页标记</param>
    /// <param name="userIdType">用户ID类型</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <returns></returns>
    [HttpGet("{departmentId}/children")]
    public async Task<IActionResult> GetSubDepartments(
        string departmentId,
        [FromQuery] bool fetchChild = false,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? pageToken = null,
        [FromQuery] string? userIdType = null,
        [FromQuery] string? departmentIdType = null)
    {
        try
        {
            var result = await _departmentApi.GetDepartmentsByParentIdAsync(departmentId, fetchChild, pageSize, pageToken, userIdType, departmentIdType);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取父部门列表
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="pageToken">分页标记</param>
    /// <param name="userIdType">用户ID类型</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <returns></returns>
    [HttpGet("{departmentId}/parents")]
    public async Task<IActionResult> GetParentDepartments(
        string departmentId,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? pageToken = null,
        [FromQuery] string? userIdType = null,
        [FromQuery] string? departmentIdType = null)
    {
        try
        {
            var result = await _departmentApi.GetParentDepartmentsByIdAsync(departmentId, pageSize, pageToken, userIdType, departmentIdType);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 搜索部门
    /// </summary>
    /// <param name="searchRequest">搜索请求体</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="pageToken">分页标记</param>
    /// <param name="userIdType">用户ID类型</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <returns></returns>
    [HttpPost("search")]
    public async Task<IActionResult> SearchDepartments(
        [FromBody] SearchRequest searchRequest,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? pageToken = null,
        [FromQuery] string? userIdType = null,
        [FromQuery] string? departmentIdType = null)
    {
        try
        {
            var result = await _departmentApi.SearchDepartmentsAsync(searchRequest, pageSize, pageToken, userIdType, departmentIdType);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}