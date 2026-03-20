// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManageDemo.Backend.Data;

namespace TaskManageDemo.Backend.Tests.Integration;

/// <summary>
/// 集成测试工厂
/// </summary>
public class IntegrationTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 替换数据库为内存数据库
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<TaskManageDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<TaskManageDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase");
            });

            // 构建服务提供者
            var serviceProvider = services.BuildServiceProvider();

            // 初始化数据库
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TaskManageDbContext>();
            db.Database.EnsureCreated();
        });

        builder.UseEnvironment("Testing");
    }
}
