// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Mud.Feishu.Abstractions;
using TaskManageDemo.Backend.Data;

namespace TaskManageDemo.Backend.EventHandlers;

/// <summary>
/// 用户变更事件处理器
/// </summary>
public class UserChangedEventHandler : IFeishuEventHandler
{
    /// <summary>
    /// 支持的事件类型
    /// </summary>
    public string SupportedEventType => "contact.user.created_v3";

    private readonly TaskManageDbContext _dbContext;
    private readonly ILogger<UserChangedEventHandler> _logger;

    /// <summary>
    /// 初始化用户变更事件处理器
    /// </summary>
    public UserChangedEventHandler(
        TaskManageDbContext dbContext,
        ILogger<UserChangedEventHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 处理事件
    /// </summary>
    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("处理用户变更事件: {EventType}", eventData.EventType);

        var eventJson = JsonSerializer.Serialize(eventData.Event);
        var userEvent = JsonSerializer.Deserialize<UserChangedEvent>(eventJson);

        if (userEvent?.User == null)
        {
            _logger.LogWarning("事件数据解析失败");
            return;
        }

        var user = userEvent.User;
        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.FeishuId == user.UserId, cancellationToken);

        if (existingUser == null)
        {
            existingUser = new User
            {
                FeishuId = user.UserId ?? string.Empty,
                Name = user.Name ?? string.Empty,
                Email = user.Email,
                Mobile = user.Mobile,
                Position = user.Position,
                DepartmentId = user.DepartmentId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastSyncedAt = DateTime.UtcNow
            };
            _dbContext.Users.Add(existingUser);
        }
        else
        {
            existingUser.Name = user.Name ?? existingUser.Name;
            existingUser.Email = user.Email ?? existingUser.Email;
            existingUser.Mobile = user.Mobile ?? existingUser.Mobile;
            existingUser.Position = user.Position ?? existingUser.Position;
            existingUser.DepartmentId = user.DepartmentId ?? existingUser.DepartmentId;
            existingUser.UpdatedAt = DateTime.UtcNow;
            existingUser.LastSyncedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("用户同步完成: {UserId}", user.UserId);
    }
}

/// <summary>
/// 用户变更事件数据
/// </summary>
public class UserChangedEvent
{
    /// <summary>
    /// 用户信息
    /// </summary>
    public UserInfo? User { get; set; }
}

/// <summary>
/// 用户信息
/// </summary>
public class UserInfo
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// 用户姓名
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    public string? Mobile { get; set; }

    /// <summary>
    /// 职位
    /// </summary>
    public string? Position { get; set; }

    /// <summary>
    /// 部门ID
    /// </summary>
    public string? DepartmentId { get; set; }
}
