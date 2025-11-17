using Microsoft.AspNetCore.Mvc;

namespace Mud.Feishu.Test.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DepartmentController : ControllerBase
{
    private readonly IFeishuDepartmentsApi _departmentApi;

    public DepartmentController(IFeishuDepartmentsApi departmentApi)
    {
        _departmentApi = departmentApi;
    }

    /// <summary>
    /// 获取部门信息
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <returns></returns>
    [HttpGet("{departmentId}")]
    public async Task<IActionResult> GetDepartment(string departmentId, [FromQuery] string departmentIdType = "department_id")
    {
        try
        {
            var result = await _departmentApi.GetDepartmentByIdAsync("", departmentId, null, departmentIdType);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }



    /// <summary>
    /// 获取部门列表
    /// </summary>
    /// <param name="pageSize">每页数量</param>
    /// <param name="pageToken">分页标记</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <param name="parentDepartmentId">父部门ID</param>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GetDepartments(
        [FromQuery] int pageSize = 50,
        [FromQuery] string? pageToken = null,
        [FromQuery] string departmentIdType = "department_id",
        [FromQuery] string? parentDepartmentId = null)
    {
        try
        {
            var result = await _departmentApi.GetDepartmentsByParentIdAsync("", parentDepartmentId, false, pageSize, pageToken, departmentIdType, parentDepartmentId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}