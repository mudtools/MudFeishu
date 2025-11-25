// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Mud.Feishu.DataModels.Units;

namespace Mud.Feishu.Test.Controllers.Messages;

/// <summary>
/// 飞书单位管理控制器
/// 用于测试单位相关的API接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UnitController : ControllerBase
{
    private readonly IFeishuTenantV3Unit _unitApi;

    public UnitController(IFeishuTenantV3Unit unitApi)
    {
        _unitApi = unitApi;
    }

    /// <summary>
    /// 创建单位
    /// </summary>
    /// <param name="unitRequest">单位创建请求体</param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> CreateUnit([FromBody] UnitInfoRequest unitRequest)
    {
        try
        {
            var result = await _unitApi.CreateUnitAsync(unitRequest);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 更新单位名称
    /// </summary>
    /// <param name="unitId">单位ID</param>
    /// <param name="nameRequest">单位名称更新请求体</param>
    /// <returns></returns>
    [HttpPatch("{unitId}")]
    public async Task<IActionResult> UpdateUnit(string unitId, [FromBody] UnitNameUpdateRequest nameRequest)
    {
        try
        {
            var result = await _unitApi.UpdateUnitAsync(unitId, nameRequest);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取单位信息
    /// </summary>
    /// <param name="unitId">单位ID</param>
    /// <returns></returns>
    [HttpGet("{unitId}")]
    public async Task<IActionResult> GetUnit(string unitId)
    {
        try
        {
            var result = await _unitApi.GetUnitInfoAsync(unitId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取单位列表
    /// </summary>
    /// <param name="pageSize">分页大小</param>
    /// <param name="pageToken">分页标记</param>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GetUnits(
        [FromQuery] int pageSize = 10,
        [FromQuery] string? pageToken = null)
    {
        try
        {
            var result = await _unitApi.GetUnitListAsync(pageSize, pageToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取单位绑定的部门列表
    /// </summary>
    /// <param name="unitId">单位ID</param>
    /// <param name="pageSize">分页大小</param>
    /// <param name="pageToken">分页标记</param>
    /// <param name="departmentIdType">部门ID类型</param>
    /// <returns></returns>
    [HttpGet("{unitId}/departments")]
    public async Task<IActionResult> GetUnitDepartments(
        string unitId,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? pageToken = null,
        [FromQuery] string? departmentIdType = null)
    {
        try
        {
            var result = await _unitApi.GetDepartmentListAsync(unitId, pageSize, pageToken, departmentIdType);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 绑定部门到单位
    /// </summary>
    /// <param name="bindRequest">绑定请求体</param>
    /// <returns></returns>
    [HttpPost("bind-department")]
    public async Task<IActionResult> BindDepartment([FromBody] UnitBindDepartmentRequest bindRequest)
    {
        try
        {
            var result = await _unitApi.BindDepartmentAsync(bindRequest);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 解除部门与单位的绑定
    /// </summary>
    /// <param name="unbindRequest">解除绑定请求体</param>
    /// <returns></returns>
    [HttpPost("unbind-department")]
    public async Task<IActionResult> UnbindDepartment([FromBody] UnitBindDepartmentRequest unbindRequest)
    {
        try
        {
            var result = await _unitApi.UnBindDepartmentAsync(unbindRequest);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 删除单位
    /// </summary>
    /// <param name="unitId">单位ID</param>
    /// <returns></returns>
    [HttpDelete("{unitId}")]
    public async Task<IActionResult> DeleteUnit(string unitId)
    {
        try
        {
            var result = await _unitApi.DeleteUnitByIdAsync(unitId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}