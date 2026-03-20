// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using TaskManageDemo.Backend.EventHandlers;
using TaskManageDemo.Backend.Services.Templates;

namespace TaskManageDemo.Backend.Data;

/// <summary>
/// 任务管理数据库上下文
/// </summary>
public class TaskManageDbContext : DbContext
{
    /// <summary>
    /// 初始化数据库上下文
    /// </summary>
    public TaskManageDbContext(DbContextOptions<TaskManageDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// 任务同步表
    /// </summary>
    public DbSet<TaskSync> Tasks => Set<TaskSync>();

    /// <summary>
    /// 用户表
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// 部门表
    /// </summary>
    public DbSet<Department> Departments => Set<Department>();

    /// <summary>
    /// 任务清单表
    /// </summary>
    public DbSet<TaskList> TaskLists => Set<TaskList>();

    /// <summary>
    /// 任务成员表
    /// </summary>
    public DbSet<TaskMemberEntity> TaskMembers => Set<TaskMemberEntity>();

    /// <summary>
    /// 任务历史表
    /// </summary>
    public DbSet<TaskHistory> TaskHistories => Set<TaskHistory>();

    /// <summary>
    /// 任务清单成员表
    /// </summary>
    public DbSet<TaskListMember> TaskListMembers => Set<TaskListMember>();

    /// <summary>
    /// 任务模板表
    /// </summary>
    public DbSet<TaskTemplate> TaskTemplates => Set<TaskTemplate>();

    /// <summary>
    /// 事件处理记录表
    /// </summary>
    public DbSet<EventProcessRecord> EventProcessRecords => Set<EventProcessRecord>();

    /// <summary>
    /// 权限定义表
    /// </summary>
    public DbSet<Permission> Permissions => Set<Permission>();

    /// <summary>
    /// 用户权限表
    /// </summary>
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();

    /// <summary>
    /// 配置实体关系
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TaskSync>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TaskGuid).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DueTime);
            entity.HasIndex(e => e.CreatorId);
            entity.HasIndex(e => e.TaskListGuid);

            entity.Property(e => e.Summary).IsRequired().HasMaxLength(500);
            entity.Property(e => e.TaskGuid).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FeishuId).IsUnique();
            entity.HasIndex(e => e.DepartmentId);

            entity.Property(e => e.FeishuId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FeishuId).IsUnique();
            entity.HasIndex(e => e.ParentDepartmentId);

            entity.Property(e => e.FeishuId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<TaskList>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TaskListGuid).IsUnique();
            entity.HasIndex(e => e.OwnerId);

            entity.Property(e => e.TaskListGuid).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<TaskMemberEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TaskSyncId, e.UserId, e.Role }).IsUnique();

            entity.HasOne(e => e.Task)
                .WithMany(t => t.Members)
                .HasForeignKey(e => e.TaskSyncId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.TaskMembers)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TaskSyncId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Task)
                .WithMany(t => t.Histories)
                .HasForeignKey(e => e.TaskSyncId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskListMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TaskListId, e.UserId }).IsUnique();

            entity.HasOne(e => e.TaskList)
                .WithMany(t => t.Members)
                .HasForeignKey(e => e.TaskListId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<EventProcessRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EventId).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.NextRetryAt);

            entity.Property(e => e.EventId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();

            entity.Property(e => e.Code).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Group).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.PermissionCode }).IsUnique();

            entity.Property(e => e.PermissionCode).IsRequired().HasMaxLength(100);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
