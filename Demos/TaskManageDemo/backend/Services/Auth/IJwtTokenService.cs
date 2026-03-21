// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Security.Claims;

namespace TaskManageDemo.Backend.Services.Auth;

/// <summary>
/// JWT Token 服务接口
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// 生成 JWT Token
    /// </summary>
    string GenerateToken(string openId, string unionId, string name, int userId);

    /// <summary>
    /// 生成 JWT Token（带权限）
    /// </summary>
    string GenerateToken(string userId, string username, string openId, string role, List<string> permissions);

    /// <summary>
    /// 生成临时 Token（用于注册流程）
    /// </summary>
    string GenerateTempToken(string feishuId, string name);

    /// <summary>
    /// 验证临时 Token
    /// </summary>
    (string feishuId, string name)? ValidateTempToken(string token);

    /// <summary>
    /// 验证 JWT Token
    /// </summary>
    bool ValidateToken(string token, out ClaimsPrincipal? principal);

    /// <summary>
    /// 从 Token 中获取用户信息
    /// </summary>
    (string openId, string unionId, string name, int userId)? GetUserFromToken(string token);
}
