// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TaskManageDemo.Backend.Data;
using TaskManageDemo.Backend.Models.Entities;
using TaskManageDemo.Backend.Services.Feishu;

namespace TaskManageDemo.Backend.Tests.Integration;

/// <summary>
/// 集成测试工厂
/// </summary>
public class IntegrationTestFactory : WebApplicationFactory<Program>
{
    public const string TestUserId = "test-user-id";
    public const string TestUserFeishuId = "test-feishu-id";
    public const string TestUserName = "Test User";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.PostConfigure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            });

            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            // Mock 外部飞书 API 边界：集成测试不依赖真实飞书凭据与网络
            var feishuTasks = new Dictionary<string, TaskSync>();
            var feishuTaskServiceMock = new Mock<IFeishuTaskService>();

            feishuTaskServiceMock
                .Setup(x => x.CreateTaskAsync(
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<List<string>?>(),
                    It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .Returns<string, string?, List<string>?, DateTime?, DateTime?, string?, CancellationToken>(
                    (summary, description, _, _, _, _, _) =>
                    {
                        var taskGuid = $"mock-{Guid.NewGuid():N}";
                        feishuTasks[taskGuid] = new TaskSync
                        {
                            TaskGuid = taskGuid,
                            Summary = summary,
                            Description = description
                        };
                        return Task.FromResult<string?>(taskGuid);
                    });

            feishuTaskServiceMock
                .Setup(x => x.UpdateTaskAsync(
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                    It.IsAny<bool?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                .Returns<string, string?, string?, bool?, DateTime?, CancellationToken>(
                    (taskGuid, summary, description, isCompleted, _, _) =>
                    {
                        if (!feishuTasks.TryGetValue(taskGuid, out var task))
                        {
                            return Task.FromResult(false);
                        }

                        if (summary != null) task.Summary = summary;
                        if (description != null) task.Description = description;
                        if (isCompleted.HasValue) task.IsCompleted = isCompleted.Value;
                        return Task.FromResult(true);
                    });

            feishuTaskServiceMock
                .Setup(x => x.GetTaskByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns<string, CancellationToken>((taskGuid, _) =>
                    Task.FromResult(feishuTasks.TryGetValue(taskGuid, out var task) ? task : null));

            feishuTaskServiceMock
                .Setup(x => x.DeleteTaskAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns<string, CancellationToken>((taskGuid, _) =>
                    Task.FromResult(feishuTasks.Remove(taskGuid)));

            services.RemoveAll<IFeishuTaskService>();
            services.AddSingleton(feishuTaskServiceMock.Object);
        });
    }

    public async Task InitializeTestDataAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskManageDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (!await db.Users.AnyAsync(u => u.FeishuId == TestUserFeishuId))
        {
            var testUser = new User
            {
                FeishuId = TestUserFeishuId,
                Name = TestUserName,
                Email = "test@example.com",
                Role = "user",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastSyncedAt = DateTime.UtcNow
            };
            db.Users.Add(testUser);
            await db.SaveChangesAsync();
        }
    }
}

/// <summary>
/// 测试认证处理器
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, IntegrationTestFactory.TestUserId),
            new(ClaimTypes.Name, IntegrationTestFactory.TestUserName),
            new("feishu_id", IntegrationTestFactory.TestUserFeishuId),
            new("role", "user"),
            new("permission", "task:read"),
            new("permission", "task:create"),
            new("permission", "task:update"),
            new("permission", "task:delete")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
