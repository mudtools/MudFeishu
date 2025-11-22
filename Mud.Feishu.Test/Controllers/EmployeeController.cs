// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.Employees;

namespace Mud.Feishu.Test.Controllers;

/// <summary>
/// 飞书V1员工管理控制器
/// 用于测试V1版本员工相关的API接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EmployeeController : ControllerBase
{
    private readonly IFeishuV1EmployeesService _v1EmployeeApi;

    public EmployeeController(IFeishuV1EmployeesService v1EmployeeApi)
    {
        _v1EmployeeApi = v1EmployeeApi;
    }

    /// <summary>
    /// 创建员工
    /// </summary>
    /// <param name="userModel">创建的员工请求体</param>
    /// <param name="employeeIdType">用户ID类型</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> CreateEmployee(
        [FromBody] EmployeeCreateRequest userModel,
        [FromQuery] string? employeeIdType = null,
        [FromQuery] string? departmentIdType = null)
    {
        try
        {
            var result = await _v1EmployeeApi.CreateEmployee_Tenant_Async(userModel, employeeIdType, departmentIdType);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 更新员工信息
    /// </summary>
    /// <param name="employeeId">员工ID</param>
    /// <param name="userModel">更新的员工请求体</param>
    /// <param name="employeeIdType">用户ID类型</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <returns></returns>
    [HttpPatch("{employeeId}")]
    public async Task<IActionResult> UpdateEmployee(
        string employeeId,
        [FromBody] EmployeeUpdateRequest userModel,
        [FromQuery] string? employeeIdType = null,
        [FromQuery] string? departmentIdType = null)
    {
        try
        {
            var result = await _v1EmployeeApi.UpdateEmployee_Tenant_Async(employeeId, userModel, employeeIdType, departmentIdType);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 离职员工
    /// </summary>
    /// <param name="employeeId">员工ID</param>
    /// <param name="deleteEmployeeRequest">离职员工请求体</param>
    /// <param name="employeeIdType">用户ID类型</param>
    /// <returns></returns>
    [HttpDelete("{employeeId}")]
    public async Task<IActionResult> DeleteEmployee(
        string employeeId,
        [FromBody] DeleteEmployeeRequest deleteEmployeeRequest,
        [FromQuery] string? employeeIdType = null)
    {
        try
        {
            var result = await _v1EmployeeApi.DeleteEmployeeById_Tenant_Async(employeeId, deleteEmployeeRequest, employeeIdType);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 恢复已离职的员工
    /// </summary>
    /// <param name="employeeId">员工ID</param>
    /// <param name="resurrectEmployeeRequest">恢复离职员工请求体</param>
    /// <param name="employeeIdType">用户ID类型</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <returns></returns>
    [HttpPost("{employeeId}/resurrect")]
    public async Task<IActionResult> ResurrectEmployee(
        string employeeId,
        [FromBody] ResurrectEmployeeRequest resurrectEmployeeRequest,
        [FromQuery] string? employeeIdType = null,
        [FromQuery] string? departmentIdType = null)
    {
        try
        {
            var result = await _v1EmployeeApi.ResurrectEmployee_Tenant_Async(employeeId, resurrectEmployeeRequest, employeeIdType, departmentIdType);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 在职员工流转到待离职状态
    /// </summary>
    /// <param name="employeeId">员工ID</param>
    /// <param name="resignEmployeeRequest">在职员工流转到待离职请求体</param>
    /// <param name="employeeIdType">用户ID类型</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <returns></returns>
    [HttpPatch("{employeeId}/to-be-resigned")]
    public async Task<IActionResult> ResignedEmployee(
        string employeeId,
        [FromBody] ResignEmployeeRequest resignEmployeeRequest,
        [FromQuery] string? employeeIdType = null,
        [FromQuery] string? departmentIdType = null)
    {
        try
        {
            var result = await _v1EmployeeApi.ResignedEmployee_Tenant_Async(employeeId, resignEmployeeRequest, employeeIdType, departmentIdType);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 待离职员工取消离职，更新为在职状态
    /// </summary>
    /// <param name="employeeId">员工ID</param>
    /// <param name="employeeIdType">用户ID类型</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <returns></returns>
    [HttpPatch("{employeeId}/regular")]
    public async Task<IActionResult> RegularEmployee(
        string employeeId,
        [FromQuery] string? employeeIdType = null,
        [FromQuery] string? departmentIdType = null)
    {
        try
        {
            var result = await _v1EmployeeApi.RegularEmployee_Tenant_Async(employeeId, employeeIdType, departmentIdType);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 批量根据员工ID查询员工详情
    /// </summary>
    /// <param name="employeeQueryRequest">员工查询请求体</param>
    /// <param name="employeeIdType">用户ID类型</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <returns></returns>
    [HttpPost("mget")]
    public async Task<IActionResult> QueryEmployees(
        [FromBody] EmployeeQueryRequest employeeQueryRequest,
        [FromQuery] string? employeeIdType = null,
        [FromQuery] string? departmentIdType = null)
    {
        try
        {
            var result = await _v1EmployeeApi.QueryEmployees_Tenant_Async(employeeQueryRequest, employeeIdType, departmentIdType);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 依据指定条件，分页批量获取符合条件的员工详情列表
    /// </summary>
    /// <param name="employeeQueryRequest">员工查询请求体</param>
    /// <param name="employeeIdType">用户ID类型</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <returns></returns>
    [HttpPost("filter")]
    public async Task<IActionResult> QueryEmployeePageList(
        [FromBody] FilterSearchRequest employeeQueryRequest,
        [FromQuery] string? employeeIdType = null,
        [FromQuery] string? departmentIdType = null)
    {
        try
        {
            var result = await _v1EmployeeApi.QueryEmployeePageList_Tenant_Async(employeeQueryRequest, employeeIdType, departmentIdType);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 搜索员工信息
    /// </summary>
    /// <param name="employeeQueryRequest">员工查询请求体</param>
    /// <param name="employeeIdType">用户ID类型</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <returns></returns>
    [HttpPost("search")]
    public async Task<IActionResult> SearchEmployeePageList(
        [FromBody] PageSearchRequest employeeQueryRequest,
        [FromQuery] string? employeeIdType = null,
        [FromQuery] string? departmentIdType = null)
    {
        try
        {
            var result = await _v1EmployeeApi.SearchEmployeePageList_Tenant_Async(employeeQueryRequest, employeeIdType, departmentIdType);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}