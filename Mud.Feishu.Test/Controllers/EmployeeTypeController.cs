// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;

namespace Mud.Feishu.Test.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeeTypeController : ControllerBase
{
    private readonly IFeishuV3EmployeeTypeService _employeeTypeApi;

    public EmployeeTypeController(IFeishuV3EmployeeTypeService employeeTypeApi)
    {
        _employeeTypeApi = employeeTypeApi;
    }

    /// <summary>
    /// 获取人员类型列表
    /// </summary>
    /// <param name="pageToken">分页标记</param>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GetEmployeeTypes([FromQuery] string? pageToken = null)
    {
        try
        {
            var result = await _employeeTypeApi.GetEmployeeTypesAsync(10, pageToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}