using Microsoft.AspNetCore.Mvc;

namespace Mud.Feishu.Test.Controllers;


[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ITokenManager _tokenManager;

    public AuthController(ITokenManager tokenManager)
    {
        _tokenManager = tokenManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetToken()
    {
        var token = await _tokenManager.GetUserAccessTokenAsync();
        return Ok(token);
    }
}
