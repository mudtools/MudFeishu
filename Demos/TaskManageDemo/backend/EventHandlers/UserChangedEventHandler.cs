// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.DataModels.Organization;
using Mud.Feishu.Abstractions.EventHandlers;
using Mud.Feishu.Abstractions.Services;
using TaskManageDemo.Backend.Data;

namespace TaskManageDemo.Backend.EventHandlers;

/// <summary>
/// 用户变更事件处理器
/// <para>处理飞书用户创建和更新事件</para>
/// </summary>
public class UserCreatedEventHandler : UserCreateEventHandler
{
    private readonly TaskManageDbContext _dbContext;

    public UserCreatedEventHandler(
        IFeishuEventDeduplicator businessDeduplicator,
        ILogger<UserCreatedEventHandler> logger,
        TaskManageDbContext dbContext)
        : base(businessDeduplicator, logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        UserCreateResult? eventEntity,
        CancellationToken cancellationToken = default)
    {
        if (eventEntity == null)
        {
            _logger.LogWarning("用户创建事件实体为空，跳过处理");
            return;
        }

        _logger.LogInformation("处理用户创建事件: OpenId={OpenId}, Name={Name}",
            eventEntity.OpenId, eventEntity.Name);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.FeishuId == eventEntity.OpenId, cancellationToken);

        if (user == null)
        {
            user = new User
            {
                FeishuId = eventEntity.OpenId ?? string.Empty,
                Name = eventEntity.Name ?? string.Empty,
                Email = eventEntity.Email ?? eventEntity.EnterpriseEmail,
                Mobile = eventEntity.Mobile,
                Position = eventEntity.JobTitle,
                DepartmentId = eventEntity.DepartmentIds?.FirstOrDefault(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastSyncedAt = DateTime.UtcNow
            };
            _dbContext.Users.Add(user);
            _logger.LogInformation("创建新用户: FeishuId={FeishuId}, Name={Name}",
                user.FeishuId, user.Name);
        }
        else
        {
            user.Name = eventEntity.Name ?? user.Name;
            user.Email = eventEntity.Email ?? eventEntity.EnterpriseEmail ?? user.Email;
            user.Mobile = eventEntity.Mobile ?? user.Mobile;
            user.Position = eventEntity.JobTitle ?? user.Position;
            user.DepartmentId = eventEntity.DepartmentIds?.FirstOrDefault() ?? user.DepartmentId;
            user.UpdatedAt = DateTime.UtcNow;
            user.LastSyncedAt = DateTime.UtcNow;
            _logger.LogInformation("更新用户: FeishuId={FeishuId}, Name={Name}",
                user.FeishuId, user.Name);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("用户同步完成: OpenId={OpenId}", eventEntity.OpenId);
    }
}

/// <summary>
/// 用户更新事件处理器
/// <para>处理飞书用户信息更新事件</para>
/// </summary>
public class UserUpdatedEventHandler : UserUpdateEventHandler
{
    private readonly TaskManageDbContext _dbContext;

    public UserUpdatedEventHandler(
        IFeishuEventDeduplicator businessDeduplicator,
        ILogger<UserUpdatedEventHandler> logger,
        TaskManageDbContext dbContext)
        : base(businessDeduplicator, logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        UserUpdateResult? eventEntity,
        CancellationToken cancellationToken = default)
    {
        if (eventEntity?.Object == null)
        {
            _logger.LogWarning("用户更新事件实体为空，跳过处理");
            return;
        }

        var userInfo = eventEntity.Object;
        _logger.LogInformation("处理用户更新事件: OpenId={OpenId}, Name={Name}",
            userInfo.OpenId, userInfo.Name);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.FeishuId == userInfo.OpenId, cancellationToken);

        if (user == null)
        {
            user = new User
            {
                FeishuId = userInfo.OpenId ?? string.Empty,
                Name = userInfo.Name ?? string.Empty,
                Email = userInfo.Email ?? userInfo.EnterpriseEmail,
                Mobile = userInfo.Mobile,
                Position = userInfo.JobTitle,
                DepartmentId = userInfo.DepartmentIds?.FirstOrDefault(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastSyncedAt = DateTime.UtcNow
            };
            _dbContext.Users.Add(user);
            _logger.LogInformation("创建新用户（来自更新事件）: FeishuId={FeishuId}, Name={Name}",
                user.FeishuId, user.Name);
        }
        else
        {
            user.Name = userInfo.Name ?? user.Name;
            user.Email = userInfo.Email ?? userInfo.EnterpriseEmail ?? user.Email;
            user.Mobile = userInfo.Mobile ?? user.Mobile;
            user.Position = userInfo.JobTitle ?? user.Position;
            user.DepartmentId = userInfo.DepartmentIds?.FirstOrDefault() ?? user.DepartmentId;
            user.UpdatedAt = DateTime.UtcNow;
            user.LastSyncedAt = DateTime.UtcNow;
            _logger.LogInformation("更新用户: FeishuId={FeishuId}, Name={Name}",
                user.FeishuId, user.Name);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("用户更新同步完成: OpenId={OpenId}", userInfo.OpenId);
    }
}
