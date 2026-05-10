// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskManageDemo.Backend.Data;

/// <summary>
/// 设计时数据库上下文工厂 - 用于 EF Core 迁移
/// </summary>
public class TaskManageDbContextFactory : IDesignTimeDbContextFactory<TaskManageDbContext>
{
    /// <summary>
    /// 创建数据库上下文实例
    /// </summary>
    public TaskManageDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TaskManageDbContext>();
        optionsBuilder.UseSqlite("Data Source=TaskManage.db");

        return new TaskManageDbContext(optionsBuilder.Options);
    }
}
