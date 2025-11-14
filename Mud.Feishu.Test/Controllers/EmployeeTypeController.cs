using Microsoft.AspNetCore.Mvc;

namespace Mud.Feishu.Test.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeeTypeController : ControllerBase
{
    private readonly IFeishuEmployeeTypeApi _employeeTypeApi;

    public EmployeeTypeController(IFeishuEmployeeTypeApi employeeTypeApi)
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
            var result = await _employeeTypeApi.GetEmployeeTypesAsync("", 10, pageToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}