// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Mud.Feishu.Abstractions;

namespace Mud.Feishu.Demo.Controllers.MultiApp;

[ApiController]
[Route("api/[controller]")]
public class MultiAppControler : ControllerBase
{
    private readonly IFeishuAppManager _feishuAppManager;

    public MultiAppControler(IFeishuAppManager feishuAppManager)
    {
        _feishuAppManager = feishuAppManager;
    }

    /// <summary>
    /// 获取当前租户下的职务列表
    /// </summary>
    /// <param name="pageSize">分页大小</param>
    /// <param name="pageToken">分页标记</param>
    /// <returns></returns>
    [HttpGet("tenant/list")]
    public async Task<IActionResult> GetTenantJobTitlesList()
    {
        try
        {
            var tenantJobTitleApi = _feishuAppManager.GetWebApi<IFeishuTenantV3JobTitle>("hr-app");
            var result = await tenantJobTitleApi.GetJobTitlesListAsync(10, null);
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
