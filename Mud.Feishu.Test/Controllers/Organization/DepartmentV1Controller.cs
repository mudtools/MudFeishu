// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.DepartmentsV1;

namespace Mud.Feishu.Test.Controllers.Messages;

/// <summary>
/// 飞书部门管理控制器（V1版本）
/// 用于测试部门相关的V1版本API接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DepartmentV1Controller : ControllerBase
{
    private readonly IFeishuV1DepartmentsService _departmentApi;

    public DepartmentV1Controller(IFeishuV1DepartmentsService departmentApi)
    {
        _departmentApi = departmentApi;
    }

    /// <summary>
    /// 创建部门
    /// 用于在企业组织机构中创建新部门，支持设置部门名称、父部门、负责人等信息
    /// </summary>
    /// <param name="departmentCreateRequest">创建部门的请求体</param>
    /// <param name="employeeIdType">用户ID类型，默认值：open_id</param>
    /// <param name="departmentIdType">部门ID类型，默认值：department_id</param>
    /// <returns>创建结果</returns>
    [HttpPost]
    public async Task<IActionResult> CreateDepartmentAsync(
        [FromBody] DepartmentCreateUpdateRequest departmentCreateRequest,
        [FromQuery] string employeeIdType = "open_id",
        [FromQuery] string departmentIdType = "department_id")
    {
        try
        {
            var result = await _departmentApi.CreateDepartment_Tenant_Async(
                departmentCreateRequest,
                employeeIdType,
                departmentIdType);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "部门创建成功"
                });
            }
            else
            {
                return BadRequest(new { success = false, error = result.Msg, code = result.Code });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 更新部门
    /// 用于更新企业组织机构部门信息。仅更新显式传参的部分
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="departmentUpdateRequest">更新部门的请求体</param>
    /// <param name="employeeIdType">用户ID类型，默认值：open_id</param>
    /// <param name="departmentIdType">部门ID类型，默认值：department_id</param>
    /// <returns>更新结果</returns>
    [HttpPatch("{departmentId}")]
    public async Task<IActionResult> UpdateDepartmentAsync(
        [FromRoute] string departmentId,
        [FromBody] DepartmentCreateUpdateRequest departmentUpdateRequest,
        [FromQuery] string employeeIdType = "open_id",
        [FromQuery] string departmentIdType = "department_id")
    {
        try
        {
            var result = await _departmentApi.UpdateDepartment_Tenant_Async(
                departmentId,
                departmentUpdateRequest,
                employeeIdType,
                departmentIdType);

            if (result.Code == 0)
            {
                return Ok(new { success = true, message = "部门更新成功" });
            }
            else
            {
                return BadRequest(new { success = false, error = result.Msg, code = result.Code });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 删除部门
    /// 从企业组织机构中删除指定的部门
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="departmentIdType">部门ID类型，默认值：department_id</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{departmentId}")]
    public async Task<IActionResult> DeleteDepartmentAsync(
        [FromRoute] string departmentId,
        [FromQuery] string departmentIdType = "department_id")
    {
        try
        {
            var result = await _departmentApi.DeleteDepartmentById_Tenant_Async(
                departmentId,
                departmentIdType);

            if (result.Code == 0)
            {
                return Ok(new { success = true, message = "部门删除成功" });
            }
            else
            {
                return BadRequest(new { success = false, error = result.Msg, code = result.Code });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 批量查询部门
    /// 支持传入多个部门ID，返回每个部门的详细信息（如名称、负责人、子部门等）
    /// </summary>
    /// <param name="departmentQueryRequest">部门查询参数请求体</param>
    /// <param name="employeeIdType">用户ID类型，默认值：open_id</param>
    /// <param name="departmentIdType">部门ID类型，默认值：department_id</param>
    /// <returns>部门详细信息列表</returns>
    [HttpPost("batch")]
    public async Task<IActionResult> QueryDepartmentsAsync(
        [FromBody] DepartmentQueryRequest departmentQueryRequest,
        [FromQuery] string employeeIdType = "open_id",
        [FromQuery] string departmentIdType = "department_id")
    {
        try
        {
            var result = await _departmentApi.QueryDepartments_Tenant_Async(
                departmentQueryRequest,
                employeeIdType,
                departmentIdType);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "批量查询部门成功"
                });
            }
            else
            {
                return BadRequest(new { success = false, error = result.Msg, code = result.Code });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 分页查询部门
    /// 用于依据指定条件，批量获取符合条件的部门详情列表
    /// </summary>
    /// <param name="filterSearchRequest">字段过滤查询条件请求体</param>
    /// <param name="employeeIdType">用户ID类型，默认值：open_id</param>
    /// <param name="departmentIdType">部门ID类型，默认值：department_id</param>
    /// <returns>分页部门列表</returns>
    [HttpPost("filter")]
    public async Task<IActionResult> QueryDepartmentsPageListAsync(
        [FromBody] FilterSearchRequest filterSearchRequest,
        [FromQuery] string employeeIdType = "open_id",
        [FromQuery] string departmentIdType = "department_id")
    {
        try
        {
            var result = await _departmentApi.QueryDepartmentsPageList_Tenant_Async(
                filterSearchRequest,
                employeeIdType,
                departmentIdType);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = "分页查询部门成功"
                });
            }
            else
            {
                return BadRequest(new { success = false, error = result.Msg, code = result.Code });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 搜索部门
    /// 用于搜索部门信息，通过部门名称等关键词搜索部门信息，返回符合条件的部门列表
    /// </summary>
    /// <param name="pageSearchRequest">分页查询参数请求体</param>
    /// <param name="employeeIdType">用户ID类型，默认值：open_id</param>
    /// <param name="departmentIdType">部门ID类型，默认值：department_id</param>
    /// <returns>搜索部门列表</returns>
    [HttpPost("search")]
    public async Task<IActionResult> SearchDepartmentsAsync(
        [FromBody] PageSearchRequest pageSearchRequest,
        [FromQuery] string employeeIdType = "open_id",
        [FromQuery] string departmentIdType = "department_id")
    {
        try
        {
            var result = await _departmentApi.SearchEmployeePageList_Tenant_Async(
                pageSearchRequest,
                employeeIdType,
                departmentIdType);

            if (result.Code == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    pageToken = result.Data.Page.PageToken,
                    hasMore = result.Data.Page.HasMore,
                    message = "搜索部门成功"
                });
            }
            else
            {
                return BadRequest(new { success = false, error = result.Msg, code = result.Code });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }
}