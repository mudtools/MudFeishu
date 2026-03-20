// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace TaskManageDemo.Backend.Models.DTOs;

/// <summary>
/// 用户信息DTO
/// </summary>
public class UserDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 飞书用户ID
    /// </summary>
    public string FeishuId { get; set; } = string.Empty;

    /// <summary>
    /// 用户Open ID
    /// </summary>
    public string? OpenId { get; set; }

    /// <summary>
    /// 用户Union ID
    /// </summary>
    public string? UnionId { get; set; }

    /// <summary>
    /// 用户姓名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 英文名
    /// </summary>
    public string? EnglishName { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    public string? Mobile { get; set; }

    /// <summary>
    /// 头像URL
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// 所属部门ID
    /// </summary>
    public string? DepartmentId { get; set; }

    /// <summary>
    /// 所属部门名称
    /// </summary>
    public string? DepartmentName { get; set; }

    /// <summary>
    /// 职位
    /// </summary>
    public string? Position { get; set; }

    /// <summary>
    /// 用户角色
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// 登录请求
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// 飞书授权码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 授权类型：authorization_code
    /// </summary>
    public string GrantType { get; set; } = "authorization_code";
}

/// <summary>
/// 登录响应
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// 访问令牌
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// 令牌类型
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// 过期时间（秒）
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// 用户信息
    /// </summary>
    public UserDto User { get; set; } = new();

    /// <summary>
    /// 是否为首次登录
    /// </summary>
    public bool IsFirstLogin { get; set; }
}

/// <summary>
/// OAuth 授权链接请求
/// </summary>
public class OAuthUrlRequest
{
    /// <summary>
    /// 重定向URL
    /// </summary>
    public string? RedirectUri { get; set; }

    /// <summary>
    /// 状态码（用于防止CSRF攻击）
    /// </summary>
    public string? State { get; set; }
}

/// <summary>
/// OAuth 授权链接响应
/// </summary>
public class OAuthUrlResponse
{
    /// <summary>
    /// 授权链接
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 状态码
    /// </summary>
    public string? State { get; set; }
}

/// <summary>
/// 更新用户请求
/// </summary>
public class UpdateUserRequest
{
    /// <summary>
    /// 用户姓名
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 英文名
    /// </summary>
    public string? EnglishName { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    public string? Mobile { get; set; }

    /// <summary>
    /// 用户角色
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// 当前用户信息
/// </summary>
public class CurrentUserInfo
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 飞书用户ID
    /// </summary>
    public string FeishuId { get; set; } = string.Empty;

    /// <summary>
    /// 用户姓名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 头像URL
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// 用户角色
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// 权限列表
    /// </summary>
    public List<string> Permissions { get; set; } = new();

    /// <summary>
    /// 所属部门
    /// </summary>
    public string? DepartmentName { get; set; }

    /// <summary>
    /// 职位
    /// </summary>
    public string? Position { get; set; }
}

/// <summary>
/// 用户查询参数
/// </summary>
public class UserQueryParameters
{
    /// <summary>
    /// 页码
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// 搜索关键词（姓名/邮箱）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 角色筛选
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// 部门ID筛选
    /// </summary>
    public string? DepartmentId { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool? IsActive { get; set; }
}
